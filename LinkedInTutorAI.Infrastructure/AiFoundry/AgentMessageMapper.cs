using System.Text;
using Azure.AI.Agents.Persistent;
using LinkedInTutorAI.Application.Dtos;
using LinkedInTutorAI.Domain.Enums;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Mapper que traduce los tipos del SDK <c>Azure.AI.Agents.Persistent</c>
/// (<see cref="PersistentThreadMessage"/>, <see cref="ThreadRun"/>, <see cref="RunStatus"/>,
/// <see cref="StreamingUpdate"/>) a los DTOs de aplicación (<see cref="FoundryResponse"/>,
/// <see cref="ChatStreamChunk"/>). Aísla a la capa de aplicación de la forma del SDK.
/// </summary>
public static class AgentMessageMapper
{
    /// <summary>
    /// Extrae el texto plano de un <see cref="PersistentThreadMessage"/> recorriendo sus
    /// <see cref="PersistentThreadMessage.ContentItems"/> y concatenando los
    /// <see cref="MessageTextContent"/>.
    /// </summary>
    /// <param name="message">Mensaje devuelto por el agente.</param>
    /// <returns>Texto plano del mensaje.</returns>
    public static string ExtractText(PersistentThreadMessage message)
    {
        var sb = new StringBuilder();
        foreach (var content in message.ContentItems)
        {
            if (content is MessageTextContent textContent)
            {
                sb.Append(textContent.Text);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Mapa un <see cref="PersistentThreadMessage"/> y el estado de su run a un
    /// <see cref="FoundryResponse"/>.
    /// </summary>
    /// <param name="message">Mensaje del asistente.</param>
    /// <param name="threadId">Identificador del thread (para continuar la conversación).</param>
    /// <param name="status">Estado final del run.</param>
    /// <returns>El <see cref="FoundryResponse"/> para la capa de aplicación.</returns>
    public static FoundryResponse ToFoundryResponse(
        PersistentThreadMessage message,
        string? threadId,
        RunStatus status) => new()
    {
        ThreadId = threadId,
        AssistantMessage = ExtractText(message),
        MessageId = message.Id,
        FinishReason = ToFinishReason(status),
        CreatedAt = message.CreatedAt
    };

    /// <summary>
    /// Construye un <see cref="ChatStreamChunk"/> de control/delta para el modo streaming.
    /// </summary>
    /// <param name="delta">Delta de texto (puede ser vacío en chunks de control).</param>
    /// <param name="threadId">Identificador del thread.</param>
    /// <param name="isFinal"><see langword="true"/> si es el último chunk.</param>
    /// <param name="reason">Motivo de finalización.</param>
    public static ChatStreamChunk ToStreamChunk(
        string delta,
        string? threadId,
        bool isFinal,
        FinishReason reason) => new()
    {
        Delta = delta,
        ThreadId = threadId,
        IsFinal = isFinal,
        FinishReason = reason
    };

    /// <summary>Traduce <see cref="RunStatus"/> del SDK a <see cref="FinishReason"/> de dominio.</summary>
    public static FinishReason ToFinishReason(RunStatus status)
    {
        if (status == RunStatus.Completed)
        {
            return FinishReason.Stop;
        }

        if (status == RunStatus.Cancelled)
        {
            return FinishReason.Cancelled;
        }

        return FinishReason.Error;
    }
}