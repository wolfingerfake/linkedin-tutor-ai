using LinkedInTutorAI.Domain.Enums;

namespace LinkedInTutorAI.Domain.Entities;

/// <summary>
/// Mensaje individual dentro de una sesión de tutoría.
/// Raíz de agregado inmutable (<see cref="AuthorRole"/> distingue usuario vs. agente).
/// </summary>
public sealed record ChatMessage
{
    /// <summary>Identificador único del mensaje.</summary>
    public required Guid Id { get; init; }

    /// <summary>Identificador de la sesión a la que pertenece.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Rol del autor del mensaje.</summary>
    public required AuthorRole Role { get; init; }

    /// <summary>Contenido textual del mensaje.</summary>
    public required string Content { get; init; }

    /// <summary>Momento (UTC) en que se creó el mensaje.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}