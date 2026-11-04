using System.ComponentModel.DataAnnotations;
using LinkedInTutorAI.Domain.Enums;
using LinkedInTutorAI.Application.Validation;
using ValidationResult = LinkedInTutorAI.Application.Validation.ValidationResult;

namespace LinkedInTutorAI.Application.Dtos;

/// <summary>
/// Solicitud enviada al agente de Azure AI Foundry.
/// Se construye a partir de un <see cref="LinkedInPostRequest"/> mediante un mapper,
/// de modo que la capa de infraestructura solo conoce este DTO y nunca el de dominio/entrada.
/// </summary>
public sealed record FoundryRequest
{
    /// <summary>Identificador del agente existente en Azure AI Foundry.</summary>
    [Required(ErrorMessage = "El AgentId es obligatorio.")]
    public required string AgentId { get; init; }

    /// <summary>
    /// Identificador del thread (conversación) en Foundry.
    /// Si es <see langword="null"/>, el servicio de infraestructura crea un thread nuevo.
    /// </summary>
    public string? ThreadId { get; init; }

    /// <summary>Mensaje/prompt que se envía al agente (ya formateado con contexto de LinkedIn).</summary>
    [Required(ErrorMessage = "El mensaje del usuario es obligatorio.")]
    [StringLength(8000, MinimumLength = 1, ErrorMessage = "El mensaje debe tener entre 1 y 8000 caracteres.")]
    public required string UserMessage { get; init; }

    /// <summary>Rol del mensaje. Por defecto, <see cref="FoundryMessageRole.User"/>.</summary>
    public FoundryMessageRole Role { get; init; } = FoundryMessageRole.User;

    /// <summary>Metadatos opcionales asociados al mensaje (perfil de LinkedIn, objetivo, etc.).</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Timeout en segundos para esta llamada concreta. Si es <see langword="null"/>,
    /// se usa <see cref="LinkedInTutorAI.Application.Options.FoundryOptions.TimeoutSeconds"/>.
    /// </summary>
    [Range(1, 300, ErrorMessage = "TimeoutSeconds debe estar entre 1 y 300.")]
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// Ejecuta la validación del DTO según sus <c>DataAnnotations</c>.
    /// </summary>
    public ValidationResult Validate() => DtoValidator.ValidateObject(this);
}