using System.Text;
using LinkedInTutorAI.Application.Dtos;
using LinkedInTutorAI.Application.Options;
using LinkedInTutorAI.Domain.Enums;

namespace LinkedInTutorAI.Application.Mappings;

/// <summary>
/// Mapper puro que traduce un <see cref="LinkedInPostRequest"/> (entrada del usuario) en un
/// <see cref="FoundryRequest"/> (mensaje para el agente de Azure AI Foundry).
/// Inserta el contexto de LinkedIn (objetivo, tono, audiencia, idioma, longitud) dentro del
/// <see cref="FoundryRequest.UserMessage"/> y lo expone también como <see cref="FoundryRequest.Metadata"/>.
/// Es estático y sin dependencias para facilitar las pruebas unitarias.
/// </summary>
public static class PostRequestMapper
{
    /// <summary>
    /// Mapa un <see cref="LinkedInPostRequest"/> a <see cref="FoundryRequest"/> usando el
    /// <see cref="FoundryOptions.AgentName"/> de la configuración.
    /// </summary>
    /// <param name="request">Solicitud de entrada del usuario.</param>
    /// <param name="options">Opciones de Azure AI Foundry (proporciona <see cref="FoundryOptions.AgentName"/>).</param>
    /// <returns>El <see cref="FoundryRequest"/> listo para enviar al agente.</returns>
    public static FoundryRequest ToFoundryRequest(
        LinkedInPostRequest request,
        FoundryOptions options)
        => ToFoundryRequest(request, options.AgentName);

    /// <summary>
    /// Mapa un <see cref="LinkedInPostRequest"/> a <see cref="FoundryRequest"/> usando un
    /// <paramref name="agentName"/> explícito. Útil para pruebas que no requieren <see cref="FoundryOptions"/>.
    /// </summary>
    /// <param name="request">Solicitud de entrada del usuario.</param>
    /// <param name="agentName">Nombre del agente existente en Azure AI Foundry.</param>
    /// <returns>El <see cref="FoundryRequest"/> listo para enviar al agente.</returns>
    public static FoundryRequest ToFoundryRequest(
        LinkedInPostRequest request,
        string agentName)
    {
        var metadata = new Dictionary<string, string>
        {
            ["goal"] = request.Goal.ToString(),
            ["tone"] = request.Tone.ToString(),
            ["language"] = request.Language,
            ["maxLength"] = request.MaxLength.ToString(),
            ["incluirHashtags"] = request.IncluirHashtags.ToString(),
            ["incluirPreguntaFinal"] = request.IncluirPreguntaFinal.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.TargetAudience))
        {
            metadata["targetAudience"] = request.TargetAudience;
        }

        if (!string.IsNullOrWhiteSpace(request.Rol))
        {
            metadata["rol"] = request.Rol;
        }

        if (!string.IsNullOrWhiteSpace(request.TipoPublicacion))
        {
            metadata["tipoPublicacion"] = request.TipoPublicacion;
        }

        return new FoundryRequest
        {
            AgentName = agentName,
            UserMessage = BuildPrompt(request),
            Metadata = metadata
        };
    }

    /// <summary>
    /// Construye el prompt final que se envía al agente, embebiendo el contexto de LinkedIn.
    /// </summary>
    /// <param name="request">Solicitud de entrada del usuario.</param>
    /// <returns>Texto del prompt listo para <see cref="FoundryRequest.UserMessage"/>.</returns>
    private static string BuildPrompt(LinkedInPostRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Actúa como tutor experto en contenido de LinkedIn.");
        sb.AppendLine("Genera una publicación lista para publicar a partir del borrador del usuario.");
        sb.AppendLine();
        sb.AppendLine($"- Objetivo: {GoalToText(request.Goal)}");
        sb.AppendLine($"- Tono: {ToneToText(request.Tone)}");
        sb.AppendLine($"- Idioma de salida: {request.Language}");
        sb.AppendLine($"- Longitud máxima: {request.MaxLength} caracteres (límite de LinkedIn: 3000)");

        if (!string.IsNullOrWhiteSpace(request.Rol))
        {
            sb.AppendLine($"- Rol/persona del autor: {request.Rol}");
        }

        if (!string.IsNullOrWhiteSpace(request.TipoPublicacion))
        {
            sb.AppendLine($"- Tipo de publicación: {request.TipoPublicacion}");
        }

        if (!string.IsNullOrWhiteSpace(request.TargetAudience))
        {
            sb.AppendLine($"- Audiencia objetivo: {request.TargetAudience}");
        }

        sb.AppendLine();
        sb.AppendLine("Borrador o idea del usuario:");
        sb.AppendLine(request.PostDraft);
        sb.AppendLine();
        sb.AppendLine("Requisitos de salida:");
        sb.AppendLine("- Estructura la respuesta con estos encabezados en mayúsculas, cada uno en su propia línea, seguido de su contenido:");
        sb.AppendLine("  TÍTULO");
        sb.AppendLine("  <título o gancho del post>");
        sb.AppendLine("  PUBLICACIÓN");
        sb.AppendLine("  <texto del post, con saltos de línea y estructura propias de LinkedIn (hook, desarrollo, CTA)>");
        if (request.IncluirPreguntaFinal)
        {
            sb.AppendLine("  Termina la PUBLICACIÓN con una pregunta final que invite a comentar.");
        }
        if (request.IncluirHashtags)
        {
            sb.AppendLine("  HASHTAGS");
            sb.AppendLine("  <una línea con 3-6 hashtags relevantes, p. ej. #MicrosoftFoundry #AzureAI>");
        }
        sb.AppendLine("  RECOMENDACIONES");
        sb.AppendLine("  <una recomendación por línea>");
        sb.AppendLine("  RESPONIBLE AI");
        sb.AppendLine("  <una nota breve sobre uso responsabile del contenido>");
        sb.AppendLine("- No superes la longitud máxima indicada.");
        sb.AppendLine("- No añadas explicaciones, texto introductorio ni markdown: solo los encabezados y su contenido.");

        return sb.ToString();
    }

    private static string GoalToText(PostGoal goal) => goal switch
    {
        PostGoal.IncreaseReach => "Maximizar alcance e impresiones",
        PostGoal.GetConnections => "Conseguir nuevas conexiones",
        PostGoal.FindJob => "Posicionarse para oportunidades laborales",
        PostGoal.BuildPersonalBrand => "Construir marca personal y autoridad",
        PostGoal.ShareKnowledge => "Compartir conocimiento o aprendizajes",
        _ => goal.ToString()
    };

    private static string ToneToText(PostTone tone) => tone switch
    {
        PostTone.Professional => "Profesional y formal",
        PostTone.Friendly => "Cercano y conversacional",
        PostTone.Inspirational => "Inspirador y aspiracional",
        PostTone.Direct => "Conciso y al grano",
        PostTone.Storytelling => "Narrativo, basado en una historia personal",
        _ => tone.ToString()
    };
}