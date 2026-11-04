namespace linkedin_tutor_ai.Models.Enums;

/// <summary>
/// Motivo por el que el agente de Azure AI Foundry finalizó la generación.
/// </summary>
public enum FinishReason
{
    /// <summary>El agente terminó naturalmente (stop sequence).</summary>
    Stop = 0,

    /// <summary>Se alcanzó la longitud máxima de salida.</summary>
    Length = 1,

    /// <summary>La respuesta fue filtrada por el filtro de contenido.</summary>
    ContentFilter = 2,

    /// <summary>El agente invocó una herramienta.</summary>
    ToolCall = 3,

    /// <summary>La generación fue cancelada por el usuario (CancellationToken).</summary>
    Cancelled = 4,

    /// <summary>Ocurrió un error durante la generación.</summary>
    Error = 5
}