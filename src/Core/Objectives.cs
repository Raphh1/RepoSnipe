using Spectre.Console;

namespace VoidTrader;

record Objective(
    string Id,
    string Name,
    string Description,
    string Category,
    Func<GameState, bool> Check,
    int    CreditReward = 0,
    int    RepReward    = 0,
    string Reward       = ""
);

static class Objectives
{
    public static readonly List<Objective> All =
    [
        // ── PROGRESSION ──────────────────────────────────────────────────────
        new("rich_100k",
            "Centenaire de crédits",
            "Accumuler 100 000cr",
            "Progression",
            s => s.Credits >= 100_000,
            CreditReward: 5_000, RepReward: 50,
            Reward: "+5 000cr, +50 réputation"),

        new("survive_50days",
            "Le Vieux de l'Espace",
            "Survivre 50 jours",
            "Progression",
            s => s.Day >= 50,
            CreditReward: 8_000, RepReward: 30,
            Reward: "+8 000cr, +30 réputation"),

        new("all_stations",
            "Cartographe du Vide",
            "Visiter au moins 15 stations différentes",
            "Progression",
            s => s.VisitedStations.Count >= 15,
            CreditReward: 10_000, RepReward: 100,
            Reward: "+10 000cr, +100 réputation"),

        new("legend_rep",
            "Légende Vivante",
            "Atteindre le statut Légende (réputation ≥ 1000)",
            "Progression",
            s => s.Reputation >= 1000,
            CreditReward: 20_000, RepReward: 0,
            Reward: "+20 000cr"),

        // ── BUILD ─────────────────────────────────────────────────────────────
        new("tier5_weapon",
            "Armement Légendaire",
            "Posséder une arme Tier 5",
            "Build",
            s => s.Weapons.Any(w => w.Tier == 5),
            CreditReward: 0, RepReward: 50,
            Reward: "+50 réputation"),

        new("ship_150hp",
            "Vaisseau Blindé",
            "Atteindre 150 PV max pour le vaisseau",
            "Build",
            s => s.ShipMaxHp >= 150,
            CreditReward: 5_000, RepReward: 0,
            Reward: "+5 000cr"),

        new("player_150hp",
            "Corps Amélioré",
            "Atteindre 150 PV max pour le joueur",
            "Build",
            s => s.PlayerMaxHp >= 150,
            CreditReward: 5_000, RepReward: 0,
            Reward: "+5 000cr"),

        // ── CRIMINEL ──────────────────────────────────────────────────────────
        new("enemy_public",
            "L'Ennemi de Tout le Monde",
            "Atteindre le statut Ennemi Public (réputation ≤ -1000)",
            "Criminel",
            s => s.Reputation <= -1_000,
            CreditReward: 5_000, RepReward: 0,
            Reward: "+5 000cr"),

        new("survive_interrogations",
            "Mur de Silence",
            "Survivre à 5 interrogatoires",
            "Criminel",
            s => s.InterrogationsSurvived >= 5,
            CreditReward: 3_000, RepReward: 50,
            Reward: "+3 000cr, +50 réputation"),

        new("prison_escape",
            "L'Évadé",
            "S'évader de prison au moins une fois",
            "Criminel",
            s => s.PrisonEscapes >= 1,
            CreditReward: 3_000, RepReward: 100,
            Reward: "+3 000cr, +100 réputation"),

        // ── NARRATIF ──────────────────────────────────────────────────────────
        new("all_npcs",
            "Réseau Étendu",
            "Rencontrer 10 PNJs nommés différents",
            "Narratif",
            s => s.NpcsMet.Count >= 10,
            CreditReward: 10_000, RepReward: 100,
            Reward: "+10 000cr, +100 réputation"),

        new("double_agent",
            "Le Double Jeu",
            "Devenir agent double",
            "Narratif",
            s => s.IsDoubleAgent,
            CreditReward: 5_000, RepReward: 0,
            Reward: "+5 000cr"),

        new("boss_killed",
            "Tueur de Légendes",
            "Vaincre un ennemi Boss (Alanossa, La Faucon...)",
            "Narratif",
            s => s.BossesDefeated >= 1,
            CreditReward: 5_000, RepReward: 50,
            Reward: "+5 000cr, +50 réputation"),

        new("faction_3missions",
            "Loyal jusqu'au Bout",
            "Accomplir 3 missions pour sa faction",
            "Narratif",
            s => s.FactionMissions >= 3 && s.Faction != FactionId.None,
            CreditReward: 8_000, RepReward: 75,
            Reward: "+8 000cr, +75 réputation"),

        new("faction_leader",
            "Le Patron",
            "Devenir chef d'une faction réputée de l'espace",
            "Narratif",
            s => s.IsFactionLeader,
            CreditReward: 30_000, RepReward: 300,
            Reward: "+30 000cr, +300 réputation"),

        // ── COLLECTION ────────────────────────────────────────────────────────
        new("weapon_collector",
            "Collectionneur Expérimenté",
            "Posséder au moins une arme de chaque Tier (1 à 5)",
            "Collection",
            s => Enumerable.Range(1, 5).All(t => s.Weapons.Any(w => w.Tier == t)),
            CreditReward: 15_000, RepReward: 100,
            Reward: "+15 000cr, +100 réputation"),

        // ── GRAND ARC ─────────────────────────────────────────────────────────
        new("all_bosses_killed",
            "Grand Nettoyage",
            "Vaincre tous les grands chefs de l'espace (8 bosses nommés)",
            "Légendaire",
            s => s.StationBossesBeaten.Count >= 8,
            CreditReward: 50_000, RepReward: 500,
            Reward: "+50 000cr, +500 réputation — tu as effacé l'ordre ancien"),

        new("space_lord",
            "Seigneur de l'Espace",
            "Rallier les 4 fragments de la Station Nexus (arc principal)",
            "Légendaire",
            s => s.StationPiecesRallied >= 4,
            CreditReward: 100_000, RepReward: 1000,
            Reward: "+100 000cr, +1000 réputation — la Station Nexus renaît"),
    ];

