using System.ClientModel.Primitives;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Política de pipeline (<c>System.ClientModel</c>) que inyecta la cabecera <c>api-key</c>
/// en cada petición al agente de Azure AI Foundry. Así se autentica por clave de API
/// (cabecera <c>api-key</c>, no Bearer), que es lo que esperan los endpoints de Foundry.
/// </summary>
internal sealed class ApiKeyHeaderPolicy : PipelinePolicy
{
    private const string ApiKeyHeader = "api-key";
    private readonly string _apiKey;

    public ApiKeyHeaderPolicy(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKey = apiKey;
    }

    public override void Process(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int messageIndex)
    {
        message.Request.Headers.Add(ApiKeyHeader, _apiKey);
        ProcessNext(message, pipeline, messageIndex);
    }

    public override async ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int messageIndex)
    {
        message.Request.Headers.Add(ApiKeyHeader, _apiKey);
        await ProcessNextAsync(message, pipeline, messageIndex);
    }
}