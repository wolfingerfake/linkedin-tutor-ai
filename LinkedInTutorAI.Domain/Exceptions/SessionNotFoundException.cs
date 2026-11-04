namespace LinkedInTutorAI.Domain.Exceptions;

/// <summary>
/// Se lanza cuando no existe una sesión con el identificador solicitado,
/// o cuando la sesión existe pero no pertenece al usuario solicitante (IDOR mitigado).
/// </summary>
public sealed class SessionNotFoundException : DomainException
{
    /// <summary>Identificador de sesión que no se encontró.</summary>
    public Guid SessionId { get; }

    /// <summary>Identificador del usuario solicitante, si se proporcionó.</summary>
    public string? UserId { get; }

    /// <summary>Crea una <see cref="SessionNotFoundException"/>.</summary>
    /// <param name="sessionId">Identificador de sesión no encontrada.</param>
    /// <param name="userId">Usuario solicitante (opcional).</param>
    public SessionNotFoundException(Guid sessionId, string? userId = null)
        : base(userId is null
            ? $"No se encontró la sesión '{sessionId}'."
            : $"No se encontró la sesión '{sessionId}' para el usuario '{userId}'.")
    {
        SessionId = sessionId;
        UserId = userId;
    }
}