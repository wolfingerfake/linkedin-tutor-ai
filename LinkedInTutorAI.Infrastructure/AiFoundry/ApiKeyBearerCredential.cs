using Azure.Core;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// <see cref="TokenCredential"/> que envuelve una clave de API de Azure AI Foundry
/// como token Bearer. Habilita el modo <see cref="LinkedInTutorAI.Domain.Enums.AuthMode.ApiKey"/>
/// </summary>
internal sealed class ApiKeyBearerCredential : TokenCredential
{
    private readonly string _apiKey;

    public ApiKeyBearerCredential(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKey = apiKey;
    }

    public override AccessToken GetToken(TokenRequestContext context, CancellationToken cancellationToken)
        => new(_apiKey, DateTimeOffset.MaxValue);

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken cancellationToken)
        => new(new AccessToken(_apiKey, DateTimeOffset.MaxValue));
}