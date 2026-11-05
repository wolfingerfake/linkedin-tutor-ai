using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using LinkedInTutorAI.Application.Options;
using LinkedInTutorAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Fábrica que construye un <see cref="AIProjectClient"/> eligiendo la credencial
/// según <see cref="FoundryOptions.AuthMode"/>. Único punto que decide cómo autenticarse
/// contra Azure AI Foundry (triple: DefaultAzureCredential, service principal
/// con <c>ClientSecretCredential</c>, o cabecera <c>api-key</c>).
/// </summary>
public sealed class AiFoundryCredentialFactory
{
    private readonly FoundryOptions _options;
    private readonly ILogger<AiFoundryCredentialFactory> _logger;

    /// <summary>Crea la fábrica con las opciones inyectadas.</summary>
    public AiFoundryCredentialFactory(IOptions<FoundryOptions> options, ILogger<AiFoundryCredentialFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Construye un <see cref="AIProjectClient"/> para el endpoint configurado,
    /// usando <see cref="DefaultAzureCredential"/> (Entra) o la cabecera <c>api-key</c>
    /// (clave de API) según <see cref="FoundryOptions.AuthMode"/>.
    /// </summary>
    public AIProjectClient CreateProjectClient()
    {
        var endpoint = new Uri(_options.Endpoint);

        return _options.AuthMode switch
        {
            AuthMode.ApiKey when string.IsNullOrWhiteSpace(_options.ApiKey)
                => throw new InvalidOperationException(
                    "AuthMode = ApiKey pero ApiKey no está configurado. Guárdalo en User Secrets o Key Vault."),
            AuthMode.ApiKey
                => CreateWithKey(endpoint, _options.ApiKey!),
            AuthMode.ServicePrincipal
                => CreateWithServicePrincipal(endpoint),
            _ => CreateWithDefaultCredential(endpoint)
        };
    }

    private AIProjectClient CreateWithDefaultCredential(Uri endpoint)
    {
        _logger.LogInformation(
            "Creando AIProjectClient con DefaultAzureCredential para {Endpoint}.",
            endpoint);
        return new AIProjectClient(endpoint, new DefaultAzureCredential());
    }

    private AIProjectClient CreateWithServicePrincipal(Uri endpoint)
    {
        if (string.IsNullOrWhiteSpace(_options.TenantId)
            || string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                "AuthMode = ServicePrincipal pero TenantId/ClientId/ClientSecret no están configurados. " +
                "Guárdalos en User Secrets o Key Vault.");
        }

        _logger.LogInformation(
            "Creando AIProjectClient con ClientSecretCredential (tenant {TenantId}, clientId {ClientId}) para {Endpoint}.",
            _options.TenantId, _options.ClientId, endpoint);
        var credential = new ClientSecretCredential(
            _options.TenantId!, _options.ClientId!, _options.ClientSecret!);
        return new AIProjectClient(endpoint, credential);
    }

    private AIProjectClient CreateWithKey(Uri endpoint, string apiKey)
    {
        _logger.LogInformation(
            "Creando AIProjectClient con cabecera api-key para {Endpoint}.",
            endpoint);
        var options = new AIProjectClientOptions();
        options.AddPolicy(new ApiKeyHeaderPolicy(apiKey), PipelinePosition.PerCall);
        return new AIProjectClient(endpoint, new NoOpCredential(), options);
    }
}