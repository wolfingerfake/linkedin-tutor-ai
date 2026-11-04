using System.ComponentModel.DataAnnotations;

namespace LinkedInTutorAI.Application.Validation;

/// <summary>
/// Helper estático que ejecuta la validación por <c>DataAnnotations</c> sobre un objeto
/// y devuelve un <see cref="ValidationResult"/> propio de la capa de aplicación.
/// Aísla el uso de <see cref="Validator"/> para que los DTOs no repitan lógica de validación.
/// </summary>
public static class DtoValidator
{
    /// <summary>
    /// Valida todas las propiedades del objeto usando sus <c>DataAnnotations</c>.
    /// </summary>
    /// <param name="instance">Instancia a validar.</param>
    /// <returns>Un <see cref="ValidationResult"/> con los errores encontrados.</returns>
    public static ValidationResult ValidateObject(object instance)
    {
        var annotationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new ValidationContext(instance);

        if (Validator.TryValidateObject(instance, context, annotationResults, validateAllProperties: true))
            return ValidationResult.Success();

        var errors = annotationResults.Select(r => new ValidationError
        {
            PropertyName = r.MemberNames.FirstOrDefault() ?? string.Empty,
            Message = r.ErrorMessage ?? "Valor no válido.",
            Code = "DATAANNOTATION"
        });

        return ValidationResult.Failure(errors);
    }
}