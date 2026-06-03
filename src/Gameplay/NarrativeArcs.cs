using Spectre.Console;

namespace VoidTrader;

/// <summary>
/// Arcs narratifs chaînés — quêtes en plusieurs étapes avec mémoire et conséquences.
/// Chaque arc a un id, des étapes numérotées, et des conditions de déclenchement.
/// </summary>
static class NarrativeArcs
{
    static readonly Random Rng = new();

    // ── IDs DES ARCS ─────────────────────────────────────────────────────────

    const string ArcAlanossa  = "arc_alanossa";
    const string ArcRaphazarus = "arc_raphazarus";
    const string ArcVael      = "arc_vael";
    const string ArcFaction   = "arc_faction";

    // ── POINT D'ENTRÉE — appelé à chaque arrivée en station ─────────────────

    public static void CheckTriggers(GameState state)
    {
        TryTriggerAlanossa(state);
        TryTriggerRaphazarus(state);
        TryTriggerVael(state);
        TryTriggerFaction(state);
        CheckProgress(state);
    }

    static int Progress(GameState state, string arc)
        => state.ArcProgress.GetValueOrDefault(arc, 0);

    static void SetProgress(GameState state, string arc, int step)
    {
        state.ArcProgress[arc] = step;
        if (!state.ActiveArcs.Contains(arc)) state.ActiveArcs.Add(arc);
    }

    static void CompleteArc(GameState state, string arc)
    {
        state.ActiveArcs.Remove(arc);
        state.CompletedArcs.Add(arc);
    }

    // ── ARC 1 : LA DETTE DE SANG (Alanossa) ─────────────────────────────────
    // Étape 0 → déclencheur : visiter Arc Ouest Apocalypse
    // Étape 1 → faire une mission pour Alanossa (tag "mission_alanossa")
    // Étape 2 → résolution : honorer ou trahir

