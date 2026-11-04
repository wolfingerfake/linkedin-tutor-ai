using System.ComponentModel.DataAnnotations;
using LinkedInTutorAI.Domain.Enums;
using LinkedInTutorAI.Application.Validation;
using ValidationResult = LinkedInTutorAI.Application.Validation.ValidationResult;

namespace LinkedInTutorAI.Application.Dtos;

/// <summary>
/// Solicitud del usuario para generar o mejorar una publicación de LinkedIn.
/// Es el DTO de entrada de la capa de aplicación; el orquestador lo traduce a un
/// <see cref="FoundryRequest"/> antes de llamar al agente.
/// </summary>
public sealed record LinkedInPostRequest
{
    /// <summary>Borrador, idea o tema base del post. Campo obligatorio.</summary>
    [Required(ErrorMessage = "El borrador del post es obligatorio.")]
    [StringLength(3000, MinimumLength = 3, ErrorMessage = "El borrador debe tener entre 3 y 3000 caracteres.")]
    public required string PostDraft { get; init; }

    /// <summary>Objetivo comunicacional del post. Por defecto, <see cref="PostGoal.IncreaseReach"/>.</summary>
    public PostGoal Goal { get; init; } = PostGoal.IncreaseReach;

    /// <summary>Tono de redacción deseado. Por defecto, <see cref="PostTone.Professional"/>.</summary>
    public PostTone Tone { get; init; } = PostTone.Professional;

    /// <summary>Audiencia objetivo del post (opcional, p. ej. "reclutadores tech").</summary>
    [StringLength(300, ErrorMessage = "La audiencia no puede superar 300 caracteres.")]
    public string? TargetAudience { get; init; }

    /// <summary>Rol o persona desde la que escribe el autor (opcional, p. ej. "reclutador", "líder técnico").</summary>
    [StringLength(120, ErrorMessage = "El rol no puede superar 120 caracteres.")]
    public string? Rol { get; init; }

    /// <summary>Tipo de publicación (opcional, p. ej. "anuncio", "historia", "consejo", "pregunta").</summary>
    [StringLength(80, ErrorMessage = "El tipo de publicación no puede superar 80 caracteres.")]
    public string? TipoPublicacion { get; init; }

    /// <summary>Si es <see langword="true"/>, se pide al agente que incluya hashtags sugeridos.</summary>
    public bool IncluirHashtags { get; init; } = true;

    /// <summary>Si es <see langword="true"/>, se pide al agente que incluya una pregunta final (CTA conversacional).</summary>
    public bool IncluirPreguntaFinal { get; init; } = true;

    /// <summary>Longitud máxima del post generado. LinkedIn limita a 3000 caracteres.</summary>
    [Range(1, 3000, ErrorMessage = "MaxLength debe estar entre 1 y 3000 caracteres.")]
    public int MaxLength { get; init; } = 3000;

    /// <summary>Idioma del post generado (código ISO 639-1, p. ej. "es", "en").</summary>
    [Required(ErrorMessage = "El idioma es obligatorio.")]
    [StringLength(5, MinimumLength = 2, ErrorMessage = "El idioma debe ser un código ISO válido.")]
    public string Language { get; init; } = "es";

    /// <summary>Identificador de la sesión/conversación previa. Si es <see langword="null"/>, se crea una nueva.</summary>
    public Guid? SessionId { get; init; }

    /// <summary>
    /// Ejecuta la validación del DTO según sus <c>DataAnnotations</c>.
    /// </summary>
    /// <returns>Un <see cref="ValidationResult"/> con los errores encontrados.</returns>
    public ValidationResult Validate() => DtoValidator.ValidateObject(this);
}