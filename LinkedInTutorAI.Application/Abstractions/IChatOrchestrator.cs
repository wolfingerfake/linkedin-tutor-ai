using LinkedInTutorAI.Application.Dtos;

namespace LinkedInTutorAI.Application.Abstractions;

/// <summary>
/// Caso de uso central: orquesta la generación de una publicación de LinkedIn.
/// Valida la <see cref="LinkedInPostRequest"/>, gestiona la sesión (carga/crea, persiste
/// mensajes), mapea a <see cref="FoundryRequest"/> y llama a <see cref="IAiFoundryAgentService"/>,
 /// finalmente mapea la <see cref="FoundryResponse"/> a <see cref="LinkedInPostResponse"/>.
/// Es el único componente que conoce el flujo completo; no conoce Azure.
/// </summary>
public interface IChatOrchestrator
{
    /// <summary>
    /// Genera una publicación de LinkedIn y devuelve la respuesta completa (modo no-streaming).
    /// </summary>
    /// <param name="request">Solicitud del usuario (borrador, objetivo, tono, idioma, etc.).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El post generado, con sugerencias y hashtags.</returns>
    Task<LinkedInPostResponse> GeneratePostAsync(LinkedInPostRequest request, CancellationToken ct = default);

    /// <summary>
    /// Genera una publicación emitiendo los deltas de texto a medida que el agente los produce
    /// (modo streaming). El llamador ensambla el post final a partir de los deltas.
    /// </summary>
    /// <param name="request">Solicitud del usuario.</param>
    /// <param name="ct">Token de cancelación; detiene la generación al cancelarse.</param>
    /// <returns>Secuencia asincrónica de deltas de texto del post.</returns>
    IAsyncEnumerable<string> GeneratePostStreamAsync(LinkedInPostRequest request, CancellationToken ct = default);
}