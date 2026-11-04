using System.ComponentModel.DataAnnotations;
using linkedin_tutor_ai.Models.Enums;
using linkedin_tutor_ai.Validation;
using ValidationResult = linkedin_tutor_ai.Validation.ValidationResult;

namespace linkedin_tutor_ai.Options;

/// <summary>
/// Opciones tipadas y validadas para la conexión con Azure AI Foundry.
/// Se enlazan desde la sección <c>AiFoundry</c> de <c>appsettings.json</c> mediante
/// <c>IOptions&lt;FoundryOptions&gt;</c> y <c>ValidateOnStart</c> (fail-fast).
/// </summary>
public sealed record FoundryOptions
{
    /// <summary>Nombre de la sección de configuración de la que se bindean estas opciones.</summary>
    public const string SectionName = "AiFoundry";

    /// <summary>
    /// Endpoint del proyecto de Azure AI Foundry
    /// (p. ej. <c>https://&lt;foundry&gt;.services.ai.azure.com/api/projects/&lt;project&gt;</c>).
    /// </summary>
    [Required(ErrorMessage = "El Endpoint de Azure AI Foundry es obligatorio.")]
    [Url(ErrorMessage = "El Endpoint debe ser una URL absoluta válida.")]
    public required string Endpoint { get; init; }

    /// <summary>Identificador del agente existente en Azure AI Foundry.</summary>
    [Required(ErrorMessage = "El AgentId es obligatorio.")]
    public required string AgentId { get; init; }

    /// <summary>Nombre del despliegue del modelo que usa el agente (p. ej. <c>gpt-4o-mini</c>).</summary>
    [Required(ErrorMessage = "El ModelDeployment es obligatorio.")]
    public required string ModelDeployment { get; init; }

    /// <summary>Modo de autenticación contra el servicio. Por defecto, <see cref="AuthMode.DefaultCredential"/>.</summary>
    public AuthMode AuthMode { get; init; } = AuthMode.DefaultCredential;

    /// <summary>
    /// Clave de API. Solo se utiliza cuando <see cref="AuthMode"/> = <see cref="AuthMode.ApiKey"/>.
    /// Almacénala en User Secrets (dev) o Azure Key Vault (prod); nunca la confirmes en git.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>Número máximo de reintentos ante 429/5xx (resiliencia con Polly).</summary>
    [Range(0, 10, ErrorMessage = "MaxRetries debe estar entre 0 y 10.")]
    public int MaxRetries { get; init; } = 3;

    /// <summary>Timeout por llamada al agente, en segundos.</summary>
    [Range(1, 600, ErrorMessage = "TimeoutSeconds debe estar entre 1 y 600.")]
    public int TimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Valida las opciones: primero las <c>DataAnnotations</c> y luego las reglas condicionales
    /// (ApiKey obligatoria cuando <see cref="AuthMode"/> = <see cref="AuthMode.ApiKey"/>).
    /// </summary>
    public ValidationResult Validate()
    {
        var baseResult = DtoValidator.ValidateObject(this);

        var extra = new List<ValidationError>();

        if (AuthMode == AuthMode.ApiKey && string.IsNullOrWhiteSpace(ApiKey))
        {
            extra.Add(new()
            {
                PropertyName = nameof(ApiKey),
                Message = "ApiKey es obligatorio cuando AuthMode = ApiKey.",
                Code = "AUTH"
            });
        }

        return extra.Count == 0
            ? baseResult
            : ValidationResult.Combine(baseResult, ValidationResult.Failure(extra));
    }
}