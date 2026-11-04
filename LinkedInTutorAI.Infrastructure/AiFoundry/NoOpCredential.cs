using Azure.Core;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// <see cref="TokenCredential"/> que no produce ningún token real.
/// Satisface el constructor de <c>AIProjectClient</c> (que exige un <c>AuthenticationTokenProvider</c>)
/// sin depender de Microsoft Entra ni de <c>az login</c>. La autenticación real la
/// aporta <see cref="ApiKeyHeaderPolicy"/> mediante la cabecera <c>api-key</c>.
/// </summary>
internal sealed class NoOpCredential : TokenCredential
{
    private static readonly AccessToken Empty = new(string.Empty, DateTimeOffset.MaxValue);

    public override AccessToken GetToken(TokenRequestContext context, CancellationToken cancellationToken) => Empty;

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken cancellationToken)
        => new(Empty);
}