    // ── VÉRIFICATION GLOBALE ────────────────────────────────────────────────

    public static void CheckAll(GameState state)
    {
        foreach (var obj in All)
        {
            if (state.CompletedObjectives.Contains(obj.Id)) continue;
            if (!obj.Check(state)) continue;

            state.CompletedObjectives.Add(obj.Id);
            state.Credits    += obj.CreditReward;
            state.Reputation += obj.RepReward;

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[gold1]OBJECTIF COMPLÉTÉ[/]").RuleStyle("gold1"));
            AnsiConsole.MarkupLine($"  [gold1 bold]{obj.Name}[/]  [grey]— {obj.Category}[/]");
            AnsiConsole.MarkupLine($"  [grey]{obj.Description}[/]");
            AnsiConsole.MarkupLine($"  [yellow]Récompense : {obj.Reward}[/]");
            AnsiConsole.Write(new Rule().RuleStyle("gold1"));
            AnsiConsole.WriteLine();
        }
    }

    // ── AFFICHAGE ───────────────────────────────────────────────────────────

    public static void Show(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[gold1 bold]── OBJECTIFS ──[/]  [grey]{state.CompletedObjectives.Count}/{All.Count} complétés[/]");
        AnsiConsole.WriteLine();

        var categories = All.GroupBy(o => o.Category);
        foreach (var cat in categories)
        {
            AnsiConsole.MarkupLine($"[grey dim]── {cat.Key.ToUpper()} ──[/]");
            foreach (var obj in cat)
            {
                var done    = state.CompletedObjectives.Contains(obj.Id);
                var icon    = done ? "[green]✔[/]" : "[grey]○[/]";
                var nameCol = done ? $"[grey]{obj.Name}[/]" : $"[white]{obj.Name}[/]";
                var prog    = GetProgress(obj.Id, state);
                var progStr = prog != "" ? $" [grey dim]{prog}[/]" : "";
                AnsiConsole.MarkupLine($"  {icon} {nameCol}{progStr}");
                if (!done) AnsiConsole.MarkupLine($"    [grey dim]{obj.Description}  →  {obj.Reward}[/]");
            }
            AnsiConsole.WriteLine();
        }
    }

    static string GetProgress(string id, GameState state) => id switch
    {
        "rich_100k"              => $"({state.Credits:N0} / 100 000cr)",
        "survive_50days"         => $"(jour {state.Day} / 50)",
        "all_stations"           => $"({state.VisitedStations.Count} / 15 stations)",
        "legend_rep"             => $"({state.Reputation} / 1000)",
        "enemy_public"           => $"({state.Reputation} / -1000)",
        "survive_interrogations" => $"({state.InterrogationsSurvived} / 5)",
        "prison_escape"          => $"({state.PrisonEscapes} / 1)",
        "all_npcs"               => $"({state.NpcsMet.Count} / 10 PNJs)",
        "faction_3missions"      => $"({state.FactionMissions} / 3 missions)",
        "boss_killed"            => $"({state.BossesDefeated} / 1 boss)",
        "faction_leader"         => state.IsFactionLeader ? "(accompli)" : "(non accompli)",
        "weapon_collector"       => $"({Enumerable.Range(1,5).Count(t => state.Weapons.Any(w => w.Tier == t))} / 5 tiers)",
        "all_bosses_killed"      => $"({state.StationBossesBeaten.Count} / 8 bosses)",
        "space_lord"             => $"({state.StationPiecesRallied} / 4 fragments)",
        _                        => "",
    };
}
