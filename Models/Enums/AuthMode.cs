namespace linkedin_tutor_ai.Models.Enums;

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
    /// Requiere <see cref="linkedin_tutor_ai.Options.FoundryOptions.ApiKey"/>.
    /// </summary>
    ApiKey = 1
}