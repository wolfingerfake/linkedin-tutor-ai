using System.ComponentModel.DataAnnotations;
using LinkedInTutorAI.Domain.Enums;

namespace LinkedInTutorAI.Application.Dtos;

/// <summary>
/// Solicitud enviada al agente de Azure AI Foundry (implementación oficial vía
/// <c>Azure.AI.Extensions.OpenAI</c>). Se construye a partir de un <see cref="LinkedInPostRequest"/>
/// mediante <c>PostRequestMapper</c>; la capa de infraestructura solo conoce este DTO.
/// </summary>
public sealed record FoundryRequest
{
    /// <summary>Nombre del agente existente en Azure AI Foundry.</summary>
    [Required(ErrorMessage = "El AgentName es obligatorio.")]
    public required string AgentName { get; init; }

    /// <summary>Mensaje/prompt que se envía al agente (ya formateado con contexto de LinkedIn).</summary>
    [Required(ErrorMessage = "El mensaje del usuario es obligatorio.")]
    [StringLength(8000, MinimumLength = 1, ErrorMessage = "El mensaje debe tener entre 1 y 8000 caracteres.")]
    public required string UserMessage { get; init; }

    /// <summary>Metadatos opcionales asociados al mensaje (contexto de LinkedIn, etc.).</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Timeout en segundos para esta llamada concreta. Si es <see langword="null"/>,
    /// se usa <see cref="LinkedInTutorAI.Application.Options.FoundryOptions.TimeoutSeconds"/>.
    /// </summary>
    [Range(1, 300, ErrorMessage = "TimeoutSeconds debe estar entre 1 y 300.")]
    public int? TimeoutSeconds { get; init; }

    /// <summary>Ejecuta la validación del DTO según sus <c>DataAnnotations</c>.</summary>
    public LinkedInTutorAI.Application.Validation.ValidationResult Validate()
        => LinkedInTutorAI.Application.Validation.DtoValidator.ValidateObject(this);
}