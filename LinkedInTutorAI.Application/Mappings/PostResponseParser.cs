using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LinkedInTutorAI.Application.Dtos;

namespace LinkedInTutorAI.Application.Mappings;

/// <summary>
/// Parse la respuesta del agente en un <see cref="LinkedInPostResponse"/>.
/// Soporta dos formatos:
/// 1) JSON (<c>{ "titulo": ..., "publicacion": ..., "hashtags": [...], "recomendaciones": [...] }</c>).
/// 2) Texto plano por secciones con encabezados en mayúsculas
///    (TÍTULO / PUBLICACIÓN / HASHTAGS / RECOMENDACIONES / RESPONIBLE AI),
///    que es lo que el agente devuelve naturalmente en el playground.
/// Si nada encaja, trata todo el texto como <see cref="LinkedInPostResponse.GeneratedPost"/>
/// y extrae hashtags del propio texto como respaldo.
/// </summary>
public static class PostResponseParser
{
    // Claves JSON
    private const string TitleKey = "titulo";
    private const string PostKey = "publicacion";
    private const string HashtagsKey = "hashtags";
    private const string RecomendacionesKey = "recomendaciones";

    // Nombres de secciones (folded, uppercase, sin acentos)
    private const string SecTitle = "TITULO";
    private const string SecPost = "PUBLICACION";
    private const string SecHashtags = "HASHTAGS";
    private const string SecRecomendaciones = "RECOMENDACIONES";
    private const string SecResponsable = "RESPONIBLE AI";

    private static readonly Regex HashtagRegex = new(@"#[\w][\w-]*", RegexOptions.Compiled);

    /// <summary>
    /// Parse <paramref name="raw"/> en un <see cref="LinkedInPostResponse"/> para la sesión <paramref name="sessionId"/>.
    /// </summary>
    public static LinkedInPostResponse Parse(string raw, Guid sessionId)
    {
        var trimmed = raw.Trim();

        // 1) Si parece JSON, lo intenta primero.
        if (trimmed.StartsWith("{"))
        {
            var json = StripFences(trimmed);
            if (TryParseJson(json, sessionId, out var jsonResp))
            {
                return jsonResp;
            }
        }

        // 2) Texto por secciones (formato natural del agente).
        return TryParseSections(raw, sessionId) ?? Fallback(raw, sessionId);
    }

    private static bool TryParseJson(string json, Guid sessionId, out LinkedInPostResponse? result)
    {
        result = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var post = GetString(root, PostKey);
            result = new LinkedInPostResponse
            {
                SessionId = sessionId,
                Title = GetString(root, TitleKey),
                GeneratedPost = string.IsNullOrWhiteSpace(post) ? json : post,
                Hashtags = GetArray(root, HashtagsKey),
                Suggestions = GetArray(root, RecomendacionesKey),
                GeneratedAt = DateTimeOffset.UtcNow
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parse texto estructurado por encabezados de sección (TÍTULO, PUBLICACIÓN, HASHTAGS, RECOMENDACIONES, RESPONIBLE AI).
    /// Devuelve <see langword="null"/> si no encuentra la sección PUBLICACIÓN.
    /// </summary>
    private static LinkedInPostResponse? TryParseSections(string raw, Guid sessionId)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var current = SecPost; // bucket por defecto: contenido antes de cualquier encabezado
        sections[current] = new List<string>();

        foreach (var line in raw.Split('\n'))
        {
            var folded = Fold(line).Trim().TrimEnd(':', '.', '-');
            var header = ClassifyHeader(folded);
            if (header is not null)
            {
                current = header;
                if (!sections.ContainsKey(current))
                {
                    sections[current] = new List<string>();
                }
            }
            else
            {
                sections[current].Add(line);
            }
        }

        if (!sections.TryGetValue(SecPost, out var postLines) || postLines.Count == 0)
        {
            return null;
        }

        return new LinkedInPostResponse
        {
            SessionId = sessionId,
            Title = JoinAndTrim(sections, SecTitle),
            GeneratedPost = JoinAndTrim(sections, SecPost),
            Hashtags = ExtractHashtags(JoinAndTrim(sections, SecHashtags)),
            Suggestions = ToList(JoinAndTrim(sections, SecRecomendaciones)),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Clasifica una línea foldeda como encabezado de sección, o <see langword="null"/> si no lo es.</summary>
    private static string? ClassifyHeader(string folded)
    {
        if (folded.Length == 0 || folded.Length > 40)
        {
            return null;
        }

        if (folded == SecTitle) return SecTitle;
        if (folded == SecPost) return SecPost;
        if (folded == SecHashtags) return SecHashtags;
        if (folded == SecRecomendaciones) return SecRecomendaciones;
        if (folded == SecResponsable) return SecResponsable;
        return null;
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

    private static IReadOnlyList<string> ExtractHashtags(string text)
    {
        var list = new List<string>();
        foreach (Match m in HashtagRegex.Matches(text))
        {
            list.Add(m.Value.Trim());
        }

        return new ReadOnlyCollection<string>(list.Distinct().ToList());
    }

    /// <summary>Convierte el texto de RECOMENDACIONES en una lista de items, quitando viñetas.</summary>
    private static IReadOnlyList<string> ToList(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<string>();
        foreach (var l in lines)
        {
            var clean = l.TrimStart('-', '•', '*', ' ', '\t');
            if (clean.Length > 0)
            {
                list.Add(clean);
            }
        }

        return new ReadOnlyCollection<string>(list);
    }

    /// <summary>Junta las líneas de una sección, quitando líneas vacías al inicio y al final.</summary>
    private static string JoinAndTrim(Dictionary<string, List<string>> sections, string key)
    {
        if (!sections.TryGetValue(key, out var lines) || lines.Count == 0)
        {
            return string.Empty;
        }

        var list = lines.ToList();
        while (list.Count > 0 && string.IsNullOrWhiteSpace(list[0]))
        {
            list.RemoveAt(0);
        }

        while (list.Count > 0 && string.IsNullOrWhiteSpace(list[^1]))
        {
            list.RemoveAt(list.Count - 1);
        }

        return string.Join('\n', list);
    }

    /// <summary>Quita acentos y pasa a mayúsculas (para comparar encabezados sin depender de tildes).</summary>
    private static string Fold(string s) =>
        new string(s.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray()).ToUpperInvariant();

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