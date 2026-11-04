using LinkedInTutorAI.Domain.Entities;

namespace LinkedInTutorAI.Application.Abstractions;

/// <summary>
/// Servicio de aplicación para el ciclo de vida de sesiones de tutoría.
/// Expone operaciones de sesión al orquestador y a la UI sin acoplarlos al repositorio.
/// </summary>
public interface ITutorSessionService
{
    /// <summary>Crea una nueva sesión para el usuario indicado.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="title">Título opcional de la sesión.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La sesión creada.</returns>
    Task<TutorSession> CreateAsync(string userId, string? title = null, CancellationToken ct = default);

    /// <summary>
    /// Obtiene una sesión por identificador; <see langword="null"/> si no existe <em>o no pertenece al usuario</em>.
    /// La comprobación de propiedad se delega al repositorio para mitigar IDOR.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <param name="userId">Identificador del usuario solicitante (propietario).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La sesión si existe y es del usuario; en caso contrario, <see langword="null"/>.</returns>
    Task<TutorSession?> GetAsync(Guid sessionId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el historial de mensajes de una sesión; <see langword="null"/> o vacío si no existe o es ajena.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <param name="userId">Identificador del usuario solicitante (propietario).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de mensajes de la sesión.</returns>
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid sessionId, string userId, CancellationToken ct = default);
}