using System.ClientModel;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using LinkedInTutorAI.Application.Abstractions;
using LinkedInTutorAI.Application.Dtos;
using LinkedInTutorAI.Application.Exceptions;
using LinkedInTutorAI.Application.Options;
using LinkedInTutorAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Implementa <see cref="IAiFoundryAgentService"/> sobre la implementación oficial
/// <c>Azure.AI.Projects</c> + <c>Azure.AI.Extensions.OpenAI</c> (<c>AIProjectClient</c>,
/// <c>ProjectResponsesClient</c>). Recupera el cliente de respuestas para el agente
/// (<see cref="FoundryOptions.AgentName"/>) y llama a <c>CreateResponse</c>
/// (y su variante en streaming), devolviendo la respuesta en los DTOs de aplicación.
/// No usa <c>PersistentAgentsClient</c> ni <c>AgentId</c>.
/// </summary>
public sealed class AgentsAiFoundryAgentService : IAiFoundryAgentService
{
    private readonly AIProjectClient _projectClient;
    private readonly FoundryOptions _options;
    private readonly ILogger<AgentsAiFoundryAgentService> _logger;

    /// <summary>Crea el servicio con el cliente y opciones inyectadas.</summary>
    public AgentsAiFoundryAgentService(
        AiFoundryCredentialFactory factory,
        IOptions<FoundryOptions> options,
        ILogger<AgentsAiFoundryAgentService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _projectClient = factory.CreateProjectClient();
    }

    /// <inheritdoc/>
    public async Task<FoundryResponse> AskAsync(FoundryRequest request, CancellationToken ct = default)
    {
        var timeoutSeconds = Math.Clamp(request.TimeoutSeconds ?? _options.TimeoutSeconds, 1, 300);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var responseClient = _projectClient.ProjectOpenAIClient
                .GetProjectResponsesClientForAgentEndpoint(request.AgentName);

            var response = await responseClient.CreateResponseAsync(
                userInputText: request.UserMessage,
                cancellationToken: cts.Token);

            return AgentMessageMapper.ToFoundryResponse(response);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new AiFoundryTimeoutException(
                $"Timeout agotado ({timeoutSeconds}s) esperando al agente.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al llamar al agente {AgentName}.", request.AgentName);
            throw AgentExceptionMapper.Map(ex);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatStreamChunk> AskStreamAsync(
        FoundryRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var timeoutSeconds = Math.Clamp(request.TimeoutSeconds ?? _options.TimeoutSeconds, 1, 300);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        // El cliente se obtiene sin await; CreateResponseStreamingAsync devuelve un
        // AsyncCollectionResult (IAsyncEnumerable) cuyo streaming real ocurre al enumerar.
        System.ClientModel.AsyncCollectionResult<StreamingResponseUpdate> stream;
        try
        {
            var responseClient = _projectClient.ProjectOpenAIClient
                .GetProjectResponsesClientForAgentEndpoint(request.AgentName);

            stream = responseClient.CreateResponseStreamingAsync(
                userInputText: request.UserMessage,
                previousResponseId: null,
                cancellationToken: cts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al iniciar stream del agente {AgentName}.", request.AgentName);
            throw AgentExceptionMapper.Map(ex);
        }

        // Se enumera manualmente para poder envolver MoveNextAsync en try/catch y mapear errores
        // (no se puede hacer yield dentro de un try con catch: restricción del lenguaje).
        await using var enumerator = stream.GetAsyncEnumerator(cts.Token);
        var hasMore = true;
        while (hasMore)
        {
            try
            {
                hasMore = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new AiFoundryTimeoutException(
                    $"Timeout agotado ({timeoutSeconds}s) esperando al agente.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en stream del agente {AgentName}.", request.AgentName);
                throw AgentExceptionMapper.Map(ex);
            }

            if (!hasMore)
            {
                break;
            }

            // Serializa el update a JSON (API pública del SDK) y extrae el delta de texto.
            var updateJson = ModelReaderWriter.Write(enumerator.Current);
            if (AgentMessageMapper.TryGetOutputDelta(updateJson, out var delta)
                && !string.IsNullOrEmpty(delta))
            {
                yield return AgentMessageMapper.ToStreamChunk(delta, null, isFinal: false, FinishReason.Stop);
            }
        }

        yield return AgentMessageMapper.ToStreamChunk(string.Empty, null, isFinal: true, FinishReason.Stop);
    }
}