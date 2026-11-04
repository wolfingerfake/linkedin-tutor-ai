using LinkedInTutorAI.Domain.Enums;

namespace LinkedInTutorAI.Application.Dtos;

/// <summary>
/// Respuesta con la publicación de LinkedIn generada por el agente.
/// Es un DTO de salida: no requiere validación.
/// </summary>
public sealed record LinkedInPostResponse
{
    /// <summary>Identificador de la sesión a la que pertenece el post generado.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Título sugerido para el post (encabezado, gancho).</summary>
    public string? Title { get; init; }

    /// <summary>Texto del post generado.</summary>
    public required string GeneratedPost { get; init; }

    /// <summary>Sugerencias de mejora o acompañamiento (hook, CTA, estructura, etc.).</summary>
    public IReadOnlyList<string> Suggestions { get; init; } = [];

    /// <summary>Hashtags sugeridos para el post.</summary>
    public IReadOnlyList<string> Hashtags { get; init; } = [];

    /// <summary>Versión alternativa más corta del post (opcional).</summary>
    public string? AlternativeVersion { get; init; }

    /// <summary>Motivo por el que finalizó la generación.</summary>
    public FinishReason FinishReason { get; init; } = FinishReason.Stop;

    /// <summary>Tokens de entrada consumidos (observabilidad/coste).</summary>
    public int? InputTokens { get; init; }

    /// <summary>Tokens de salida consumidos.</summary>
    public int? OutputTokens { get; init; }

    /// <summary>Momento (UTC) en que se generó la respuesta.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}