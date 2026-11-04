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

    /// <summary>Obtiene una sesión por identificador; <see langword="null"/> si no existe.</summary>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La sesión, o <see langword="null"/>.</returns>
    Task<TutorSession?> GetAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Obtiene el historial de mensajes de una sesión.</summary>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de mensajes de la sesión.</returns>
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid sessionId, CancellationToken ct = default);
}