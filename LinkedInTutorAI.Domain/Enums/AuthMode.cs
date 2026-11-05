namespace LinkedInTutorAI.Domain.Enums;

/// <summary>
/// Modo de autenticación contra Azure AI Foundry.
/// Controla cómo <c>AiFoundryCredentialFactory</c> obtiene la credencial en runtime.
/// </summary>
public enum AuthMode
{
    /// <summary>
    /// Autenticación basada en identidad (Managed Identity, Azure CLI, Visual Studio, etc.).
    /// Best practice: sin secretos en código ni en configuración.
    /// </summary>
    DefaultCredential = 0,

    /// <summary>
    /// Autenticación mediante clave de API (<c>AzureKeyCredential</c>).
    /// Útil para entornos académicos o locales sin identidad gestionada.
    /// Requiere <see cref="LinkedInTutorAI.Application.Options.FoundryOptions.ApiKey"/>.
    /// </summary>
    ApiKey = 1,

    /// <summary>
    /// Autenticación con un service principal de Microsoft Entra ID
    /// (<c>ClientSecretCredential</c>): identidad no interactiva "tipo clave"
    /// (tenantId + clientId + clientSecret) adecuada para producción/despliegue.
    /// Requiere <see cref="LinkedInTutorAI.Application.Options.FoundryOptions.TenantId"/>,
    /// <see cref="LinkedInTutorAI.Application.Options.FoundryOptions.ClientId"/> y
    /// <see cref="LinkedInTutorAI.Application.Options.FoundryOptions.ClientSecret"/>,
    /// y que el service principal tenga el rol <c>Azure AI Developer</c> en el proyecto.
    /// </summary>
    ServicePrincipal = 2
}