using Spectre.Console;

namespace VoidTrader;

static class Prison
{
    private static readonly Random Rng = new();

    public static void Enter(GameState state)
    {
        var bail = ComputeBail(state);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red bold]DÉTENTION[/]").RuleStyle("red"));
        Narrator.Say("Tu te réveilles derrière des barreaux. L'odeur de la cellule est un mélange de désinfectant bon marché et de mauvaises décisions.", Color.Red);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [bold red]Tu es en prison. Tu ne peux aller nulle part.[/]");
        AnsiConsole.MarkupLine("  [grey]Tes affaires ont été saisies. Ton vaisseau est en fourrière. La sortie passe par ici, ou nulle part.[/]");
        AnsiConsole.MarkupLine($"  [grey]Caution estimée :[/] [yellow]{bail}cr[/]");
        AnsiConsole.WriteLine();
        Narrator.Pause();

        while (state.IsImprisoned)
        {
            ChoiceMenu.Resolve(PrisonSituation(state, bail), state);
        }

        AnsiConsole.MarkupLine("[green bold]Tu es libre.[/]");
        AnsiConsole.WriteLine();
    }

    static int ComputeBail(GameState state)
    {
        var base_ = 400;
        var repPenalty = Math.Abs(Math.Min(0, state.Reputation)) * 4;
        return Math.Min(6000, base_ + repPenalty);
    }

    static Situation PrisonSituation(GameState state, int bail) => new(
        "[red bold]Tu es en cellule. Il n'y a pas d'autre issue que d'ici.[/] Que fais-tu ?",
        [
            new("Payer la caution",
                s =>
                {
                    if (s.Credits < bail)
                    {
                        Narrator.Say($"T'as pas les {bail}cr. Le gardien te regarde sans pitié.", Color.Red);
                        Narrator.Pause();
                        return;
                    }
                    s.Credits -= bail;
                    s.IsImprisoned = false;
                    s.Reputation += 10;
                    Narrator.Say($"Tu paies les {bail}cr. Le gardien te rend tes affaires dans un sac en plastique. +10 réputation.", Color.Green);
                    Narrator.Pause();
                },
                s => s.Credits >= bail),

            new("Corrompre le gardien",
                s =>
                {
                    var pot = bail / 3;
                    if (s.Credits < pot)
                    {
                        Narrator.Say($"T'as même pas de quoi graisser une patte. Il faudrait {pot}cr.", Color.Red);
                        Narrator.Pause();
                        return;
                    }
                    s.Credits -= pot;
                    var success = Rng.Next(100) < 55 + Math.Max(0, s.Reputation / 10);
                    if (success)
                    {
                        s.IsImprisoned = false;
                        Narrator.Say($"Le gardien glisse la clé sans te regarder. Tu récupères tes affaires par la sortie de service. -{pot}cr.", Color.Green);
                    }
                    else
                    {
                        s.Reputation -= 15;
                        AdvanceDay(s);
                        Narrator.Say($"Il prend l'argent. Et il appelle son superviseur. -{pot}cr, -15 réputation. +1 jour de détention supplémentaire.", Color.Red);
                    }
                    Narrator.Pause();
                },
                s => s.Credits >= bail / 3),

            new("Tenter une évasion furtive — repérer le moment idéal",
                s => ResolveStealthEscape(s)),

            new("Tenter une évasion frontale — forcer le passage",
                s =>
                {
                    var escapeChance = 25 + (s.Class.Name == "Contrebandier" ? 20 : 0)
                                          + (s.Class.Name == "Vagabond" ? 10 : 0)
                                          + (s.Class.Name == "Seigneur de guerre" ? 15 : 0);
                    if (Rng.Next(100) < escapeChance)
                    {
                        s.IsImprisoned = false;
                        s.PrisonEscapes++;
                        s.Reputation -= 30;
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(20, 40));
                        Narrator.Say("Tu passes en force. Des gardes tentent de t'arrêter. Tu en prends pour tes frais mais tu sors. -30 réputation. -PV joueur.", Color.Gold1);
                    }
                    else
                    {
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(30, 55));
                        s.Reputation -= 25;
                        AdvanceDay(s);
                        AdvanceDay(s);
                        Narrator.Say("Ils t'ont attendu dans le couloir. Retour en cellule en mauvais état. -PV joueur, -25 réputation, +2 jours.", Color.Red);
                    }
                    Narrator.Pause();
                }),

            new("Négocier avec le superviseur — proposer quelque chose",
                s => ResolveNegotiation(s, bail)),

            new("Purger la peine — attendre sans rien faire",
                s =>
                {
                    var days = Rng.Next(2, 5);
                    for (int i = 0; i < days; i++) AdvanceDay(s);

                    s.IsImprisoned = false;
                    s.Reputation += 5;

                    switch (Rng.Next(4))
                    {
                        case 0:
                            var contact = Rng.Next(300, 900);
                            s.Credits += contact;
                            Narrator.Say($"Tu purges {days} jours. En cellule tu rencontres quelqu'un d'utile. Il te laisse un numéro et un acompte. +{contact}cr. +5 réputation.", Color.Cyan1);
                            break;
                        case 1:
                            Narrator.Say($"Tu purges {days} jours. Tu utilises le temps pour observer. Pour réfléchir. Pour te promettre de faire moins d'erreurs. +5 réputation.", Color.Grey);
                            break;
                        case 2:
                            s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(10, 20));
                            Narrator.Say($"Tu purges {days} jours. La nourriture de la détention te fait regretter les rations spatiales. -PV joueur. +5 réputation.", Color.Yellow);
                            break;
                        case 3:
                            var info = Rng.Next(500, 1500);
                            s.Credits += info;
                            Narrator.Say($"Tu purges {days} jours. Un codétenu te parle d'un dépôt non surveillé avant de partir. +{info}cr d'infos exploitées.", Color.Gold1);
                            break;
                    }
                    Narrator.Pause();
                }),
        ],
        Color.Red);

    // ── ÉVASION FURTIVE ─────────────────────────────────────────────────────

    static void ResolveStealthEscape(GameState state)
    {
        Narrator.Say("Tu observes les mouvements des gardes. Les rotations, les angles morts, les moments creux. Il faut être méthodique.", Color.Yellow);
        AnsiConsole.WriteLine();

        ChoiceMenu.Resolve(new Situation("Phase 1 — Sortir de la cellule. Comment ?",
        [
            new("Attendre la relève et glisser dans l'angle mort", s =>
            {
                if (Rng.Next(100) < 65)
                {
                    Narrator.Say("Tu minutais bien. Tu te fais tout petit. La porte reste ouverte trois secondes. Tu passes.", Color.Green);
                    StealthPhase2(s);
                }
                else
                {
                    s.Reputation -= 10;
                    AdvanceDay(s);
                    Narrator.Say("Le garde s'est retourné trop tôt. Il t'a vu. Il t'explique que c'est une mauvaise idée. +1 jour. -10 rép.", Color.Red);
                    Narrator.Pause();
                }
            }),
            new("Créer une diversion — faire du bruit dans le couloir voisin", s =>
            {
                if (Rng.Next(100) < 50)
                {
                    Narrator.Say("Le garde part vérifier. Tu as vingt secondes. Tu les utilises toutes.", Color.Green);
                    StealthPhase2(s);
                }
                else
                {
                    s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(10, 25));
                    AdvanceDay(s);
                    Narrator.Say("Le garde a envoyé son collègue à ta place. Et le collègue est moins patient. -PV. +1 jour.", Color.Red);
                    Narrator.Pause();
                }
            }),
            new("Tenter de forcer la serrure à mains nues", s =>
            {
                var bonus = s.Class.Name == "Hackeur" ? 25 : s.Class.Name == "Mécanicien" ? 20 : 0;
                if (Rng.Next(100) < 35 + bonus)
                {
                    Narrator.Say("Tu as des mains habiles. Et de la chance. La serrure cède sans bruit.", Color.Green);
                    StealthPhase2(s);
                }
                else
                {
                    s.Reputation -= 15;
                    AdvanceDay(s);
                    Narrator.Say("Ça a fait un bruit. Un bruit très précis. Le genre qui fait venir des gens. -15 rép. +1 jour.", Color.Red);
                    Narrator.Pause();
                }
            }),
            new("← Abandonner pour l'instant — trop risqué", _ => { Narrator.Say("Tu te rassieds. Pas ce soir.", Color.Grey); Narrator.Pause(); }),
        ], Color.Yellow), state);
    }

    static void StealthPhase2(GameState state)
    {
        AnsiConsole.WriteLine();
        ChoiceMenu.Resolve(new Situation("Phase 2 — Couloir principal. Des gardes patrouillent. Comment tu passes ?",
        [
            new("Longer les murs — rester dans l'ombre", s =>
            {
                if (Rng.Next(100) < 70)
                {
                    Narrator.Say("Lent. Méticuleux. Tu progresses sans un son.", Color.Green);
                    StealthPhase3(s);
                }
                else
                {
                    s.Reputation -= 20;
                    AdvanceDay(s);
                    AdvanceDay(s);
                    Narrator.Say("Une caméra que tu avais pas vue. Retour à la case départ, en moins bonne forme. -20 rép. +2 jours.", Color.Red);
                    Narrator.Pause();
                }
            }),
            new("Voler un uniforme dans un casier ouvert", s =>
            {
                if (Rng.Next(100) < 55)
                {
                    Narrator.Say("Taille approximative. Assurance maximale. Tu marches comme si tu avais le droit.", Color.Green);
                    StealthPhase3(s);
                }
                else
                {
                    s.Reputation -= 15;
                    AdvanceDay(s);
                    Narrator.Say("Un superviseur te demande de te présenter. Ton badge est en carton. Ça ne lui échappe pas. -15 rép. +1 jour.", Color.Red);
                    Narrator.Pause();
                }
            }),
            new("Courir — tout ou rien", s =>
            {
                if (Rng.Next(100) < 35)
                {
                    s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(10, 25));
                    Narrator.Say("Tu coures. Ils crient. Tu coures plus vite. Ça marche, mais juste.", Color.Green);
                    StealthPhase3(s);
                }
                else
                {
                    s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(25, 50));
                    s.Reputation -= 25;
                    AdvanceDay(s); AdvanceDay(s); AdvanceDay(s);
                    Narrator.Say("Ils t'ont rattrapé. Ça s'est mal passé. -PV, -25 rép, +3 jours.", Color.Red);
                    Narrator.Pause();
                }
            }),
        ], Color.Yellow), state);
    }

    static void StealthPhase3(GameState state)
    {
        AnsiConsole.WriteLine();
        ChoiceMenu.Resolve(new Situation("Phase 3 — La sortie est là. Un garde en faction, dos tourné. Comment tu finis ça ?",
        [
            new("Attendre qu'il regarde ailleurs — la patience d'un saint", s =>
            {
                var wait = Rng.Next(100) < 75;
                if (wait)
                {
                    s.IsImprisoned = false;
                    s.PrisonEscapes++;
                    s.Reputation -= 20;
                    Narrator.Say("L'instant arrive. Tu passes. La porte. Le couloir. L'air libre. Tu ne te retournes pas. -20 réputation. Tu es dehors.", Color.Gold1);
                }
                else
                {
                    s.Reputation -= 15;
                    AdvanceDay(s);
                    Narrator.Say("Il ne se retourne pas. Mais le système de sécurité enregistre ton passage. Une alarme douce. Retour case départ. -15 rép. +1 jour.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("Neutraliser le garde discrètement", s =>
            {
                var bonus = s.Class.Name == "Seigneur de guerre" || s.Class.Name == "Vétéran" ? 20 : 0;
                if (Rng.Next(100) < 50 + bonus)
                {
                    s.IsImprisoned = false;
                    s.PrisonEscapes++;
                    s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(5, 15));
                    s.Reputation -= 25;
                    Narrator.Say("Silencieux. Efficace. Il dort. Tu sors. -25 réputation. -PV. Tu es dehors.", Color.Gold1);
                }
                else
                {
                    s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(20, 40));
                    s.Reputation -= 30;
                    AdvanceDay(s); AdvanceDay(s);
                    Narrator.Say("Il était pas aussi distrait que tu pensais. -PV sévère. -30 rép. +2 jours.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("Trouver un autre passage — chercher un sas secondaire", s =>
            {
                if (Rng.Next(100) < 60)
                {
                    s.IsImprisoned = false;
                    s.PrisonEscapes++;
                    s.Reputation -= 15;
                    Narrator.Say("Un conduit de maintenance. Étroit. Pas prévu pour les humains. Mais fonctionnel. Tu sors courbatu mais libre. -15 réputation.", Color.Green);
                }
                else
                {
                    AdvanceDay(s);
                    Narrator.Say("Le sas secondaire était verrouillé. Et surveillé. Retour en cellule. +1 jour.", Color.Red);
                }
                Narrator.Pause();
            }),
        ], Color.Yellow), state);
    }

    // ── NÉGOCIATION AVEC LE SUPERVISEUR ─────────────────────────────────────

    static void ResolveNegotiation(GameState state, int bail)
    {
        Narrator.Say("Le superviseur entre. Il a le regard de quelqu'un qui a entendu toutes les histoires possibles.", Color.Yellow);
        AnsiConsole.WriteLine();

        ChoiceMenu.Resolve(new Situation("Comment tu l'approches ?",
        [
            new("Proposer des informations confidentielles", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        s.Reputation -= 35;
                        s.IsImprisoned = false;
                        Narrator.Say("Il écoute. Il vérifie. Il hoche la tête. 'Utile.' Tu sors. -35 réputation. Tu as trahi quelqu'un.", Color.Red);
                        break;
                    case 1:
                        s.Reputation -= 15;
                        var cr = Rng.Next(400, 1000); s.Credits += cr;
                        s.IsImprisoned = false;
                        Narrator.Say($"Il apprécie. Il t'offre même une 'prime de collaboration'. -{15} rép. +{cr}cr. Tu sors proprement.", Color.Yellow);
                        break;
                    case 2:
                        s.Reputation -= 10;
                        AdvanceDay(s);
                        Narrator.Say("Tes infos sont périmées. Il hausse les épaules. 'Ramenez-le en cellule.' -10 rép. +1 jour.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),
            new("Proposer un service futur — travailler pour eux", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        s.IsImprisoned = false;
                        s.FactionMissions++;
                        Narrator.Say("Il a besoin d'un indépendant. Tu es disponible. Arrangement conclu. +1 mission faction. Tu sors avec une dette.", Color.Yellow);
                        break;
                    case 1:
                        AdvanceDay(s);
                        Narrator.Say("'On n'a pas besoin de toi.' Il part. +1 jour à méditer.", Color.Grey);
                        break;
                    case 2:
                        s.Reputation += 10;
                        s.IsImprisoned = false;
                        Narrator.Say("Il voit le potentiel. Il te relâche avec discrétion. +10 rép. La dette est implicite.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),
            new("Invoquer un vice-de-procédure — contester l'arrestation", s =>
            {
                var bonus = s.Class.Name == "Hackeur" ? 30 : s.Reputation > 100 ? 20 : 0;
                if (Rng.Next(100) < 20 + bonus)
                {
                    s.IsImprisoned = false;
                    s.Reputation += 20;
                    Narrator.Say("Il vérifie le dossier. Effectivement, quelque chose cloche. Il te relâche avec un air agacé. +20 rép.", Color.Green);
                }
                else
                {
                    AdvanceDay(s);
                    Narrator.Say("Il a lu le règlement plus longtemps que toi. 'Tout est en ordre.' +1 jour.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("← Annuler — pas maintenant", _ => { Narrator.Say("Il repart. La porte se referme.", Color.Grey); Narrator.Pause(); }),
        ], Color.Yellow), state);
    }

    static void AdvanceDay(GameState state)
    {
        state.Day++;
        Events.ApplyTravelEffects(state);
    }
}
