using LinkedInTutorAI.Application.Validation;

namespace LinkedInTutorAI.Application.Exceptions;

/// <summary>
/// Se lanza cuando un DTO de entrada no pasa la validación.
/// Transporta el <see cref="ValidationResult"/> con los errores detallados.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Resultado de validación con los errores que motivaron la excepción.</summary>
    public ValidationResult ValidationResult { get; }

    /// <summary>Crea una <see cref="ValidationException"/>.</summary>
    /// <param name="validationResult">Resultado de validación fallido.</param>
    public ValidationException(ValidationResult validationResult)
        : base("La solicitud no es válida: " + validationResult)
    {
        ValidationResult = validationResult;
    }
}