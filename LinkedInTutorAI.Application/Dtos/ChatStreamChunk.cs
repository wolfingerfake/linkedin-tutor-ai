using LinkedInTutorAI.Domain.Enums;

namespace LinkedInTutorAI.Application.Dtos;

/// <summary>
/// Fragmento incremental emitido por el agente en modo streaming.
/// Permite devolver, además del delta de texto, el <see cref="ThreadId"/> (para continuar la
/// conversación) y la señal de fin (<see cref="IsFinal"/>).
/// </summary>
public sealed record ChatStreamChunk
{
    /// <summary>Delta de texto generado en este fragmento (puede ser vacío en chunks de control).</summary>
    public required string Delta { get; init; }

    /// <summary>
    /// Identificador del thread de Foundry al que pertenece la conversación.
    /// Se propaga sobre todo el stream para que el orquestador pueda persistirlo.
    /// </summary>
    public string? ThreadId { get; init; }

    /// <summary><see langword="true"/> en el último chunk del stream.</summary>
    public bool IsFinal { get; init; }

    /// <summary>Motivo de finalización (informativo; solo relevante si <see cref="IsFinal"/>).</summary>
    public FinishReason FinishReason { get; init; } = FinishReason.Stop;
}