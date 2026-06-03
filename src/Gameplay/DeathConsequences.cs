namespace VoidTrader;

using Spectre.Console;

/// <summary>
/// Système qui gère ce qui se passe APRÈS la mort du joueur.
/// Au lieu de redémarrer silencieusement, on montre des rumeurs et conséquences.
/// </summary>
static class DeathConsequences
{
    private static readonly Random Rng = new();

    /// <summary>
    /// Affiche une épilogue et enregistre la mort pour les futures rencontres.
    /// </summary>
    public static void ShowDeathEpilogue(GameState state)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[red bold]═══════════════════════════════════════[/]");
        AnsiConsole.MarkupLine("[red bold]  MORT DE {0} — JOUR {1}[/]", state.Class.Name, state.Day);
        AnsiConsole.MarkupLine("[red bold]═══════════════════════════════════════[/]\n");

        // Affiche la cause de mort
        var deathMessage = GetDeathMessage(state);
        AnsiConsole.MarkupLine("[grey]{0}[/]\n", deathMessage);

        // Statistiques finales
        AnsiConsole.MarkupLine("[yellow]Statistiques finales :[/]");
        AnsiConsole.MarkupLine("  Crédits : {0}", state.Credits);
        AnsiConsole.MarkupLine("  Réputation : {0} ({1})", state.Reputation, state.ReputationLabel);
        AnsiConsole.MarkupLine("  Jours survécu : {0}", state.Day);
        AnsiConsole.MarkupLine("  Bosses vaincus : {0}\n", state.BossesDefeated);

        // Rumeurs — ce que les gens disent
        AnsiConsole.MarkupLine("[cyan1 bold]Ce qu'on raconte sur toi :[/]");
        var rumours = GenerateRumours(state);
        foreach (var rumour in rumours)
            AnsiConsole.MarkupLine("  [grey]▪[/] {0}", rumour);

        // Enregistre la mort
        RecordDeath(state);

        // Prompt avant nouvelle partie
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Recommencer ? [dim](Enter)[/]");
        Narrator.Pause();
    }

    /// <summary>
    /// Génère 3-4 rumeurs basées sur le profil du joueur.
    /// </summary>
    static List<string> GenerateRumours(GameState state)
    {
        var rumours = new List<string>();

        // Rumeur 1 : Sur sa mort
        if (string.IsNullOrEmpty(state.DeathCause))
            rumours.Add("Personne sait vraiment ce qui s'est passé. Juste qu'il est parti et pas revenu.");
        else
            rumours.Add($"Il est mort comme ça : {state.DeathCause}");

        // Rumeur 2 : Sur sa réputation
        if (state.Reputation > 100)
            rumours.Add($"C'était quelqu'un de respecté. Trop respecté. Peut-être qu'on l'a laissé crever exprès.");
        else if (state.Reputation < -100)
            rumours.Add($"Bon débarras. On disait qu'il était une ordure. Les rumeurs avaient raison.");
        else
            rumours.Add("Pas beaucoup de gens vont le pleurer. Pas beaucoup vont le mépriser non plus.");

        // Rumeur 3 : Sur ses actions
        rumours.Add("Il était seul. Personne pour le venger, personne pour l'enterrer.");

        // Rumeur 4 : Sur son héritage
        var rumour4 = state.Faction switch
        {
            FactionId.Faucons => "Les Faucons Noirs disent qu'il aurait fait un bon pirate, s'il avait survécu.",
            FactionId.Emporium => "L'Emporium se demande qui héritera de ses dettes.",
            FactionId.Gardiens => "Les Gardiens pensent qu'il est mort de façon... honorable.",
            FactionId.Culte => "Le Culte dit qu'il a compris trop tard le Vide.",
            _ => "Personne de l'administration locale ne mentionne même son nom.",
        };
        rumours.Add(rumour4);

        return rumours;
    }

    /// <summary>
    /// Formule un message de mort basé sur le contexte.
    /// </summary>
    static string GetDeathMessage(GameState state)
    {
        if (!string.IsNullOrEmpty(state.DeathCause))
            return state.DeathCause;

        return new[] {
            "Tu as fermé les yeux. Pas pour dormir.",
            "Quelque chose s'est brisé. Tu ne savais pas si c'était ton corps ou ton vaisseau.",
            "Les ténèbres de l'espace étaient plus froides que prévu.",
            "Personne n'a entendu ton dernier cri.",
            "Tu as compris trop tard que tu n'aurais pas dû venir ici.",
        }[Rng.Next(5)];
    }

    /// <summary>
    /// Enregistre la mort du joueur pour les futures rencontres.
    /// </summary>
    static void RecordDeath(GameState state)
    {
        // TODO: Une fois que PriorDeaths compile correctement
        /*
        var death = new PriorDeath(
            DayOfDeath: state.Day,
            Location: state.CurrentStation,
            Cause: state.DeathCause ?? "Cause inconnue",
            Credits: state.Credits,
            Reputation: state.Reputation
        );

        state.PriorDeaths.Add(death);

        // Marque NPCs comme ayant entendu parler de ta mort
        foreach (var npc in state.PersistentNpcs.Values)
        {
            // 30% chance qu'un NPC connaisse ta mort
            if (Rng.Next(100) < 30)
            {
                // Met à jour sa réaction
                var updatedNpc = npc with { ReputationDelta = npc.ReputationDelta + (Rng.Next(2) == 0 ? -5 : 0) };
                state.PersistentNpcs[npc.Id] = updatedNpc;
            }
        }
        */
    }

    /// <summary>
    /// Retourne un message spécial si le joueur revient après être "mort" (nouvelle partie).
    /// Les NPCs vont réagir différemment.
    /// </summary>
    public static string GetGhostGreeting(string npcName, int priorDeaths)
    {
        if (priorDeaths == 0) return "";  // Première vie

        return priorDeaths switch
        {
            1 => $"{npcName} t'observe avec étonnement. 'T'étais mort... Tu l'as pas l'air, là.'",
            2 => $"{npcName} recule. 'Toi ? T'es impossible à tuer, ou t'es juste impossible ?'",
            _ => $"{npcName} voit quelque chose dans tes yeux. De la peur. 'T'es revenu... de combien de vies maintenant ?'",
        };
    }

    /// <summary>
    /// Chance qu'un NPC refuse de parler au joueur après trop de morts.
    /// </summary>
    public static bool ShouldAvoidAfterManyDeaths(int deathCount)
    {
        if (deathCount == 0) return false;
        if (deathCount == 1) return Rng.Next(100) < 5;   // 5%
        if (deathCount == 2) return Rng.Next(100) < 15;  // 15%
        return Rng.Next(100) < 40;  // 40% pour 3+ morts
    }
}
