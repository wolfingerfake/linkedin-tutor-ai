using System.Collections.Concurrent;
using LinkedInTutorAI.Domain.Abstractions;
using LinkedInTutorAI.Domain.Entities;

namespace LinkedInTutorAI.Infrastructure.Persistence;

/// <summary>
/// Implementación en memoria de <see cref="ITutorSessionRepository"/> para el MVP académico.
/// Sustituible por EF Core + SQLite sin tocar la capa de aplicación.
/// Aplica la mitigación de IDOR: <see cref="GetByIdAsync"/> solo devuelve la sesión
/// si pertenece al <paramref name="userId"/> indicado.
/// </summary>
public sealed class InMemoryTutorSessionRepository : ITutorSessionRepository
{
    private readonly ConcurrentDictionary<Guid, TutorSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, List<ChatMessage>> _messages = new();

    /// <inheritdoc/>
    public Task<TutorSession?> GetByIdAsync(Guid sessionId, string userId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session) && session.UserId == userId)
        {
            var messages = _messages.TryGetValue(sessionId, out var msgs) ? msgs : new List<ChatMessage>();
            var withMessages = session with { Messages = messages.ToList() };
            return Task.FromResult<TutorSession?>(withMessages);
        }

        // No existe o es ajena: se trata como no encontrada (IDOR).
        return Task.FromResult<TutorSession?>(null);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TutorSession>> GetByUserAsync(string userId, CancellationToken ct = default)
    {
        var list = _sessions.Values
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<TutorSession>>(list);
    }

    /// <inheritdoc/>
    public Task<TutorSession> CreateAsync(TutorSession session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        _messages[session.Id] = new List<ChatMessage>();
        return Task.FromResult(session);
    }

    /// <inheritdoc/>
    public Task AddMessageAsync(Guid sessionId, ChatMessage message, CancellationToken ct = default)
    {
        if (_messages.TryGetValue(sessionId, out var msgs))
        {
            msgs.Add(message);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SaveAsync(TutorSession session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}