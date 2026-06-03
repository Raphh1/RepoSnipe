using Spectre.Console;

namespace VoidTrader;

/// <summary>
/// Point d'accès centralisé pour la mémoire des PNJs.
/// Chaque PNJ nommé conserve son historique avec le joueur pour toute la run.
/// </summary>
static class NpcTracker
{
    // ── ACCÈS ────────────────────────────────────────────────────────────────

    public static PersistentNpc Get(GameState state, string id, string name, string station)
    {
        if (!state.KnownNpcs.TryGetValue(id, out var npc))
        {
            npc = new PersistentNpc(id, name, station, FirstMetDay: state.Day);
            state.KnownNpcs[id] = npc;
        }
        return npc;
    }

    public static NpcReaction Reaction(GameState state, string id, string name, string station)
        => Get(state, id, name, station).GetReaction(state, state.Day);

    // ── MISE À JOUR ──────────────────────────────────────────────────────────

    public static void RecordMeeting(GameState state, string id, string name, string station,
        int repDelta = 0, string? tag = null)
    {
        var npc = Get(state, id, name, station);
        state.KnownNpcs[id] = npc.RecordMeeting(repDelta, tag);
    }

    public static void MakeAlly(GameState state, string id, string name, string station)
    {
        var npc = Get(state, id, name, station);
        state.KnownNpcs[id] = npc.BecomeAlly();
    }

    public static void MakeEnemy(GameState state, string id, string name, string station)
    {
        var npc = Get(state, id, name, station);
        state.KnownNpcs[id] = npc.BecomeEnemy();
    }

    public static bool IsEnemy(GameState state, string id)
        => state.KnownNpcs.TryGetValue(id, out var n) && n.IsEnemy;

    public static bool IsAlly(GameState state, string id)
        => state.KnownNpcs.TryGetValue(id, out var n) && n.IsAlly;

    public static bool HasTag(GameState state, string id, string tag)
        => state.KnownNpcs.TryGetValue(id, out var n) && (n.PriorChoices?.Contains(tag) ?? false);

    // ── AFFICHAGE ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Affiche le message d'accueil d'un PNJ selon son historique avec le joueur.
    /// Retourne la réaction pour que l'appelant puisse adapter la suite.
    /// </summary>
    public static NpcReaction ShowGreeting(GameState state, string id, string name, string station)
    {
        var npc      = Get(state, id, name, station);
        var reaction = npc.GetReaction(state, state.Day);

        if (npc.TimesMetCount > 0)
        {
            var msg   = npc.GetGreeting(reaction);
            var color = reaction switch
            {
                NpcReaction.Ally     => Color.Gold1,
                NpcReaction.Friendly => Color.Green,
                NpcReaction.Warm     => Color.Cyan1,
                NpcReaction.Hostile  => Color.Red,
                NpcReaction.Cold     => Color.OrangeRed1,
                _                    => Color.Grey,
            };
            Narrator.Say(msg, color);
        }

        state.KnownNpcs[id] = npc.RecordMeeting();
        state.NpcsMet.Add(name);
        return reaction;
    }

    // ── RIVAUX PERSISTANTS ───────────────────────────────────────────────────

    /// <summary>
    /// Vérifie si un rival cherche le joueur. Appelé à chaque voyage.
    /// Retourne true si une rencontre avec un rival s'est produite.
    /// </summary>
    public static bool MaybeRivalEncounter(GameState state)
    {
        var enemies = state.KnownNpcs.Values
            .Where(n => n.IsEnemy)
            .ToList();

        if (enemies.Count == 0) return false;
        if (new Random().Next(100) >= 18 * Math.Min(enemies.Count, 3)) return false;

        var rival = enemies[new Random().Next(enemies.Count)];
        TriggerRivalEncounter(state, rival);
        return true;
    }

    static readonly Random Rng = new();

    static void TriggerRivalEncounter(GameState state, PersistentNpc rival)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[red]RIVAL — {rival.Name.ToUpper()}[/]").RuleStyle("red"));

