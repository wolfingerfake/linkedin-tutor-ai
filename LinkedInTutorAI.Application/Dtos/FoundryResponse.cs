using LinkedInTutorAI.Domain.Enums;

namespace LinkedInTutorAI.Application.Dtos;

/// <summary>
/// Respuesta recibida del agente de Azure AI Foundry.
/// DTO de salida construido por <c>AgentMessageMapper</c> en la capa de infraestructura
/// a partir de los tipos del SDK de Azure.
/// </summary>
public sealed record FoundryResponse
{
    /// <summary>Identificador del thread, para reutilizar contexto en llamadas siguientes.</summary>
    public string? ThreadId { get; init; }

    /// <summary>Texto de respuesta del agente.</summary>
    public required string AssistantMessage { get; init; }

    /// <summary>Identificador del mensaje devuelto por el agente (opcional).</summary>
    public string? MessageId { get; init; }

    /// <summary>Motivo por el que finalizó el run del agente.</summary>
    public FinishReason FinishReason { get; init; } = FinishReason.Stop;

    /// <summary>Tokens de entrada consumidos.</summary>
    public int? InputTokens { get; init; }

    /// <summary>Tokens de salida consumidos.</summary>
    public int? OutputTokens { get; init; }

    /// <summary>Momento (UTC) en que se recibió la respuesta.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}