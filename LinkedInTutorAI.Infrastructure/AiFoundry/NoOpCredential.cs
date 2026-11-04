using Azure.Core;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// <see cref="TokenCredential"/> que no produce ningún token real.
/// Sirve para satisfacer el constructor de <see cref="global::Azure.AI.Agents.Persistent.PersistentAgentsClient"/>
/// (que exige un <c>TokenCredential</c>) sin depender de Microsoft Entra ni de
/// <c>az login</c>. La autenticación real la aporta <see cref="ApiKeyHeaderPolicy"/>
/// mediante la cabecera <c>api-key</c>.
/// </summary>
internal sealed class NoOpCredential : TokenCredential
{
    private static readonly AccessToken Empty = new(string.Empty, DateTimeOffset.MaxValue);

    /// <inheritdoc/>
    public override AccessToken GetToken(TokenRequestContext context, CancellationToken cancellationToken) => Empty;

    /// <inheritdoc/>
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken cancellationToken)
        => new(Empty);
}