    static void TryTriggerAlanossa(GameState state)
    {
        if (state.CompletedArcs.Contains(ArcAlanossa)) return;
        if (state.CurrentStation != "Arc Ouest Apocalypse") return;
        if (Progress(state, ArcAlanossa) > 0) return;

        // Premier contact : démarrage de l'arc
        if (NpcTracker.HasTag(state, "alanossa", "mission_alanossa")) return;

        SetProgress(state, ArcAlanossa, 1);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red]ARC — LA DETTE DE SANG[/]").RuleStyle("red"));
        Narrator.Say("Quelqu'un dans les couloirs d'Arc Ouest t'a remarqué. Un messager d'Alanossa t'aborde. 'Elle veut te parler. Pas maintenant — elle t'observe encore. Reviens.' L'arc démarre.", Color.Red);
        AnsiConsole.WriteLine();
    }

    static void TryTriggerRaphazarus(GameState state)
    {
        if (state.CompletedArcs.Contains(ArcRaphazarus)) return;
        if (Progress(state, ArcRaphazarus) > 0) return;

        // Déclencheur : explorer L'Arc Perdu avec de l'exploration avancée
        if (state.CurrentStation != "L'Arc Perdu") return;
        if (state.VisitedStations.Count < 5) return; // besoin d'expérience

        SetProgress(state, ArcRaphazarus, 1);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[magenta1]ARC — LE PROPHÈTE DU VIDE[/]").RuleStyle("magenta1"));
        Narrator.Say("Pendant ton exploration de L'Arc Perdu, tu trouves un symbole gravé dans le métal. Pas aléatoire — précis, répété, comme un message. Raphazarus sait que tu l'as vu. Il attend que tu déchiffres.", Color.Magenta1);
        state.UnlockedSecrets.Add("symboles_raphazarus");
        AnsiConsole.WriteLine();
    }

    static void TryTriggerVael(GameState state)
    {
        if (state.CompletedArcs.Contains(ArcVael)) return;
        if (Progress(state, ArcVael) > 0) return;
        if (state.CurrentStation != "Les Décombres de Vael") return;

        SetProgress(state, ArcVael, 1);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[grey]ARC — L'ENQUÊTE DE VAEL[/]").RuleStyle("grey"));
        Narrator.Say("En fouillant les Décombres, tu trouves un corps récent — pas une victime de la guerre ancienne. Quelqu'un est mort ici il y a moins d'une semaine. Personne d'autre n'a l'air de s'en inquiéter.", Color.Grey);
        state.UnlockedSecrets.Add("corps_vael");
        AnsiConsole.WriteLine();
    }

    static void TryTriggerFaction(GameState state)
    {
        if (state.CompletedArcs.Contains(ArcFaction)) return;
        if (Progress(state, ArcFaction) > 0) return;
        if (state.Faction == FactionId.None) return;
        if (state.FactionMissions < 2) return; // besoin de missions accomplie

        SetProgress(state, ArcFaction, 1);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[cyan1]ARC — LA GUERRE DES FACTIONS[/]").RuleStyle("cyan1"));
        var factionName = Factions.Info[state.Faction].Name;
        Narrator.Say($"Tes missions pour {factionName} n'ont pas été inaperçues. Leurs rivaux t'ont remarqué. Une escalade commence. Tu es maintenant impliqué dans quelque chose de plus grand que toi.", Color.Cyan1);
        AnsiConsole.WriteLine();
    }

    // ── PROGRESSION — appelée à chaque action clé ────────────────────────────

    static void CheckProgress(GameState state)
    {
        ProgressAlanossa(state);
        ProgressRaphazarus(state);
        ProgressVael(state);
        ProgressFaction(state);
    }

    static void ProgressAlanossa(GameState state)
    {
        var step = Progress(state, ArcAlanossa);
        if (step == 0 || state.CompletedArcs.Contains(ArcAlanossa)) return;

        // Étape 1 → 2 : a fait une mission pour Alanossa (tag "mission_alanossa")
        if (step == 1 && NpcTracker.HasTag(state, "alanossa", "mission_alanossa"))
        {
            SetProgress(state, ArcAlanossa, 2);
            AnsiConsole.WriteLine();
            Narrator.Say("Alanossa a entendu que tu as fait ce qu'elle demandait. Elle veut te revoir. Retourne à Arc Ouest Apocalypse pour la suite.", Color.Red);
        }

        // Étape 2 → résolution disponible à Arc Ouest
        if (step == 2 && state.CurrentStation == "Arc Ouest Apocalypse")
            ShowAlanossaResolution(state);
    }

    static void ShowAlanossaResolution(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red]ARC — LA DETTE DE SANG : RÉSOLUTION[/]").RuleStyle("red"));
        Narrator.Say("Alanossa te reçoit. Elle sait tout ce que tu as fait. Elle te regarde différemment cette fois.", Color.Red);
        AnsiConsole.WriteLine();

        var reaction = NpcTracker.ShowGreeting(state, "alanossa", "Alanossa", "Arc Ouest Apocalypse");

        ChoiceMenu.Resolve(new Situation("Alanossa attend ta décision finale.",
        [
            new("[gold1]Proposer une alliance permanente[/]  [grey dim](faction Faucons, avantages permanents)[/]", s =>
            {
                NpcTracker.MakeAlly(s, "alanossa", "Alanossa", "Arc Ouest Apocalypse");
                NpcTracker.RecordMeeting(s, "alanossa", "Alanossa", "Arc Ouest Apocalypse", 50, "Alliance");
                s.Faction = FactionId.Faucons;
                s.Reputation += 80;
                s.FactionMissions++;
                CompleteArc(s, ArcAlanossa);
                Narrator.Say("'Bienvenue dans les Faucons Noirs. Tu travailles avec nous maintenant.' Son ton dit que c'est définitif. +80 réputation. Faction Faucons débloquée.", Color.Gold1);
                Narrator.Pause();
            }),

            new("Rester indépendant mais garder le contact", s =>
            {
                NpcTracker.RecordMeeting(s, "alanossa", "Alanossa", "Arc Ouest Apocalypse", 20, "Indépendant");
                s.Reputation += 30;
                CompleteArc(s, ArcAlanossa);
                Narrator.Say("'Indépendant. J'aurais dû m'y attendre.' Elle hoche la tête. 'Ne travaille pas contre nous.' +30 réputation. La porte reste ouverte.", Color.Yellow);
                Narrator.Pause();
            }),

            new("[red]La trahir — vendre l'information sur ses opérations[/]", s =>
            {
                NpcTracker.MakeEnemy(s, "alanossa", "Alanossa", "Arc Ouest Apocalypse");
                NpcTracker.RecordMeeting(s, "alanossa", "Alanossa", "Arc Ouest Apocalypse", -100, "Trahi");
                var credits = Rng.Next(3000, 7000);
                s.Credits += credits;
                s.Reputation -= 50;
                CompleteArc(s, ArcAlanossa);
                Narrator.Say($"+{credits}cr. Tu as vendu ce que tu savais. Alanossa ne le sait pas encore. Mais elle le saura. Et elle ne pardonne pas.", Color.Red);
                Narrator.Pause();
            }),
        ], Color.Red), state);
    }

    static void ProgressRaphazarus(GameState state)
    {
        var step = Progress(state, ArcRaphazarus);
        if (step == 0 || state.CompletedArcs.Contains(ArcRaphazarus)) return;

        // Étape 1 → 2 : visiter L'Académie Stellaire pour déchiffrer les symboles
        if (step == 1 && state.CurrentStation == "L'Académie Stellaire"
            && state.UnlockedSecrets.Contains("symboles_raphazarus"))
        {
            SetProgress(state, ArcRaphazarus, 2);
            AnsiConsole.WriteLine();
            Narrator.Say("L'Archiviste Zenn reconnaît les symboles que tu décris. 'Ces inscriptions datent d'avant la Grande Guerre. Elles forment un message codé — un avertissement. Ou une invitation.' Elle t'en donne la clé.", Color.Cyan1);
            state.UnlockedSecrets.Add("cle_symboles");
        }

        // Étape 2 → 3 : retourner à L'Arc Perdu avec la clé
        if (step == 2 && state.CurrentStation == "L'Arc Perdu"
            && state.UnlockedSecrets.Contains("cle_symboles"))
        {
            SetProgress(state, ArcRaphazarus, 3);
            AnsiConsole.WriteLine();
            Narrator.Say("Avec la clé de Zenn, les symboles prennent sens. Ce n'était pas un avertissement. C'est une question — posée à quiconque saurait la lire. Raphazarus voulait voir qui pouvait répondre.", Color.Magenta1);
        }

        // Étape 3 → résolution
        if (step == 3 && state.CurrentStation == "L'Arc Perdu"
            && NpcTracker.HasTag(state, "raphazarus", "parle_symboles"))
            ShowRaphazarusResolution(state);
    }

    static void ShowRaphazarusResolution(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[magenta1]ARC — LE PROPHÈTE DU VIDE : RÉSOLUTION[/]").RuleStyle("magenta1"));
        NpcTracker.ShowGreeting(state, "raphazarus", "Raphazarus", "L'Arc Perdu");

        ChoiceMenu.Resolve(new Situation("Raphazarus attend une réponse à sa question.",
        [
            new("Répondre honnêtement — dire ce que tu as compris", s =>
            {
                NpcTracker.MakeAlly(s, "raphazarus", "Raphazarus", "L'Arc Perdu");
                NpcTracker.RecordMeeting(s, "raphazarus", "Raphazarus", "L'Arc Perdu", 60, "Répondu");
                s.Reputation += 100;
                // Donner le Sceptre
                var sceptre = WeaponPool.Tier5.First(w => w.Name == "Le Sceptre de Raphazarus");
                s.Weapons.Add(sceptre);
                s.UnlockedSecrets.Add("secret_vide");
                CompleteArc(s, ArcRaphazarus);
                Narrator.Say("'C'est la bonne réponse.' Il te tend quelque chose. 'Garde ça. Tu en auras besoin pour ce qui arrive.' Sceptre de Raphazarus obtenu. +100 réputation. Un secret du Vide déverrouillé.", Color.Gold1);
                Combat.ShowWeaponDrop(sceptre);
                Narrator.Pause();
            }),

            new("Admettre que tu ne comprends pas — rester honnête", s =>
            {
                NpcTracker.RecordMeeting(s, "raphazarus", "Raphazarus", "L'Arc Perdu", 20, "Honnête");
                s.Reputation += 40;
                s.Credits += Rng.Next(2000, 4000);
                CompleteArc(s, ArcRaphazarus);
                Narrator.Say("'L'honnêteté est rare.' Il te donne quelque chose quand même. 'Une autre fois peut-être.' +40 réputation.", Color.Yellow);
                Narrator.Pause();
            }),

            new("Mentir — prétendre avoir tout compris", s =>
            {
                if (Rng.Next(100) < 40)
                {
                    NpcTracker.MakeEnemy(s, "raphazarus", "Raphazarus", "L'Arc Perdu");
                    NpcTracker.RecordMeeting(s, "raphazarus", "Raphazarus", "L'Arc Perdu", -40, "Menti");
                    s.Reputation -= 30;
                    CompleteArc(s, ArcRaphazarus);
                    Narrator.Say("'Tu mens.' Il repart sans te regarder. Quelque chose vient de se fermer définitivement. -30 réputation.", Color.Red);
                }
                else
                {
                    s.Reputation += 20;
                    CompleteArc(s, ArcRaphazarus);
                    Narrator.Say("Il te croit ou fait semblant. Il te donne quelque chose. Tu te demanderas longtemps si tu t'en es sorti.", Color.Yellow);
                }
                Narrator.Pause();
            }),
        ], Color.Magenta1), state);
    }

    static void ProgressVael(GameState state)
    {
        var step = Progress(state, ArcVael);
        if (step == 0 || state.CompletedArcs.Contains(ArcVael)) return;

        // Étape 1 → 2 : interroger l'Ancien (tag "interroge_ancien")
        if (step == 1 && NpcTracker.HasTag(state, "ancien_vael", "interroge_vael"))
        {
            SetProgress(state, ArcVael, 2);
            Narrator.Say("L'Ancien t'a donné un nom. Ce nom est associé à une station que tu connais déjà. Quelqu'un a tué quelqu'un à Vael et est reparti. Tu peux suivre la piste.", Color.Grey);
        }

        // Étape 2 → 3 : trouver l'indice dans une autre station
        if (step == 2 && state.UnlockedSecrets.Contains("indice_vael_" + state.CurrentStation))
        {
            SetProgress(state, ArcVael, 3);
            Narrator.Say("Les pièces s'assemblent. Tu sais qui. Tu sais presque pourquoi. Il manque un dernier élément.", Color.Grey);
        }

        // Étape 3 : confrontation disponible
        if (step == 3 && state.UnlockedSecrets.Contains("coupable_vael"))
            ShowVaelResolution(state);
    }

    public static void ProgressVaelClue(GameState state, string station)
    {
        if (Progress(state, ArcVael) == 2)
            state.UnlockedSecrets.Add("indice_vael_" + station);
    }

    static void ShowVaelResolution(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[grey]ARC — L'ENQUÊTE DE VAEL : RÉSOLUTION[/]").RuleStyle("grey"));
        Narrator.Say("Tu as tout reconstitué. La mort n'était pas un accident. Le coupable est quelqu'un que tu as rencontré. Et il est là, dans cette station.", Color.Grey);

        ChoiceMenu.Resolve(new Situation("Tu sais qui a tué. Qu'est-ce que tu fais ?",
        [
            new("Confronter le coupable — le dénoncer publiquement", s =>
            {
                s.Reputation += 80;
                CompleteArc(s, ArcVael);
                Narrator.Say("Tu parles. Tout le monde écoute. Le coupable n'avait pas prévu d'être nommé en public. La communauté de Vael s'en souvient. +80 réputation.", Color.Green);
                Narrator.Pause();
            }),

            new("Le confronter en privé — lui laisser une chance de s'expliquer", s =>
            {
                var outcome = Rng.Next(3);
                switch (outcome)
                {
                    case 0:
                        s.Reputation += 40;
                        s.Credits += Rng.Next(1500, 4000);
                        CompleteArc(s, ArcVael);
                        Narrator.Say("Il explique. L'histoire est compliquée. Tu la crois à moitié. Tu prends une compensation et tu gardes le silence.", Color.Yellow);
                        break;
                    case 1:
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, new Enemy(
                            "Coupable acculé", 65, 12, 28, 400, 1200,
                            "Il n'avait pas prévu d'avoir à se défendre.", KillChance: 20)));
                        if (s.PlayerHp > 0)
                        {
                            s.Reputation += 50;
                            CompleteArc(s, ArcVael);
                        }
                        break;
                    case 2:
                        s.Reputation += 20;
                        CompleteArc(s, ArcVael);
                        Narrator.Say("Il nie. Tu n'as pas de preuve absolue. Tu le laisses partir. Avec un avertissement qu'il ne prend pas à la légère.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Garder l'information — elle a peut-être de la valeur", s =>
            {
                var credits = Rng.Next(2000, 6000);
                s.Credits += credits;
                s.Reputation -= 30;
                CompleteArc(s, ArcVael);
                Narrator.Say($"Tu vends ce que tu sais. La mort de Vael reste non résolue. +{credits}cr. -30 réputation.", Color.OrangeRed1);
                Narrator.Pause();
            }),
        ], Color.Grey), state);
    }

    static void ProgressFaction(GameState state)
    {
        var step = Progress(state, ArcFaction);
        if (step == 0 || state.CompletedArcs.Contains(ArcFaction)) return;
        if (state.Faction == FactionId.None) return;

        // Étape 1 → 2 : 4 missions faction accomplies
        if (step == 1 && state.FactionMissions >= 4)
        {
            SetProgress(state, ArcFaction, 2);
            AnsiConsole.WriteLine();
            var fName = Factions.Info[state.Faction].Name;
            Narrator.Say($"{fName} t'envoie un message prioritaire. 'Il est temps de passer aux choses sérieuses. On a besoin de toi pour une opération contre nos rivaux. C'est un choix que tu ne pourras pas défaire.'", Color.Cyan1);
        }

        // Étape 2 : choix final disponible à la station de faction
        if (step == 2 && state.FactionMissions >= 6)
            ShowFactionResolution(state);
    }

    static void ShowFactionResolution(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[cyan1]ARC — LA GUERRE DES FACTIONS : RÉSOLUTION[/]").RuleStyle("cyan1"));
        var fName    = Factions.Info[state.Faction].Name;
        var rivalFac = state.Faction switch
        {
            FactionId.Faucons  => FactionId.Gardiens,
            FactionId.Gardiens => FactionId.Faucons,
            FactionId.Emporium => FactionId.Culte,
            _                  => FactionId.Emporium,
        };
        var rName = Factions.Info[rivalFac].Name;

        Narrator.Say($"L'escalade entre {fName} et {rName} atteint son point critique. Ta faction te demande d'agir contre les intérêts rivaux de façon irrévocable.", Color.Cyan1);

        ChoiceMenu.Resolve(new Situation("Quel rôle tu joues dans cette guerre ?",
        [
            new($"Exécuter la mission — loyauté totale à {fName}", s =>
            {
                s.Reputation += 120;
                s.FactionMissions += 2;
                s.IsFactionLeader = true;
                FactionSystem.AddStanding(s, s.Faction, 200);
                FactionSystem.AddStanding(s, rivalFac, -300);
                CompleteArc(s, ArcFaction);
                Narrator.Say($"Tu exécutes. La guerre est déclarée officiellement. {fName} gagne du terrain. Toi tu deviens l'un des leurs de façon définitive. +120 réputation. Chef de faction débloqué.", Color.Gold1);
                Narrator.Pause();
            }),

            new("Devenir agent double — travailler pour les deux", s =>
            {
                s.IsDoubleAgent = true;
                s.Credits += Rng.Next(5000, 12000);
                FactionSystem.AddStanding(s, s.Faction, 50);
                FactionSystem.AddStanding(s, rivalFac, 50);
                CompleteArc(s, ArcFaction);
                Narrator.Say("Tu joues les deux camps. C'est risqué, lucratif, et instable. Les deux te font confiance. Pour l'instant.", Color.Yellow);
                Narrator.Pause();
            }),

            new($"Refuser — quitter {fName}", s =>
            {
                var oldFaction = s.Faction;
                s.Faction = FactionId.None;
                FactionSystem.AddStanding(s, oldFaction, -200);
                s.Reputation -= 40;
                CompleteArc(s, ArcFaction);
                Narrator.Say($"Tu quittes {fName}. Ils ne t'oublieront pas. Et pas de façon positive. -40 réputation.", Color.Red);
                Narrator.Pause();
            }),
        ], Color.Cyan1), state);
    }

    // ── AFFICHAGE DES ARCS ACTIFS ────────────────────────────────────────────

    public static void ShowActiveArcs(GameState state)
    {
        if (!state.ActiveArcs.Any() && !state.CompletedArcs.Any()) return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[gold1 bold]── ARCS NARRATIFS ──[/]");
        AnsiConsole.WriteLine();

        foreach (var arc in state.ActiveArcs)
        {
            var step  = state.ArcProgress.GetValueOrDefault(arc, 0);
            var label = arc switch
            {
                ArcAlanossa   => ("La Dette de Sang",      Color.Red),
                ArcRaphazarus => ("Le Prophète du Vide",   Color.Magenta1),
                ArcVael       => ("L'Enquête de Vael",     Color.Grey),
                ArcFaction    => ("La Guerre des Factions",Color.Cyan1),
                _             => (arc,                     Color.White),
            };
            AnsiConsole.MarkupLine($"  [{label.Item2}]⟳ {label.Item1}[/]  [grey dim]étape {step}[/]");
        }

        foreach (var arc in state.CompletedArcs)
        {
            var label = arc switch
            {
                ArcAlanossa   => "La Dette de Sang",
                ArcRaphazarus => "Le Prophète du Vide",
                ArcVael       => "L'Enquête de Vael",
                ArcFaction    => "La Guerre des Factions",
                _             => arc,
            };
            AnsiConsole.MarkupLine($"  [grey]✔ {label} (terminé)[/]");
        }
        AnsiConsole.WriteLine();
    }
}
