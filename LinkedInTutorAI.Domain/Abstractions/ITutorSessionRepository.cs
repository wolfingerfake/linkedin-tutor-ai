using LinkedInTutorAI.Domain.Entities;

namespace LinkedInTutorAI.Domain.Abstractions;

/// <summary>
/// Contrato de persistencia para sesiones de tutoría y sus mensajes.
/// Vive en <c>Domain</c> (abstracción) para que <c>Application</c> no dependa de ninguna
/// implementación concreta (DIP). La implementación (p. ej. <c>InMemoryTutorSessionRepository</c>)
/// se registra en <c>Infrastructure</c>.
/// </summary>
public interface ITutorSessionRepository
{
    /// <summary>Obtiene una sesión por identificador; <see langword="null"/> si no existe.</summary>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La sesión encontrada, o <see langword="null"/>.</returns>
    Task<TutorSession?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Lista las sesiones de un usuario, ordenadas por creación descendente.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de sesiones del usuario.</returns>
    Task<IReadOnlyList<TutorSession>> GetByUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Persiste una nueva sesión y devuelve la sesión almacenada (con su Id final).</summary>
    /// <param name="session">Sesión a crear.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La sesión creada.</returns>
    Task<TutorSession> CreateAsync(TutorSession session, CancellationToken ct = default);

    /// <summary>Añade un mensaje a una sesión existente.</summary>
    /// <param name="sessionId">Identificador de la sesión destino.</param>
    /// <param name="message">Mensaje a añadir.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task AddMessageAsync(Guid sessionId, ChatMessage message, CancellationToken ct = default);

    /// <summary>Guarda los cambios de una sesión (actualización completa).</summary>
    /// <param name="session">Sesión con los cambios a persistir.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task SaveAsync(TutorSession session, CancellationToken ct = default);
}