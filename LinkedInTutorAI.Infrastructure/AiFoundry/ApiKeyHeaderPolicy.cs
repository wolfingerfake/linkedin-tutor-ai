using Azure.Core;
using Azure.Core.Pipeline;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Política de pipeline que inyecta la cabecera <c>api-key</c> en cada petición
/// al agente de Azure AI Foundry. Así se autentica por clave de API (en lugar
/// de Bearer/Entra), que es lo que esperan los endpoints de Foundry para claves.
/// </summary>
internal sealed class ApiKeyHeaderPolicy : HttpPipelinePolicy
{
    private const string ApiKeyHeader = "api-key";
    private readonly string _apiKey;

    /// <summary>Crea la política con la clave de API a inyectar.</summary>
    /// <param name="apiKey">Clave de API de Azure AI Foundry.</param>
    public ApiKeyHeaderPolicy(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKey = apiKey;
    }

    /// <inheritdoc/>
    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        message.Request.Headers.SetValue(ApiKeyHeader, _apiKey);
        ProcessNext(message, pipeline);
    }

    /// <inheritdoc/>
    public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        message.Request.Headers.SetValue(ApiKeyHeader, _apiKey);
        await ProcessNextAsync(message, pipeline);
    }
}