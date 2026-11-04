using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using LinkedInTutorAI.Application.Options;
using LinkedInTutorAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Fábrica que construye un <see cref="PersistentAgentsClient"/> eligiendo la credencial
/// según <see cref="FoundryOptions.AuthMode"/>. Único punto de la aplicación que decide
/// cómo autenticarse contra Azure AI Foundry (dual: DefaultAzureCredential o ApiKeyBearerCredential).
/// </summary>
public sealed class AiFoundryCredentialFactory
{
    private readonly FoundryOptions _options;
    private readonly ILogger<AiFoundryCredentialFactory> _logger;

    /// <summary>Crea la fábrica con las opciones inyectadas.</summary>
    /// <param name="options">Opciones de Azure AI Foundry.</param>
    /// <param name="logger">Logger.</param>
    public AiFoundryCredentialFactory(IOptions<FoundryOptions> options, ILogger<AiFoundryCredentialFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Construye un <see cref="PersistentAgentsClient"/> para el endpoint configurado,
    /// usando <see cref="DefaultAzureCredential"/> (Entra) o <see cref="ApiKeyBearerCredential"/>
    /// (clave de API como Bearer) según <see cref="FoundryOptions.AuthMode"/>.
    /// </summary>
    /// <returns>Cliente del agente de Azure AI Foundry.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si <see cref="FoundryOptions.AuthMode"/> es <see cref="AuthMode.ApiKey"/>
    /// y <see cref="FoundryOptions.ApiKey"/> falta.
    /// </exception>
    public PersistentAgentsClient CreateAgentsClient()
    {
        return _options.AuthMode switch
        {
            AuthMode.ApiKey when string.IsNullOrWhiteSpace(_options.ApiKey)
                => throw new InvalidOperationException(
                    "AuthMode = ApiKey pero ApiKey no está configurado. Guárdalo en User Secrets o Key Vault."),
            AuthMode.ApiKey
                => CreateWithKey(_options.Endpoint, _options.ApiKey!),
            _ => CreateWithDefaultCredential(_options.Endpoint)
        };
    }

    private PersistentAgentsClient CreateWithDefaultCredential(string endpoint)
    {
        _logger.LogInformation(
            "Creando PersistentAgentsClient con DefaultAzureCredential para {Endpoint}.",
            endpoint);
        return new PersistentAgentsClient(endpoint, new DefaultAzureCredential());
    }

    private PersistentAgentsClient CreateWithKey(string endpoint, string apiKey)
    {
        _logger.LogInformation(
            "Creando PersistentAgentsClient con autenticación por api-key (header api-key) para {Endpoint}.",
            endpoint);
        var options = new PersistentAgentsAdministrationClientOptions();
        options.AddPolicy(new ApiKeyHeaderPolicy(apiKey), HttpPipelinePosition.PerCall);
        return new PersistentAgentsClient(endpoint, new NoOpCredential(), options);
    }
}