using Azure.Core;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// <see cref="TokenCredential"/> que envuelve una clave de API de Azure AI Foundry como
/// token Bearer. Permite el modo <see cref="LinkedInTutorAI.Domain.Enums.AuthMode.ApiKey"/>
/// sin depender de <c>AzureKeyCredential</c> (cabecera <c>api-key</c>), autenticando con
/// <c>Authorization: Bearer &lt;apiKey&gt;</c>, válido para los endpoints del proyecto de Foundry.
/// El token se trata como no expirable (fecha máxima) para simplificar el MVP académico.
/// </summary>
internal sealed class ApiKeyBearerCredential : TokenCredential
{
    private readonly string _apiKey;

    /// <summary>Crea la credencial con la clave de API.</summary>
    /// <param name="apiKey">Clave de API de Azure AI Foundry.</param>
    public ApiKeyBearerCredential(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKey = apiKey;
    }

    /// <inheritdoc/>
    public override AccessToken GetToken(TokenRequestContext context, CancellationToken cancellationToken)
        => new(_apiKey, DateTimeOffset.MaxValue);

    /// <inheritdoc/>
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken cancellationToken)
        => new(new AccessToken(_apiKey, DateTimeOffset.MaxValue));
}