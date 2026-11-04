using LinkedInTutorAI.Application.Dtos;

namespace LinkedInTutorAI.Application.Abstractions;

/// <summary>
/// Contrato del servicio que dialoga con el agente de Azure AI Foundry.
/// Declarado en <c>Application</c> e implementado en <c>Infrastructure</c> con la implementación
/// oficial <c>Azure.AI.Projects</c> + <c>Azure.AI.Extensions.OpenAI</c> (Responses API),
/// de modo que la capa de aplicación no acopla a los tipos de Azure.
/// El agente ya existe en Foundry; este servicio obtiene el cliente de respuestas
/// para el agente y llama a <c>CreateResponse</c> con <see cref="FoundryRequest.UserMessage"/>.
/// </summary>
public interface IAiFoundryAgentService
{
    /// <summary>
    /// Envía la solicitud al agente y devuelve la respuesta completa (modo no-streaming).
    /// </summary>
    /// <param name="request">Solicitud ya mapeada a <see cref="FoundryRequest"/>.</param>
    /// <param name="ct">Token de cancelación; aborta el run de Foundry si se cancela.</param>
    /// <returns>Respuesta del agente.</returns>
    Task<FoundryResponse> AskAsync(FoundryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Envía la solicitud al agente y emite los deltas de la respuesta a medida que se generan
    /// (modo streaming). Cada <see cref="string"/> es un fragmento incremental de texto.
    /// </summary>
    /// <param name="request">Solicitud ya mapeada a <see cref="FoundryRequest"/>.</param>
    /// <param name="ct">Token de cancelación; detiene la enumeración y el run al cancelarse.</param>
    /// <returns>Secuencia asincrónica de <see cref="ChatStreamChunk"/> (deltas de texto + thread id).</returns>
    IAsyncEnumerable<ChatStreamChunk> AskStreamAsync(FoundryRequest request, CancellationToken ct = default);
}