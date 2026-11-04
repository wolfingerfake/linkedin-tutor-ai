using Azure;
using Azure.AI.Agents.Persistent;
using LinkedInTutorAI.Application.Abstractions;
using LinkedInTutorAI.Application.Dtos;
using LinkedInTutorAI.Application.Exceptions;
using LinkedInTutorAI.Application.Options;
using LinkedInTutorAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Implementa <see cref="IAiFoundryAgentService"/> sobre el SDK <c>Azure.AI.Agents.Persistent</c>.
/// Recupera/crea el thread, añade el mensaje del usuario, ejecuta el <c>run</c> contra el
/// agente existente (<see cref="FoundryOptions.AgentId"/>) y devuelve la respuesta,
/// tanto completa (<see cref="AskAsync"/>) como en streaming (<see cref="AskStreamAsync"/>).
/// Traduce los errores del SDK a excepciones de aplicación en la frontera.
/// </summary>
public sealed class AgentsAiFoundryAgentService : IAiFoundryAgentService
{
    private readonly PersistentAgentsClient _client;
    private readonly FoundryOptions _options;
    private readonly ILogger<AgentsAiFoundryAgentService> _logger;

    /// <summary>Crea el servicio con el cliente y opciones inyectadas.</summary>
    /// <param name="factory">Fábrica de <see cref="PersistentAgentsClient"/>.</param>
    /// <param name="options">Opciones de Azure AI Foundry.</param>
    /// <param name="logger">Logger.</param>
    public AgentsAiFoundryAgentService(
        AiFoundryCredentialFactory factory,
        IOptions<FoundryOptions> options,
        ILogger<AgentsAiFoundryAgentService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = factory.CreateAgentsClient();
    }

    /// <inheritdoc/>
    public async Task<FoundryResponse> AskAsync(FoundryRequest request, CancellationToken ct = default)
    {
        var threadId = request.ThreadId ?? (await _client.Threads.CreateThreadAsync()).Value.Id;

        await _client.Messages.CreateMessageAsync(
            threadId,
            MessageRole.User,
            request.UserMessage);

        var run = (await _client.Runs.CreateRunAsync(threadId, request.AgentId)).Value;

        // Polling con deadline basado en el timeout configurado (no en constante mágica).
        var timeoutSeconds = Math.Clamp(request.TimeoutSeconds ?? _options.TimeoutSeconds, 1, 300);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new AiFoundryTimeoutException(
                    $"Timeout agotado ({timeoutSeconds}s) esperando al agente en el thread '{threadId}'.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            run = (await _client.Runs.GetRunAsync(threadId, run.Id)).Value;
        }

        if (run.Status != RunStatus.Completed)
        {
            if (run.Status == RunStatus.Cancelled)
            {
                throw new OperationCanceledException("El run fue cancelado.");
            }

            throw new AiFoundryUnavailableException(
                $"El run del agente terminó con estado '{run.Status}'.");
        }

        // Recupera el último mensaje del asistente.
        var messages = _client.Messages.GetMessagesAsync(threadId, order: ListSortOrder.Descending);
        PersistentThreadMessage? assistantMessage = null;
        await foreach (var msg in messages)
        {
            if (msg.Role != MessageRole.User)
            {
                assistantMessage = msg;
                break;
            }
        }

        if (assistantMessage is null)
        {
            throw new AiFoundryException("El agente no devolvió ningún mensaje del asistente.");
        }

        return AgentMessageMapper.ToFoundryResponse(assistantMessage, threadId, run.Status);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatStreamChunk> AskStreamAsync(
        FoundryRequest request,
        CancellationToken ct = default)
    {
        var threadId = request.ThreadId;
        if (threadId is null)
        {
            var thread = await _client.Threads.CreateThreadAsync();
            threadId = thread.Value.Id;
            // Comunica el thread id recién creado al orquestador para que pueda persistirlo.
            yield return AgentMessageMapper.ToStreamChunk(
                string.Empty, threadId, isFinal: false, FinishReason.Stop);
        }

        await _client.Messages.CreateMessageAsync(
            threadId,
            MessageRole.User,
            request.UserMessage);

        // Deadline basado en el timeout configurado: el streaming no queda sin límite.
        // Nota: una expiración del deadline se propaga como OperationCanceledException
        // (limitación de los iteradores async: no se puede catch dentro de un bloque con yield).
        // La conversión a AiFoundryTimeoutException se hace en el extremo no-iterador (UI).
        var streamTimeoutSeconds = Math.Clamp(request.TimeoutSeconds ?? _options.TimeoutSeconds, 1, 300);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(streamTimeoutSeconds));

        var stream = _client.Runs.CreateRunStreamingAsync(threadId, request.AgentId);

        await foreach (var update in stream.WithCancellation(timeoutCts.Token))
        {
            if (timeoutCts.IsCancellationRequested)
            {
                throw new AiFoundryTimeoutException(
                    $"Timeout agotado ({streamTimeoutSeconds}s) en streaming del thread '{threadId}'.");
            }
            if (update is MessageContentUpdate contentUpdate)
            {
                yield return AgentMessageMapper.ToStreamChunk(
                    contentUpdate.Text, threadId, isFinal: false, FinishReason.Stop);
            }
            else if (update.UpdateKind == StreamingUpdateReason.RunCompleted)
            {
                yield return AgentMessageMapper.ToStreamChunk(
                    string.Empty, threadId, isFinal: true, FinishReason.Stop);
            }
            else if (update.UpdateKind == StreamingUpdateReason.Error)
            {
                _logger.LogError("El run en streaming falló para el thread {ThreadId}.", threadId);
                yield return AgentMessageMapper.ToStreamChunk(
                    string.Empty, threadId, isFinal: true, FinishReason.Error);
            }
        }
    }
}