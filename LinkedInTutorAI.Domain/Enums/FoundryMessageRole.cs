namespace LinkedInTutorAI.Domain.Enums;

/// <summary>
/// Rol de un mensaje dentro de la conversación con el agente de Azure AI Foundry.
/// </summary>
public enum FoundryMessageRole
{
    /// <summary>Instrucciones de sistema (rara vez se envían desde la app).</summary>
    System = 0,

    /// <summary>Mensaje del usuario.</summary>
    User = 1,

    /// <summary>Respuesta del agente.</summary>
    Assistant = 2,

    /// <summary>Resultado de una invocación a herramienta.</summary>
    Tool = 3
}