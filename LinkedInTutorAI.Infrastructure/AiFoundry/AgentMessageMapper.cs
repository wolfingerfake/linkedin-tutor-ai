using Azure.AI.Extensions.OpenAI;
using LinkedInTutorAI.Application.Dtos;
using LinkedInTutorAI.Domain.Enums;
using OpenAI.Responses;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Mapper que traduce los tipos de la implementación oficial
/// <c>Azure.AI.Extensions.OpenAI</c> (<see cref="ResponseResult"/>) a los DTOs de
/// aplicación (<see cref="FoundryResponse"/>, <see cref="ChatStreamChunk"/>).
/// Aísla a la capa de aplicación de la forma del SDK.
/// </summary>
public static class AgentMessageMapper
{
    /// <summary>
    /// Mapa un <see cref="ResponseResult"/> a <see cref="FoundryResponse"/>.
    /// </summary>
    /// <param name="response">Respuesta del agente (Responses API).</param>
    /// <param name="threadId">Identificador de conversación opcional (la Responses API es stateless).</param>
    /// <returns>El <see cref="FoundryResponse"/> para la capa de aplicación.</returns>
    public static FoundryResponse ToFoundryResponse(ResponseResult response, string? threadId = null) => new()
    {
        ThreadId = threadId,
        AssistantMessage = response.GetOutputText() ?? string.Empty,
        FinishReason = FinishReason.Stop,
        CreatedAt = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// Construye un <see cref="ChatStreamChunk"/> de delta/control para el modo streaming.
    /// </summary>
    /// <param name="delta">Delta de texto (puede ser vacío en chunks de control).</param>
    /// <param name="threadId">Identificador de conversación opcional.</param>
    /// <param name="isFinal"><see langword="true"/> si es el último chunk.</param>
    /// <param name="reason">Motivo de finalización.</param>
    public static ChatStreamChunk ToStreamChunk(
        string delta,
        string? threadId,
        bool isFinal,
        FinishReason reason) => new()
    {
        Delta = delta,
        ThreadId = threadId,
        IsFinal = isFinal,
        FinishReason = reason
    };
}