namespace LinkedInTutorAI.Domain.Exceptions;

/// <summary>Clase base para excepciones de dominio (semánticas, no técnicas).</summary>
public class DomainException : Exception
{
    /// <summary>Crea una <see cref="DomainException"/> con un mensaje.</summary>
    /// <param name="message">Descripción del error.</param>
    protected DomainException(string message) : base(message) { }

    /// <summary>Crea una <see cref="DomainException"/> con mensaje y causa interna.</summary>
    /// <param name="message">Descripción del error.</param>
    /// <param name="inner">Excepción causal.</param>
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}