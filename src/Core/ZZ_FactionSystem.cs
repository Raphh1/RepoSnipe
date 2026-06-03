using Spectre.Console;

namespace VoidTrader;

static class FactionSystem
{
    private static readonly Random Rng = new();

    public static int GetStanding(GameState state, FactionId faction)
        => state.FactionStanding.GetValueOrDefault(faction, 0);

    public static void AddStanding(GameState state, FactionId faction, int amount)
    {
        state.FactionStanding[faction] = Math.Clamp(
            GetStanding(state, faction) + amount,
            -1000, 1000
        );
        DamageRivalFaction(state, faction, amount / 2);
    }

    static (FactionId, FactionId) GetRivals(FactionId faction) => faction switch
    {
        FactionId.Faucons  => (FactionId.Gardiens, FactionId.Emporium),
        FactionId.Gardiens => (FactionId.Faucons,  FactionId.Culte),
        FactionId.Emporium => (FactionId.Faucons,  FactionId.Culte),
        FactionId.Culte    => (FactionId.Gardiens, FactionId.Emporium),
        _                  => (FactionId.None,     FactionId.None),
    };

    static void DamageRivalFaction(GameState state, FactionId faction, int damage)
    {
        var (rival1, rival2) = GetRivals(faction);
        if (rival1 != FactionId.None)
            state.FactionStanding[rival1] = Math.Max(-1000, GetStanding(state, rival1) - damage);
        if (rival2 != FactionId.None)
            state.FactionStanding[rival2] = Math.Max(-1000, GetStanding(state, rival2) - damage);
    }

    public static bool IsLoyal(GameState state, FactionId faction, int threshold = 100)
        => GetStanding(state, faction) >= threshold;

    public static bool IsHostile(GameState state, FactionId faction, int threshold = -100)
        => GetStanding(state, faction) <= threshold;

    public static string DescribeStanding(GameState state, FactionId faction)
        => GetStanding(state, faction) switch
        {
            >= 500 => "Héros de la faction",
            >= 200 => "Très apprécié",
            >= 50  => "Respecté",
            >= 0   => "Neutre",
            >= -50 => "Légèrement suspect",
            >= -200 => "Persona non grata",
            >= -500 => "Ennemi de la faction",
            _       => "Cible à abattre",
        };

    public static string GetFactionSummary(GameState state)
    {
        var lines = new List<string>();
        foreach (var factionId in new[] { FactionId.Faucons, FactionId.Emporium, FactionId.Gardiens, FactionId.Culte })
        {
            var (name, _, _) = Factions.Info[factionId];
            var standing = GetStanding(state, factionId);
            var desc  = DescribeStanding(state, factionId);
            var color = standing switch
            {
                >= 100 => "[green]",
                >= 0   => "[yellow]",
                >= -100 => "[orange1]",
                _       => "[red]",
            };
            lines.Add($"{color}{name}[/] : {desc} ({standing:+0;-#})");
        }
        return string.Join("\n", lines);
    }
}
