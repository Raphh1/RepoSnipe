using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace VoidTrader;

/// <summary>
/// Charge et met en cache les fichiers JSON de contenu.
/// Tous les textes narratifs (ambiance, events, wander) viennent d'ici.
/// La logique (outcomes, combat) reste en C#.
/// </summary>
static class ContentLoader
{
    static readonly Random Rng = new();
    static JsonNode? _ambiance;
    static JsonNode? _exploration;
    static JsonNode? _wander;

    static bool _loaded = false;

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        var baseDir = AppContext.BaseDirectory;
        // Cherche le dossier content/ à la racine du projet
        var contentDir = FindContentDir(baseDir);

        _ambiance    = LoadJson(contentDir, "ambiance.json");
        _exploration = LoadJson(contentDir, "exploration.json");
        _wander      = LoadJson(contentDir, "wander.json");
    }

    static string FindContentDir(string start)
    {
        var dir = start;
        for (int i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(dir, "content");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        return Path.Combine(start, "content"); // fallback
    }

    static JsonNode? LoadJson(string dir, string file)
    {
        var path = Path.Combine(dir, file);
        if (!File.Exists(path)) return null;
        try { return JsonNode.Parse(File.ReadAllText(path)); }
        catch { return null; }
    }

    // ── AMBIANCE ────────────────────────────────────────────────────────────

    /// <summary>Retourne un texte d'ambiance aléatoire pour une station.</summary>
    public static string? GetAmbiance(string station)
    {
        Load();
        var arr = _ambiance?[station]?.AsArray();
        if (arr == null || arr.Count == 0) return null;
        return arr[Rng.Next(arr.Count)]?.GetValue<string>();
    }

    // ── EXPLORATION EVENTS ──────────────────────────────────────────────────

    public record EventChoice(string Label, string Flavor, string Outcome);
    public record ExplorationEvent(string Setup, EventChoice[] Choices);

    public static ExplorationEvent? GetExplorationEvent(string zoneType)
    {
        Load();
        var arr = _exploration?[zoneType]?.AsArray();
        if (arr == null || arr.Count == 0) return null;

        var node = arr[Rng.Next(arr.Count)];
        if (node == null) return null;

        var setup   = node["setup"]?.GetValue<string>() ?? "";
        var choicesNode = node["choices"]?.AsArray();
        if (choicesNode == null) return null;

        var choices = choicesNode
            .Select(c => new EventChoice(
                c?["label"]?.GetValue<string>()   ?? "?",
                c?["flavor"]?.GetValue<string>()  ?? "",
                c?["outcome"]?.GetValue<string>() ?? "nothing"))
            .ToArray();

        return new ExplorationEvent(setup, choices);
    }

    // ── WANDER EVENTS ───────────────────────────────────────────────────────

    public record WanderEvent(string Setup, EventChoice[] Choices);

    public static WanderEvent? GetWanderEvent(string station, int danger)
    {
        Load();

        // Cherche d'abord station-specific, puis catégorie danger, puis generic
        var specific = _wander?[station]?.AsArray();
        var category = danger switch { >= 4 => "high", 3 => "mid", _ => "low" };
        var cat      = _wander?[category]?.AsArray();
        var generic  = _wander?["generic"]?.AsArray();

        // Pool combiné : spécifique + catégorie + générique
        var pool = new List<JsonNode?>();
        if (specific != null) pool.AddRange(specific);
        if (cat      != null) pool.AddRange(cat);
        if (generic  != null && pool.Count < 3) pool.AddRange(generic);

        if (pool.Count == 0) return null;
        var node = pool[Rng.Next(pool.Count)];
        if (node == null) return null;

        var setup   = node["setup"]?.GetValue<string>() ?? "";
        var choicesNode = node["choices"]?.AsArray();
        if (choicesNode == null) return null;

        var choices = choicesNode
            .Select(c => new EventChoice(
                c?["label"]?.GetValue<string>()   ?? "?",
                c?["flavor"]?.GetValue<string>()  ?? "",
                c?["outcome"]?.GetValue<string>() ?? "nothing"))
            .ToArray();

        return new WanderEvent(setup, choices);
    }
}
