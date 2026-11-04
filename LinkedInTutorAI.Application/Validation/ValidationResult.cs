namespace LinkedInTutorAI.Application.Validation;

/// <summary>
/// Resultado de validar un DTO. Inmutable; los errores se exponen como lista de solo lectura.
/// </summary>
public sealed record ValidationResult
{
    /// <summary><see langword="true"/> cuando no hay errores de validación.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Lista de errores detectados. Vacía cuando <see cref="IsValid"/> es <see langword="true"/>.</summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>Construye un resultado válido (sin errores).</summary>
    public static ValidationResult Success() => new();

    /// <summary>Construye un resultado con errores.</summary>
    /// <param name="errors">Errores de validación.</param>
    public static ValidationResult Failure(params ValidationError[] errors) =>
        new() { Errors = errors.ToList() };

    /// <summary>Construye un resultado con errores a partir de una secuencia.</summary>
    /// <param name="errors">Errores de validación.</param>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) =>
        new() { Errors = errors.ToList() };

    /// <summary>Combina dos resultados en uno solo (concatena errores).</summary>
    public static ValidationResult Combine(ValidationResult first, ValidationResult second)
    {
        if (first.IsValid) return second;
        if (second.IsValid) return first;
        return new ValidationResult { Errors = first.Errors.Concat(second.Errors).ToList() };
    }

    /// <summary>Representación textual compacta de los errores.</summary>
    public override string ToString() =>
        IsValid ? "Valid" : string.Join(" | ", Errors.Select(e => $"{e.PropertyName}: {e.Message}"));
}