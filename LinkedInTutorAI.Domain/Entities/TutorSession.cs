namespace LinkedInTutorAI.Domain.Entities;

/// <summary>
/// Sesión de tutoría: agrupa la conversación entre el usuario y el agente de LinkedIn.
/// Raíz de agregado; contiene su historial de <see cref="ChatMessage"/>.
/// </summary>
public sealed record TutorSession
{
    /// <summary>Identificador único de la sesión.</summary>
    public required Guid Id { get; init; }

    /// <summary>Identificador del usuario propietario.</summary>
    public required string UserId { get; init; }

    /// <summary>Título descriptivo de la sesión (p. ej. objetivo del post).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Momento (UTC) de creación de la sesión.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Identificador del thread en Azure AI Foundry asociado a esta sesión.
    /// Permite continuar la conversación con el agente. <see langword="null"/> hasta el primer mensaje.
    /// </summary>
    public string? FoundryThreadId { get; init; }

    /// <summary>Historial de mensajes de la sesión (solo lectura).</summary>
    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];
}