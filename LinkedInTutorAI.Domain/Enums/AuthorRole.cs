namespace LinkedInTutorAI.Domain.Enums;

/// <summary>
/// Autor de un mensaje dentro de una sesión de tutoría.
/// </summary>
public enum AuthorRole
{
    /// <summary>Mensaje escrito por el usuario.</summary>
    User = 0,

    /// <summary>Respuesta generada por el agente (asistente/tutor).</summary>
    Assistant = 1
}