        var reason = rival.PriorChoices?.LastOrDefault() switch
        {
            "Trahi"      => $"{rival.Name} t'a cherché depuis que tu l'as trahi. Il t'a trouvé.",
            "Volé"       => $"{rival.Name} n'a pas oublié ce que tu lui as pris. Il veut des comptes.",
            "Humilié"    => $"{rival.Name} a gardé ça en mémoire. Chaque jour depuis.",
            "Abandonné"  => $"{rival.Name} était compté mort à cause de toi. Il ne l'est pas.",
            _            => $"{rival.Name} bloque ta route. Ses yeux te reconnaissent immédiatement.",
        };

        Narrator.Say(reason, Color.Red);
        AnsiConsole.WriteLine();

        ChoiceMenu.Resolve(new Situation($"{rival.Name} est là. Il veut quelque chose.",
        [
            new("Se battre", s =>
            {
                var scaledEnemy = new Enemy(
                    rival.Name,
                    60 + Math.Abs(rival.ReputationDelta) * 2,
                    12 + Math.Abs(rival.ReputationDelta) / 3,
                    25 + Math.Abs(rival.ReputationDelta) / 2,
                    300, 800,
                    "Il se bat avec la rage de quelqu'un qui attendait depuis longtemps.");

                var outcome = Combat.Start(s, scaledEnemy);
                if (outcome == CombatOutcome.Victory)
                {
                    s.KnownNpcs.Remove(rival.Id);
                    Narrator.Say($"{rival.Name} est à terre. La dette est soldée à ta façon.", Color.Gold1);
                    Narrator.Pause();
                }
                else
                    Situations.ApplyCombatOutcome(s, outcome);
            }),

            new("Présenter des excuses — essayer de régler ça", s =>
            {
                var chance = 25 + Math.Max(0, s.Reputation / 8);
                if (Rng.Next(100) < chance)
                {
                    s.Reputation += 20;
                    s.KnownNpcs[rival.Id] = rival with { IsEnemy = false };
                    Narrator.Say($"Il écoute. Ses épaules se relâchent. 'Ça n'efface rien. Mais...' Il repart. +20 rép.", Color.Yellow);
                }
                else
                {
                    Narrator.Say("Il n'est pas prêt à écouter. Le combat était inévitable.", Color.Red);
                    Situations.ApplyCombatOutcome(s, Combat.Start(s, new Enemy(
                        rival.Name, 55, 12, 24, 200, 600,
                        "Sa rage n'accepte pas les mots.")));
                }
                Narrator.Pause();
            }),

            new("Payer pour solder la dette", s =>
            {
                var montant = 800 + Math.Abs(rival.ReputationDelta) * 20;
                if (s.Credits < montant)
                {
                    Narrator.Say($"T'as pas les {montant}cr. Il n'attend plus.", Color.Red);
                    Situations.ApplyCombatOutcome(s, Combat.Start(s, new Enemy(
                        rival.Name, 55, 12, 24, 200, 600, "Il n'avait plus envie de parler.")));
                    return;
                }
                s.Credits -= montant;
                s.KnownNpcs[rival.Id] = rival with { IsEnemy = false };
                Narrator.Say($"-{montant}cr. Il prend l'argent. Il repart. La rancœur a un prix.", Color.Yellow);
                Narrator.Pause();
            }),

            new("Fuir — pas aujourd'hui", s =>
            {
                if (Rng.Next(100) < 50)
                {
                    s.Fuel = Math.Max(0, s.Fuel - 1);
                    Narrator.Say("Tu te tires. -1 carburant. Il sera là la prochaine fois.", Color.Grey);
                }
                else
                {
                    Narrator.Say("Il est plus rapide que prévu.", Color.Red);
                    Situations.ApplyCombatOutcome(s, Combat.Start(s, new Enemy(
                        rival.Name, 55, 12, 24, 200, 600, "Il t'attendait.")));
                }
                Narrator.Pause();
            }),
        ], Color.Red), state);
    }
}
