using System.Runtime.CompilerServices;
using System.Text;
using LinkedInTutorAI.Application.Abstractions;
using LinkedInTutorAI.Application.Dtos;
using LinkedInTutorAI.Application.Exceptions;
using LinkedInTutorAI.Application.Mappings;
using LinkedInTutorAI.Application.Options;
using LinkedInTutorAI.Domain.Abstractions;
using LinkedInTutorAI.Domain.Entities;
using LinkedInTutorAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkedInTutorAI.Application.Orchestrators;

/// <summary>
/// Implementa <see cref="IChatOrchestrator"/>: caso de uso central de generación de posts.
/// Valida la solicitud, resuelve/crea la sesión (con propiedad del usuario), mapea a
/// <see cref="FoundryRequest"/>, llama a <see cref="IAiFoundryAgentService"/>, persiste los
/// mensajes y el <see cref="TutorSession.FoundryThreadId"/>, y mapea la respuesta a
/// <see cref="LinkedInPostResponse"/>. No conoce Azure ni el SDK.
/// </summary>
public sealed class ChatOrchestrator : IChatOrchestrator
{
    private readonly IAiFoundryAgentService _agentService;
    private readonly ITutorSessionRepository _sessionRepository;
    private readonly FoundryOptions _options;
    private readonly ILogger<ChatOrchestrator> _logger;

    /// <summary>Crea el orquestador con sus dependencias inyectadas.</summary>
    /// <param name="agentService">Servicio del agente de Azure AI Foundry.</param>
    /// <param name="sessionRepository">Repositorio de sesiones (abstracción de dominio).</param>
    /// <param name="options">Opciones de Azure AI Foundry.</param>
    /// <param name="logger">Logger.</param>
    public ChatOrchestrator(
        IAiFoundryAgentService agentService,
        ITutorSessionRepository sessionRepository,
        IOptions<FoundryOptions> options,
        ILogger<ChatOrchestrator> logger)
    {
        _agentService = agentService;
        _sessionRepository = sessionRepository;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<LinkedInPostResponse> GeneratePostAsync(
        LinkedInPostRequest request,
        string userId,
        CancellationToken ct = default)
    {
        EnsureUserId(userId);
        ValidateRequest(request);

        var session = await ResolveSessionAsync(request, userId, ct);
        var foundryRequest = PostRequestMapper.ToFoundryRequest(request, _options);
        ValidateFoundryRequest(foundryRequest);

        var foundryResponse = await _agentService.AskAsync(foundryRequest, ct);

        await PersistAssistantMessageAsync(session, foundryResponse.AssistantMessage, foundryResponse.ThreadId, ct);

        _logger.LogInformation(
            "Post generado para sesión {SessionId} (thread {ThreadId}).",
            session.Id, foundryResponse.ThreadId ?? session.FoundryThreadId);

        return PostResponseParser.Parse(foundryResponse.AssistantMessage, session.Id) with
        {
            FinishReason = foundryResponse.FinishReason,
            InputTokens = foundryResponse.InputTokens,
            OutputTokens = foundryResponse.OutputTokens
        };
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatStreamChunk> GeneratePostStreamAsync(
        LinkedInPostRequest request,
        string userId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        EnsureUserId(userId);
        ValidateRequest(request);

        var session = await ResolveSessionAsync(request, userId, ct);
        var foundryRequest = PostRequestMapper.ToFoundryRequest(request, _options);
        ValidateFoundryRequest(foundryRequest);

        var fullText = new StringBuilder();
        string? threadId = session.FoundryThreadId;

        await foreach (var chunk in _agentService.AskStreamAsync(foundryRequest, ct).WithCancellation(ct))
        {
            if (chunk.ThreadId is not null)
            {
                threadId = chunk.ThreadId;
            }

            if (chunk.IsFinal)
            {
                continue; // no reenviamos el final del agente: emitiremos el nuestro tras persistir
            }

            if (!string.IsNullOrEmpty(chunk.Delta))
            {
                fullText.Append(chunk.Delta);
            }

            yield return chunk;
        }

        // Persiste el mensaje del asistente y el thread id al terminar el stream.
        await PersistAssistantMessageAsync(session, fullText.ToString(), threadId, ct);

        var response = PostResponseParser.Parse(fullText.ToString(), session.Id) with
        {
            FinishReason = FinishReason.Stop
        };

        yield return new ChatStreamChunk
        {
            Delta = string.Empty,
            ThreadId = threadId,
            IsFinal = true,
            FinishReason = response.FinishReason,
            Response = response
        };

        _logger.LogInformation(
            "Post (streaming) completado para sesión {SessionId} (thread {ThreadId}).",
            session.Id, threadId ?? session.FoundryThreadId);
    }

    /// <summary>
    /// Garantiza que <paramref name="userId"/> no esté vacío: el orquestador nunca debe aceptar un
    /// usuario sin autenticar (mitiga el bypass de autorización). El <paramref name="userId"/>
    /// debe derivarse server-side del principal autenticado en el extremo que llama (p. ej.
    /// <c>HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)</c>); el wiring de auth/Authorize
    /// corresponde a la capa Web, pendiente.
    /// </summary>
    private static void EnsureUserId(string userId)
        => ArgumentException.ThrowIfNullOrWhiteSpace(userId);

    /// <summary>Valida el DTO de entrada y lanza <see cref="ValidationException"/> si no es válido.</summary>
    private static void ValidateRequest(LinkedInPostRequest request)
    {
        var result = request.Validate();
        if (!result.IsValid)
        {
            throw new ValidationException(result);
        }
    }

    /// <summary>Valida el <see cref="FoundryRequest"/> mapeado y lanza <see cref="ValidationException"/> si no es válido.</summary>
    private static void ValidateFoundryRequest(FoundryRequest request)
    {
        var result = request.Validate();
        if (!result.IsValid)
        {
            throw new ValidationException(result);
        }
    }

    /// <summary>
    /// Resuelve una sesión existente por <see cref="LinkedInPostRequest.SessionId"/> verificando propiedad,
    /// o crea una nueva si no existe o es ajena.
    /// </summary>
    private async Task<TutorSession> ResolveSessionAsync(LinkedInPostRequest request, string userId, CancellationToken ct)
    {
        if (request.SessionId is Guid sessionId)
        {
            var existing = await _sessionRepository.GetByIdAsync(sessionId, userId, ct);
            if (existing is not null)
            {
                return existing;
            }

            _logger.LogInformation("Sesión {SessionId} no encontrada o ajena; se crea una nueva.", sessionId);
        }

        var session = await _sessionRepository.CreateAsync(new TutorSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = TruncateTitle(request.PostDraft)
        }, ct);

        return session;
    }

    /// <summary>Persiste el mensaje del asistente y, si procede, el <see cref="TutorSession.FoundryThreadId"/>.</summary>
    private async Task PersistAssistantMessageAsync(
        TutorSession session,
        string assistantContent,
        string? threadId,
        CancellationToken ct)
    {
        await _sessionRepository.AddMessageAsync(session.Id, new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = AuthorRole.Assistant,
            Content = assistantContent
        }, ct);

        if (threadId is not null && session.FoundryThreadId != threadId)
        {
            await _sessionRepository.SaveAsync(session with { FoundryThreadId = threadId }, ct);
        }
    }

    /// <summary>Recorta un borrador a un título de sesión legible (máx. 60 caracteres).</summary>
    private static string TruncateTitle(string draft) =>
        draft.Length <= 60 ? draft : draft[..60] + "…";
}