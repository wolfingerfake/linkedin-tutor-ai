namespace LinkedInTutorAI.Application.Validation;

/// <summary>
/// Representa un único error de validación sobre una propiedad de un DTO.
/// </summary>
public sealed record ValidationError
{
    /// <summary>Nombre de la propiedad que falló la validación.</summary>
    public required string PropertyName { get; init; }

    /// <summary>Mensaje descriptivo del error, orientado al usuario.</summary>
    public required string Message { get; init; }

    /// <summary>Código de error opcional para mapeo/internacionalización (p. ej. "DATAANNOTATION", "AUTH").</summary>
    public string? Code { get; init; }

    /// <summary>Crea un <see cref="ValidationError"/> de forma concisa.</summary>
    /// <param name="propertyName">Propiedad que falló.</param>
    /// <param name="message">Mensaje del error.</param>
    /// <param name="code">Código opcional.</param>
    public static ValidationError Create(string propertyName, string message, string? code = null) =>
        new() { PropertyName = propertyName, Message = message, Code = code };
}