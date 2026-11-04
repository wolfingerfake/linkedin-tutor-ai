namespace linkedin_tutor_ai.Models.Enums;

/// <summary>
/// Objetivo comunicacional de la publicación de LinkedIn.
/// Permite al agente adaptar el enfoque del post generado.
/// </summary>
public enum PostGoal
{
    /// <summary>Maximizar alcance e impresiones.</summary>
    IncreaseReach = 0,

    /// <summary>Conseguir nuevas conexiones / invitaciones aceptadas.</summary>
    GetConnections = 1,

    /// <summary>Posicionarse para oportunidades laborales.</summary>
    FindJob = 2,

    /// <summary>Construir marca personal y autoridad.</summary>
    BuildPersonalBrand = 3,

    /// <summary>Compartir conocimiento o aprendizajes.</summary>
    ShareKnowledge = 4
}