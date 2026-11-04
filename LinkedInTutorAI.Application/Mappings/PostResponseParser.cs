using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using LinkedInTutorAI.Application.Dtos;

namespace LinkedInTutorAI.Application.Mappings;

/// <summary>
/// Parse la respuesta JSON del agente en un <see cref="LinkedInPostResponse"/>.
/// Es tolerante: si el agente envuelve el JSON en fences de markdown o añade prose,
/// los quita; si el parseo falla, trata todo el texto como la <see cref="LinkedInPostResponse.GeneratedPost"/>
/// y extrae hashtags del propio texto como respaldo.
/// </summary>
public static class PostResponseParser
{
    private const string TitleKey = "titulo";
    private const string PostKey = "publicacion";
    private const string HashtagsKey = "hashtags";
    private const string RecomendacionesKey = "recomendaciones";

    private static readonly Regex HashtagRegex = new(@"(?m)^\s*#\w[\w-]*", RegexOptions.Compiled);

    /// <summary>
    /// Parse <paramref name="raw"/> en un <see cref="LinkedInPostResponse"/> para la sesión <paramref name="sessionId"/>.
    /// </summary>
    /// <param name="raw">Texto de respuesta del agente.</param>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <returns>El <see cref="LinkedInPostResponse"/> parseado, con respaldo si el JSON es inválido.</returns>
    public static LinkedInPostResponse Parse(string raw, Guid sessionId)
    {
        var json = StripFences(raw);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var post = GetString(root, PostKey);
            return new LinkedInPostResponse
            {
                SessionId = sessionId,
                Title = GetString(root, TitleKey),
                GeneratedPost = string.IsNullOrWhiteSpace(post) ? json : post,
                Hashtags = GetArray(root, HashtagsKey),
                Suggestions = GetArray(root, RecomendacionesKey),
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }
        catch
        {
            return Fallback(raw, sessionId);
        }
    }

    private static LinkedInPostResponse Fallback(string raw, Guid sessionId) => new()
    {
        SessionId = sessionId,
        Title = null,
        GeneratedPost = raw,
        Hashtags = ExtractHashtags(raw),
        Suggestions = [],
        GeneratedAt = DateTimeOffset.UtcNow
    };

    private static string? GetString(JsonElement root, string key) =>
        root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static IReadOnlyList<string> GetArray(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<string>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s)
            {
                list.Add(s);
            }
        }

        return new ReadOnlyCollection<string>(list);
    }

    private static IReadOnlyList<string> ExtractHashtags(string raw)
    {
        var list = new List<string>();
        foreach (Match m in HashtagRegex.Matches(raw))
        {
            list.Add(m.Value.Trim());
        }

        return new ReadOnlyCollection<string>(list.Distinct().ToList());
    }

    /// <summary>Quita fences de markdown (```json … ```) y espacios.</summary>
    private static string StripFences(string raw)
    {
        var s = raw.Trim();
        if (!s.StartsWith("```"))
        {
            return s;
        }

        var firstNewline = s.IndexOf('\n');
        if (firstNewline >= 0)
        {
            s = s[(firstNewline + 1)..];
        }

        if (s.EndsWith("```"))
        {
            s = s[..^3];
        }

        return s.Trim();
    }
}