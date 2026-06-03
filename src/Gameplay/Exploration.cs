using Spectre.Console;

namespace VoidTrader;

static class Exploration
{
    private static readonly Random Rng = new();

    public static void Explore(GameState state)
    {
        while (true)
        {
            RunZone(state, state.ZoneDepth);
            if (state.PlayerHp <= 0 || state.IsImprisoned || state.IsDead) return;

            state.ZoneDepth++;
            var depth = state.ZoneDepth;
            var dangerNote = depth switch
            {
                1 => "[yellow]Plus tu t'enfonces, plus c'est risqué.[/]",
                2 => "[orange1]T'es loin maintenant. Les dangers augmentent.[/]",
                3 => "[red]Tu pousses vraiment ta chance.[/]",
                _ => "[red bold]Personne ne s'aventure aussi profond. Personne ne revient en parler.[/]",
            };
            AnsiConsole.MarkupLine($"\n  Profondeur [white]{depth}[/] — {dangerNote}");

            var cont = ChoiceMenu.Present(new Situation("Tu continues ?",
            [
                new("Continuer l'exploration"),
                new("Rentrer"),
            ]), state);

            if (cont is null || cont.Label.Contains("Rentrer")) return;
        }
    }

    static void RunZone(GameState state, int depth)
    {
        StationAmbiance.Show(state.CurrentStation, isExploration: true);
        if (MaybeFatalZoneEvent(state, depth)) return;

        var station = Universe.Get(state.CurrentStation);
        if (depth >= 4 && Universe.Danger(station) >= 2
            && !state.StationBossesBeaten.Contains(state.CurrentStation)
            && Rng.Next(100) < 55)
        {
            TriggerStationBoss(state);
            return;
        }

        // 40% de chance : event JSON (nouveau contenu), 60% : event C# (existant)
        var zoneType = GetZoneType(state.CurrentStation);
        if (Rng.Next(100) < 40)
        {
            JsonEvents.RunExplorationEvent(state, zoneType);
            return;
        }

        switch (state.CurrentStation)
        {
            case "La Carcasse" or "Les Bas-Fonds de Vega" or "Fort Kharos"
            or "Port des Brumes" or "Arc Ouest Apocalypse" or "Le Purgatoire":
                ExploreDangerous(state); break;

            case "Port Méridien" or "Terminus Sud" or "Nexus Aldara" or "Colonie Perséphone"
            or "Le Sanctuaire des Dérives" or "Station Belvédère":
                ExplorePeaceful(state); break;

            case "Forge Alpha" or "La Ferronnerie" or "L'Arc du Pic de l'Est" or "Sanctum Machina":
                ExploreIndustrial(state); break;

            case "La Bulle" or "Les Abysses de Velkor" or "L'Académie Stellaire":
                ExploreScientific(state); break;

            case "Les Décombres de Vael" or "Épave de l'Aurore Noire"
            or "Le Vaisseau Fantôme Errant" or "L'Arc Perdu":
                ExploreRuins(state); break;

            case "Esmeralda":
                ExploreNature(state); break;

            case "Scotty Golden North" or "Star Quest" or "La Couronne d'Eos":
                ExploreLuxury(state); break;

            case "Emporium Requiem" or "La Citadelle Écarlate" or "Fort Ossian" or "La Citadelle":
                ExploreMilitary(state); break;

            default:
                ExploreGeneric(state); break;
        }
    }

    static string GetZoneType(string station) => station switch
    {
        "La Carcasse" or "Les Bas-Fonds de Vega" or "Fort Kharos"
        or "Port des Brumes" or "Arc Ouest Apocalypse" or "Le Purgatoire" => "dangerous",

        "Port Méridien" or "Terminus Sud" or "Nexus Aldara" or "Colonie Perséphone"
        or "Le Sanctuaire des Dérives" or "Station Belvédère" => "peaceful",

        "Forge Alpha" or "La Ferronnerie" or "L'Arc du Pic de l'Est" or "Sanctum Machina"
        or "La Forge des Damnés" or "L'Entrepôt Zéro" => "industrial",

        "La Bulle" or "Les Abysses de Velkor" or "L'Académie Stellaire"
        or "L'Observatoire" => "scientific",

        "Les Décombres de Vael" or "Épave de l'Aurore Noire"
        or "Le Vaisseau Fantôme Errant" or "L'Arc Perdu" => "ruins",

        "Esmeralda" or "Colonie Perséphone" => "nature",

        "Scotty Golden North" or "Star Quest" or "La Couronne d'Eos"
        or "Emporium Requiem" or "Station Belvédère" => "luxury",

        "Emporium Requiem" or "La Citadelle Écarlate" or "Fort Ossian"
        or "La Citadelle" or "Avant-Poste Kalem" or "Fort Kharos" => "military",

        _ => "generic",
    };

    // ── CATASTROPHES ────────────────────────────────────────────────────────────

    static bool MaybeFatalZoneEvent(GameState state, int depth)
    {
        var danger = Universe.Danger(Universe.Get(state.CurrentStation));
        if (danger < 3) return false;

        var catastropheChance = (danger - 2) * 5 + depth * 3;
        if (Rng.Next(100) >= catastropheChance) return false;

        var canKill = danger >= 4;
        var fatal   = canKill && Rng.Next(100) < 35;

        var deaths = new[]
        {
            ("Une armée de bandits",   "Tu débouches en plein milieu d'un campement de pillards. Des dizaines. Ils te voient avant que tu puisses reculer."),
            ("Un monstre colossal",    "Le sol tremble. Une chose immense se dresse. Tu n'es même pas un repas. Juste une gêne qu'on écrase."),
            ("Le vide",                "Une passerelle cède sous ton poids. Pas de rambarde, pas de filet, pas de seconde chance."),
            ("Un piège mortel ancien", "Un système de défense automatisé se réveille. Tu comprends une fraction de seconde trop tard."),
        };

        if (fatal)
        {
            var (title, text) = deaths[Rng.Next(deaths.Length)];
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[red bold]{title}[/]").RuleStyle("red"));
            Narrator.Say(text, Color.Red);
            state.PlayerHp   = 0;
            state.IsDead     = true;
            state.DeathCause = $"{title} — {state.CurrentStation}, profondeur {depth}.";
            Narrator.Pause();
            return true;
        }

        // Piège survivable avec un CHOIX de réaction
        ChoiceMenu.Resolve(new Situation("Un danger surgit. Tu réagis comment ?", [
            new("Plonger et encaisser", s =>
            {
                var hp = Rng.Next(25, 55);
                s.PlayerHp = Math.Max(1, s.PlayerHp - hp);
                Narrator.Say($"T'encaisses le coup. -{hp} PV. Tu traverses, amochée mais debout.", Color.Red);
                Narrator.Pause();
            }),
            new("Sprint et esquive", s =>
            {
                if (Rng.Next(100) < 55)
                { Narrator.Say("T'esquives de justesse. Rien. Adrénaline à fond.", Color.Green); }
                else
                {
                    var hp = Rng.Next(12, 30);
                    s.PlayerHp = Math.Max(1, s.PlayerHp - hp);
                    Narrator.Say($"Presque. -{hp} PV. Un éclat t'attrape en fuyant.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("Neutraliser la source", s =>
            {
                var e = Combat.GetScaled(s, depth + 1);
                Narrator.Say("T'attaques directement la menace.", Color.OrangeRed1);
                Situations.ApplyCombatOutcome(s, Combat.Start(s, e));
            }),
        ], Color.Red), state);
        return true;
    }

    static void TriggerStationBoss(GameState state)
    {
        var boss = Combat.StationBoss(state);
        if (boss is null) return;

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[red bold]LE MAÎTRE DES LIEUX[/]").RuleStyle("red"));
        Narrator.Say($"Au bout des couloirs les plus profonds, une présence t'attendait. {boss.Name}.", Color.Red);
        AnsiConsole.MarkupLine($"[grey]{boss.Description}[/]");

        var choice = ChoiceMenu.Present(new Situation("Tu fais quoi ?",
        [
            new("L'affronter"),
            new("Reculer doucement"),
        ]), state);

        if (choice is null || choice.Label.Contains("Reculer"))
        {
            Narrator.Say("Tu recules sans le quitter des yeux. Il te laisse partir. Pour l'instant.", Color.Grey);
            Narrator.Pause();
            return;
        }

        var outcome = Combat.Start(state, boss);
        if (outcome == CombatOutcome.Victory)
        {
            state.StationBossesBeaten.Add(state.CurrentStation);
            state.Reputation += 40;
            Display.ShowEvent($"Tu as terrassé le maître de {state.CurrentStation}. +40 réputation.", Color.Gold1);
            QuestSystem.CheckKillQuests(state, state.CurrentStation);
            Narrator.Pause();
        }
        else Situations.ApplyCombatOutcome(state, outcome);
    }

    // ── HELPER ──────────────────────────────────────────────────────────────────

    // Lance une scène d'un pool de façon aléatoire
    static void RollScene(GameState state, List<Action<GameState>> pool)
        => pool[Rng.Next(pool.Count)](state);

    // ─────────────────────────────────────────────────────────────────────────────
    // ZONES DANGEREUSES (La Carcasse, Bas-Fonds, Fort Kharos, etc.)
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreDangerous(GameState state)
    {
        var intros = new[]
        {
            "Tu pousses une porte rouillée marquée DANGER. Ce n'est pas une suggestion.",
            "Des couloirs où la lumière a abandonné l'idée de fonctionner.",
            "Tu suis une odeur suspecte vers une section qui n'existe pas officiellement.",
            "Chaque porte fermée ici cache quelque chose. Certaines sont préférables fermées.",
        };
        Narrator.Say(intros[Rng.Next(intros.Length)], Color.Red);

        RollScene(state, [
            // Scène 1 : Corps frais
            s => {
                ChoiceMenu.Resolve(new Situation("Un corps frais. Encore chaud. Ses crédits sont là.", [
                    new("Prendre les crédits et partir vite", gs => {
                        var cr = Rng.Next(200, 700);
                        gs.Credits += cr; gs.Reputation -= 5;
                        Narrator.Say($"T'as pris. +{cr}cr. T'as essayé de pas penser à qui c'était.", Color.Gold1);
                        Narrator.Pause();
                    }),
                    new("Fouiller aussi ses affaires", gs => {
                        var cr = Rng.Next(200, 600);
                        gs.Credits += cr;
                        if (Rng.Next(2) == 0) { gs.Cargo.Add("Armes", 1); Display.ShowEvent("+1 Arme trouvée sur lui.", Color.Cyan1); }
                        gs.Reputation -= 10;
                        if (Rng.Next(100) < 30) {
                            Narrator.Say("Quelqu'un arrive. Ils t'ont vu fouiller.", Color.Red);
                            var e = Combat.GetScaled(gs, gs.ZoneDepth);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                        } else { Narrator.Say($"Personne. Tu repartis avec tout. +{cr}cr.", Color.Gold1); Narrator.Pause(); }
                    }),
                    new("Repartir sans toucher — trop récent", gs => {
                        Narrator.Say("T'es parti. Ceux qui ont fait ça sont peut-être encore dans le coin.", Color.Grey);
                        if (Rng.Next(100) < 40) {
                            Narrator.Say("Ils étaient là.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth)));
                        } else Narrator.Pause();
                    }),
                ], Color.Red), s);
            },

            // Scène 2 : Bifurcation de couloir
            s => {
                ChoiceMenu.Resolve(new Situation("Le couloir bifurque. À gauche : du bruit. À droite : silence total.", [
                    new("Aller vers le bruit", gs => {
                        switch (Rng.Next(3)) {
                            case 0:
                                Narrator.Say("Un atelier clandestin. Des ouvriers qui fabriquent quelque chose d'illégal. Ils t'ont vu.", Color.OrangeRed1);
                                ChoiceMenu.Resolve(new Situation("Qu'est-ce que tu fais ?", [
                                    new("Proposer de racheter ta discrétion", ggs => {
                                        var cout = Rng.Next(200, 600);
                                        if (ggs.Credits >= cout) { ggs.Credits -= cout; Narrator.Say($"Marché conclu. -{cout}cr. Ils reprennent le travail.", Color.Yellow); }
                                        else { Narrator.Say("T'as pas assez. L'ambiance tourne mal.", Color.Red); Situations.ApplyCombatOutcome(ggs, Combat.Start(ggs, Combat.GetScaled(ggs, ggs.ZoneDepth))); }
                                        Narrator.Pause();
                                    }),
                                    new("Negocier du butin contre ton silence", ggs => {
                                        ggs.Cargo.Add("Marchandises illégales", Rng.Next(1,3));
                                        ggs.Reputation -= 10;
                                        Narrator.Say("Ils t'offrent quelque chose pour fermer les yeux. Tu prends.", Color.OrangeRed1);
                                        Narrator.Pause();
                                    }),
                                    new("Les attaquer — récupérer leur stock", ggs => {
                                        Narrator.Say("Ils étaient plus nombreux que prévu.", Color.Red);
                                        Situations.ApplyCombatOutcome(ggs, Combat.Start(ggs, Combat.GetScaled(ggs, ggs.ZoneDepth + 1)));
                                    }),
                                ], Color.OrangeRed1), gs);
                                break;
                            case 1:
                                var cr = Rng.Next(300, 800);
                                gs.Credits += cr;
                                Narrator.Say($"Un pari en cours entre quelques durs à cuire. Tu te glisses et tu gagnes ta mise. +{cr}cr.", Color.Gold1);
                                Narrator.Pause();
                                break;
                            case 2:
                                Narrator.Say("Un garde corrompu et son crew. Ils t'ont repéré.", Color.Red);
                                Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth)));
                                break;
                        }
                    }),
                    new("Aller vers le silence", gs => {
                        switch (Rng.Next(3)) {
                            case 0:
                                var cr = Rng.Next(400, 1200);
                                gs.Credits += cr;
                                Narrator.Say($"Une planque. Quelqu'un n'est pas revenu la chercher. +{cr}cr.", Color.Gold1);
                                Narrator.Pause();
                                break;
                            case 1:
                                Narrator.Say("Une salle vide avec une caisse verrouillée.", Color.Yellow);
                                ChoiceMenu.Resolve(new Situation("La caisse ?", [
                                    new("Forcer", ggs => {
                                        if (Rng.Next(2) == 0) { var b = Rng.Next(500, 1500); ggs.Credits += b; Narrator.Say($"+{b}cr dans la caisse.", Color.Gold1); }
                                        else { var d = Rng.Next(20, 45); ggs.PlayerHp = Math.Max(1, ggs.PlayerHp - d); Narrator.Say($"Piégée. -{d} PV.", Color.Red); }
                                        Narrator.Pause();
                                    }),
                                    new("Laisser — trop risqué", ggs => { Narrator.Say("T'as laissé tomber. Sage.", Color.Grey); Narrator.Pause(); }),
                                ], Color.Yellow), gs);
                                break;
                            case 2:
                                var hp = Rng.Next(15, 35);
                                gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp);
                                Narrator.Say($"Quelque chose dans l'ombre. T'as pas eu le temps de réagir. -{hp} PV.", Color.Red);
                                Narrator.Pause();
                                break;
                        }
                    }),
                    new("Rester en observation — attendre de voir", gs => {
                        if (Rng.Next(2) == 0) {
                            var cr = Rng.Next(100, 400);
                            gs.Credits += cr;
                            Narrator.Say($"Depuis ta cachette, t'as vu quelque chose passer et tu l'as suivi jusqu'à un butin. +{cr}cr.", Color.Cyan1);
                        } else {
                            Narrator.Say("Pendant que t'observais, quelqu'un t'observait aussi.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth)));
                        }
                        Narrator.Pause();
                    }),
                ], Color.OrangeRed1), s);
            },

            // Scène 3 : PNJ blessé sur les lieux
            s => {
                ChoiceMenu.Resolve(new Situation("Un homme adossé contre un mur. Blessé par balle. Il te regarde approcher.", [
                    new("L'aider — soigner ou écouter", gs => {
                        if (gs.Cargo.Get("Médicaments") > 0) {
                            gs.Cargo.Remove("Médicaments", 1);
                            gs.Reputation += 20;
                            var cr = Rng.Next(400, 1200);
                            gs.Credits += cr;
                            Narrator.Say($"Tu le soignes. Il sort quelque chose de sa veste. 'Merci. Prends ça.' +{cr}cr, +20 rép.", Color.Green);
                        } else {
                            gs.Reputation += 10;
                            Narrator.Say("T'as rien pour soigner mais t'as attendu avec lui. Il t'a donné une info avant de perdre conscience.", Color.Cyan1);
                        }
                        Narrator.Pause();
                    }),
                    new("L'interroger — qui t'a fait ça ?", gs => {
                        switch (Rng.Next(3)) {
                            case 0:
                                var cr = Rng.Next(500, 1500);
                                gs.Credits += cr;
                                Narrator.Say($"Il parle. Il te dit où est la planque de ceux qui l'ont tiré. Tu y vas seul. Bien décidé. +{cr}cr.", Color.Gold1);
                                break;
                            case 1:
                                gs.Reputation -= 20;
                                Narrator.Say("Il parle. Ce qu'il dit te met dans une sale situation. T'aurais pas dû demander.", Color.Red);
                                break;
                            case 2:
                                Narrator.Say("Il dit un nom. Tu le reconnais. Ça change quelque chose.", Color.Yellow);
                                gs.Reputation += 5;
                                break;
                        }
                        Narrator.Pause();
                    }),
                    new("Le fouiller — il survivra pas de toute façon", gs => {
                        var cr = Rng.Next(150, 600);
                        gs.Credits += cr; gs.Reputation -= 25;
                        Narrator.Say($"Il te regarde faire. Il dit rien. Pire que s'il criait. -{25} réputation, +{cr}cr.", Color.Red);
                        Narrator.Pause();
                    }),
                    new("Passer sans s'arrêter", gs => {
                        if (Rng.Next(3) == 0) { gs.Reputation -= 10; Display.ShowEvent("Quelqu'un t'a vu passer. -10 rép.", Color.Grey); }
                        Narrator.Pause();
                    }),
                ], Color.SteelBlue1), s);
            },

            // Scène 4 : Transaction illégale en cours
            s => {
                ChoiceMenu.Resolve(new Situation("Tu tombes sur une transaction en cours entre deux gangs. Ils t'ont vu.", [
                    new("Rester immobile — jouer les ombres", gs => {
                        var chance = 40 + (gs.Reputation < 0 ? 20 : 0);
                        if (Rng.Next(100) < chance) { Narrator.Say("Ils t'ignorent. La transaction continue. T'es personne.", Color.Grey); Narrator.Pause(); }
                        else { Narrator.Say("'Toi. T'as vu quelque chose ?' L'ambiance est mauvaise.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth))); }
                    }),
                    new("S'interposer et prendre une part", gs => {
                        if (Rng.Next(100) < 35) {
                            var cr = Rng.Next(500, 1500); gs.Credits += cr; gs.Reputation -= 15;
                            Narrator.Say($"Audace. Ils ont ri. Puis ils ont payé. +{cr}cr.", Color.Gold1);
                        } else {
                            Narrator.Say("Mauvais tirage. Ils ont pas apprécié.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth + 1)));
                        }
                        Narrator.Pause();
                    }),
                    new("Attaquer le premier groupe et prendre tout", gs => {
                        Narrator.Say("T'as l'effet de surprise. Mais ils sont deux groupes.", Color.OrangeRed1);
                        var e = Combat.Scale(Combat.GetScaled(gs, gs.ZoneDepth), 1);
                        var outcome = Combat.Start(gs, e);
                        if (outcome == CombatOutcome.Victory) {
                            var cr = Rng.Next(800, 2500); gs.Credits += cr;
                            Display.ShowEvent($"Tu récupères les deux lots. +{cr}cr.", Color.Gold1);
                        } else Situations.ApplyCombatOutcome(gs, outcome);
                    }),
                    new("Repartir discrètement par où t'es venu", gs => {
                        Narrator.Say("T'as vu quelque chose. T'as choisi de pas l'avoir vu. Sage.", Color.Grey);
                        Narrator.Pause();
                    }),
                ], Color.OrangeRed1), s);
            },

            // Scène 5 : Prisonnier enchaîné
            s => {
                ChoiceMenu.Resolve(new Situation("Une personne enchaînée à un tuyau. Elle murmure d'approcher.", [
                    new("La libérer", gs => {
                        switch (Rng.Next(3)) {
                            case 0:
                                gs.Reputation += 30;
                                var cr = Rng.Next(600, 1800);
                                gs.Credits += cr;
                                Narrator.Say($"Elle te remercie. C'était quelqu'un d'important. +{cr}cr, +30 rép.", Color.Gold1);
                                break;
                            case 1:
                                gs.Reputation -= 25;
                                Narrator.Say("C'était un appât. Ceux qui l'avaient mise là arrivent.", Color.Red);
                                Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth + 1)));
                                return;
                            case 2:
                                gs.Reputation += 10;
                                Narrator.Say("Elle part sans un mot. Un peu après, tu trouves quelque chose posé à ta place habituelle.", Color.Cyan1);
                                gs.Cargo.Add("Médicaments", 2);
                                break;
                        }
                        Narrator.Pause();
                    }),
                    new("L'interroger d'abord — qui es-tu ?", gs => {
                        var cr = Rng.Next(300, 900);
                        gs.Credits += cr; gs.Reputation += 5;
                        Narrator.Say($"Elle raconte. C'est utile. Tu la libères après. +{cr}cr d'infos, +5 rép.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("La laisser — pas ton problème", gs => {
                        gs.Reputation -= 15;
                        Narrator.Say("T'as laissé. Elle t'a regardé partir. Ce regard, tu t'en souviendras. -15 rép.", Color.Red);
                        Narrator.Pause();
                    }),
                ], Color.SteelBlue1), s);
            },

            // Scène 6 : Embuscade — 1 ou plusieurs ennemis
            s => {
                var depth = s.ZoneDepth;
                if (Rng.Next(100) < 45)
                {
                    // Multi-ennemis (2-3)
                    var count   = Rng.Next(2, 4);
                    var enemies = Enumerable.Range(0, count).Select(_ => Combat.GetScaled(s, depth)).ToList();
                    Narrator.Say($"Un groupe de {count} individus armés vous bloque le passage. Il n'y a pas de sortie.", Color.Red);
                    Situations.ApplyCombatOutcome(s, Combat.StartMulti(s, enemies));
                }
                else
                {
                    var enemy = Combat.GetScaled(s, depth);
                    Narrator.Say("Une silhouette bloque le couloir. Pas de discussion cette fois.", Color.Red);
                    Situations.ApplyCombatOutcome(s, Combat.Start(s, enemy));
                }
            },

            // Scène 7 : Caisse piégée / non piégée
            s => {
                ChoiceMenu.Resolve(new Situation("Une grosse caisse métallique, pas de serrure apparente. Abandonnée là.", [
                    new("Ouvrir brutalement", gs => {
                        switch (Rng.Next(4)) {
                            case 0: var cr = Rng.Next(600, 1800); gs.Credits += cr; Narrator.Say($"+{cr}cr en billets froissés.", Color.Gold1); break;
                            case 1: gs.Cargo.Add("Armes", 1); gs.Cargo.Add("Explosifs", 1); Narrator.Say("Armements. Quelqu'un préparait quelque chose. +1 Arme, +1 Explosif.", Color.Cyan1); break;
                            case 2: var hp = Rng.Next(30, 60); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp); Narrator.Say($"Piégée. Gaz ou éclats. -{hp} PV.", Color.Red); break;
                            case 3: Narrator.Say("Vide. Propre. Trop propre. Quelqu'un la regardait peut-être.", Color.Grey); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Secouer et écouter avant d'ouvrir", gs => {
                        if (Rng.Next(100) < 60) {
                            var cr = Rng.Next(400, 1200); gs.Credits += cr;
                            Narrator.Say($"T'as eu le bon réflexe. Elle était pas piégée. +{cr}cr.", Color.Gold1);
                        } else {
                            Narrator.Say("Aucun indice. Tu l'ouvres quand même. Erreur.", Color.Red);
                            var hp = Rng.Next(20, 40); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp);
                            Display.ShowEvent($"-{hp} PV. Piège.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Chercher à qui appartient cette caisse", gs => {
                        switch (Rng.Next(3)) {
                            case 0:
                                Narrator.Say("Le propriétaire arrive pendant que tu cherches. C'est un type très bien armé.", Color.Red);
                                Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth)));
                                return;
                            case 1:
                                gs.Reputation += 20; var cr = Rng.Next(300, 800); gs.Credits += cr;
                                Narrator.Say($"Le propriétaire était dans la station. Il te récompense de ton honnêteté. +{cr}cr, +20 rép.", Color.Green);
                                break;
                            case 2:
                                Narrator.Say("Introuvable. Probablement mort. La caisse est à toi.", Color.Gold1);
                                var c = Rng.Next(500, 1400); gs.Credits += c; Display.ShowEvent($"+{c}cr.", Color.Gold1);
                                break;
                        }
                        Narrator.Pause();
                    }),
                ], Color.Yellow), s);
            },

            // Scène 8 : Gang local
            s => {
                var n = Rng.Next(3) + 2;
                ChoiceMenu.Resolve(new Situation($"Un gang de {n} types te barre la route. Ils t'ont reconnu ou ils s'en foutent — dans les deux cas t'es dans leurs pattes.", [
                    new("Payer pour passer", gs => {
                        var cout = Rng.Next(150, 500);
                        if (gs.Credits >= cout) {
                            gs.Credits -= cout;
                            Narrator.Say($"Tu paies. Ils s'écartent. -{cout}cr. La prochaine fois ils demanderont plus.", Color.Yellow);
                        } else {
                            Narrator.Say("T'as pas assez. Ça tourne mal.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth)));
                            return;
                        }
                        Narrator.Pause();
                    }),
                    new("Intimider — tu travailles pour quelqu'un de pire", gs => {
                        var chance = 20 + Math.Max(0, -gs.Reputation / 5) + (gs.Class.Name == "Seigneur de guerre" ? 30 : 0);
                        if (Rng.Next(100) < chance) {
                            gs.Reputation -= 10;
                            Narrator.Say("Ils hésitent. Puis ils s'écartent. Quelque chose dans ta façon de parler.", Color.Green);
                        } else {
                            Narrator.Say("Ils rigolent. Puis ils avancent.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth)));
                            return;
                        }
                        Narrator.Pause();
                    }),
                    new("Foncer droit dans le tas", gs => {
                        var e = Combat.Scale(Combat.GetScaled(gs, gs.ZoneDepth), 1);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                    }),
                    new("Rebrousser chemin — un autre couloir", gs => {
                        if (Rng.Next(100) < 40) {
                            Narrator.Say("T'as fait demi-tour. Bien. Puis t'as tourné en rond et perdu une heure.", Color.Grey);
                        } else {
                            Narrator.Say("Ils t'ont suivi. T'avais cru les semer.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth)));
                            return;
                        }
                        Narrator.Pause();
                    }),
                ], Color.Red), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ZONES PACIFIQUES (Port Méridien, Nexus, Sanctuaire...)
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExplorePeaceful(GameState state)
    {
        var intros = new[]
        {
            "Tu te promènes dans les rues propres. C'est presque suspect.",
            "Tu explores comme un touriste. Les gens sourient. Ta main reste près de tes crédits.",
            "Les gardes ici ont l'air d'avoir été payés pour ça.",
        };
        Narrator.Say(intros[Rng.Next(intros.Length)], Color.Grey);

        RollScene(state, [
            // Scène 1 : Dispute à arbitrer
            s => {
                ChoiceMenu.Resolve(new Situation("Deux marchands se disputent violemment devant tout le monde. Ça va dégénérer.", [
                    new("S'interposer et arbitrer", gs => {
                        switch (Rng.Next(3)) {
                            case 0: gs.Reputation += 25; var cr = Rng.Next(300, 700); gs.Credits += cr; Narrator.Say($"T'as trouvé le compromis que ni l'un ni l'autre aurait trouvé. Ils t'ont récompensé. +{cr}cr, +25 rép.", Color.Green); break;
                            case 1: gs.Reputation += 10; Narrator.Say("T'as calmé les choses. Pas de récompense mais la station sait que t'as fait quelque chose. +10 rép.", Color.Cyan1); break;
                            case 2: gs.PlayerHp = Math.Max(1, gs.PlayerHp - Rng.Next(8, 20)); Narrator.Say("Ils ont tous les deux arrêté de se disputer pour s'en prendre à toi. C'est injuste. -PV.", Color.Red); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Filmer et miser sur l'issue", gs => {
                        if (Rng.Next(2) == 0) { var cr = Rng.Next(100, 400); gs.Credits += cr; Narrator.Say($"T'as parié sur le bon. +{cr}cr.", Color.Gold1); }
                        else { var l = Rng.Next(100, 400); gs.Credits = Math.Max(0, gs.Credits - l); Narrator.Say($"Mauvais pari. -{l}cr.", Color.Red); }
                        Narrator.Pause();
                    }),
                    new("Profiter de la distraction pour faire les poches", gs => {
                        var cr = Rng.Next(100, 500); gs.Credits += cr; gs.Reputation -= 15;
                        Narrator.Say($"Ils étaient tellement occupés. +{cr}cr, -15 rép.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new("Ignorer", gs => { Narrator.Say("T'as enjambé la dispute. Personne ne t'a remarqué.", Color.Grey); Narrator.Pause(); }),
                ], Color.Yellow), s);
            },

            // Scène 2 : Inconnu en détresse
            s => {
                ChoiceMenu.Resolve(new Situation("Une personne s'effondre devant toi. Malaise ou quelque chose de pire.", [
                    new("S'arrêter et aider", gs => {
                        var med = gs.Cargo.Get("Médicaments") > 0;
                        if (med) gs.Cargo.Remove("Médicaments", 1);
                        gs.Reputation += med ? 25 : 15;
                        if (Rng.Next(100) < 50) { var cr = Rng.Next(300, 900); gs.Credits += cr; Narrator.Say($"Elle reprend ses esprits. Très reconnaissante. +{cr}cr, +rép.", Color.Green); }
                        else Narrator.Say("Tu l'as aidée. Elle remercie et repart. C'est tout. Mais ça compte. +rép.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                    new("Appeler du secours et s'éloigner", gs => {
                        gs.Reputation += 8;
                        Narrator.Say("Les secours arrivent. T'étais déjà loin. Quelqu'un t'a quand même reconnu. +8 rép.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Fouiller discrètement pendant qu'elle est inconsciente", gs => {
                        var cr = Rng.Next(100, 400); gs.Credits += cr; gs.Reputation -= 30;
                        Narrator.Say($"T'as fouillé quelqu'un d'inconscient dans une zone publique. Des gens t'ont vu. -30 rép, +{cr}cr.", Color.Red);
                        Narrator.Pause();
                    }),
                ], Color.SteelBlue1), s);
            },

            // Scène 3 : Opportunité de deal honnête
            s => {
                var paye = Rng.Next(400, 1000);
                ChoiceMenu.Resolve(new Situation($"Un marchand cherche quelqu'un de confiance pour une livraison rapide. {paye}cr. Aucune question.", [
                    new("Accepter", gs => {
                        switch (Rng.Next(3)) {
                            case 0: gs.Credits += paye; gs.Reputation += 10; Narrator.Say($"Livraison sans accroc. +{paye}cr, +10 rép.", Color.Green); break;
                            case 1: gs.Credits += paye * 2; gs.Reputation += 5; Narrator.Say($"Le destinataire a doublé la mise. +{paye * 2}cr.", Color.Gold1); break;
                            case 2: gs.Reputation -= 20; gs.IsImprisoned = true; Narrator.Say("La douane t'attendait. Il t'a vendu.", Color.Red); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Négocier le tarif", gs => {
                        var negoChance = 50 + (gs.Reputation > 0 ? 20 : 0);
                        if (Rng.Next(100) < negoChance) { gs.Credits += (int)(paye * 1.4); Narrator.Say($"Il a accepté le tarif supérieur. +{(int)(paye*1.4)}cr.", Color.Gold1); }
                        else { Narrator.Say("Il a trouvé quelqu'un d'autre.", Color.Grey); }
                        Narrator.Pause();
                    }),
                    new("Refuser", gs => { Narrator.Say("T'as refusé. Il a haussé les épaules.", Color.Grey); Narrator.Pause(); }),
                ], Color.Cyan1), s);
            },

            // Scène 4 : Vol à la tire — chasse au pickpocket
            s => {
                Narrator.Say("Ta main touche ta veste. Un doigt pas à toi s'en éloigne.", Color.Yellow);
                ChoiceMenu.Resolve(new Situation("Pickpocket repéré en train de s'enfuir.", [
                    new("Courir après", gs => {
                        if (Rng.Next(100) < 55) {
                            gs.Reputation += 10;
                            Narrator.Say("Tu le rattrapes. Foule qui applaudit presque. +10 rép.", Color.Green);
                            ChoiceMenu.Resolve(new Situation("Il est là. Qu'est-ce que t'en fais ?", [
                                new("Récupérer ton argent et le relâcher", ggs => { var r = Rng.Next(50, 200); ggs.Credits += r; ggs.Reputation += 5; Narrator.Say($"T'as récupéré {r}cr. Tu l'as laissé partir.", Color.Green); Narrator.Pause(); }),
                                new("Le remettre aux gardes", ggs => { ggs.Reputation -= 5; Narrator.Say("Les gardes l'emmènent. Il te regarde jusqu'au bout.", Color.Grey); Narrator.Pause(); }),
                                new("Le forcer à vider ses poches entières", ggs => { var cr = Rng.Next(200, 700); ggs.Credits += cr; ggs.Reputation -= 10; Narrator.Say($"Ses poches entières. +{cr}cr. La foule était moins contente.", Color.OrangeRed1); Narrator.Pause(); }),
                            ], Color.Yellow), gs);
                        } else {
                            var l = Rng.Next(100, 400); gs.Credits = Math.Max(0, gs.Credits - l);
                            Narrator.Say($"Il était plus rapide. -{l}cr. T'as couru pour rien.", Color.Red);
                            Narrator.Pause();
                        }
                    }),
                    new("Crier au voleur — alerte publique", gs => {
                        gs.Reputation += 5;
                        if (Rng.Next(2) == 0) Narrator.Say("Les gardes le rattrapent. +5 rép.", Color.Green);
                        else Narrator.Say("Il disparaît dans la foule. T'as l'air ridicule.", Color.Grey);
                        Narrator.Pause();
                    }),
                    new("Laisser tomber — pas la peine", gs => {
                        var l = Rng.Next(80, 300); gs.Credits = Math.Max(0, gs.Credits - l);
                        Narrator.Say($"Mauvaise décision. Il a pris {l}cr. Tu le regardes partir.", Color.Red);
                        Narrator.Pause();
                    }),
                ], Color.Yellow), s);
            },

            // Scène 5 : Recrutement d'un PNJ
            s => {
                ChoiceMenu.Resolve(new Situation("Une personne t'accoste. 'J'ai entendu que tu t'en sortais dans les coins sombres. J'ai quelque chose.'", [
                    new("Écouter la proposition", gs => {
                        var paye = Rng.Next(500, 2000);
                        Narrator.Say($"Une info à livrer à quelqu'un. {paye}cr. Elle a besoin de quelqu'un de discret.", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Tu acceptes ?", [
                            new("Oui", ggs => {
                                if (Rng.Next(100) < 65) { ggs.Credits += paye; ggs.Reputation += 5; Narrator.Say($"Mission faite. +{paye}cr.", Color.Green); }
                                else { ggs.Reputation -= 30; ggs.IsImprisoned = true; Narrator.Say("Piège. La livraison était une provocation pour les autorités.", Color.Red); }
                                Narrator.Pause();
                            }),
                            new("Non", ggs => { Narrator.Say("'Dommage.' Elle part.", Color.Grey); Narrator.Pause(); }),
                        ], Color.Yellow), gs);
                    }),
                    new("La snober", gs => { Narrator.Say("Elle hausse les épaules.", Color.Grey); Narrator.Pause(); }),
                ], Color.Cyan1), s);
            },

            // Scène 6 : Objet perdu par quelqu'un
            s => {
                ChoiceMenu.Resolve(new Situation("Un portefeuille par terre. Des papiers, des crédits, une photo.", [
                    new("Le garder", gs => {
                        var cr = Rng.Next(200, 600); gs.Credits += cr; gs.Reputation -= 10;
                        Narrator.Say($"+{cr}cr. -10 rép. L'image sur la photo t'a suivi un moment.", Color.OrangeRed1); Narrator.Pause();
                    }),
                    new("Chercher le propriétaire", gs => {
                        switch (Rng.Next(3)) {
                            case 0: gs.Reputation += 25; var cr = Rng.Next(200, 500); gs.Credits += cr; Narrator.Say($"C'était quelqu'un de bien. Il t'a bien récompensé. +{cr}cr, +25 rép.", Color.Green); break;
                            case 1: gs.Reputation += 10; Narrator.Say("T'as trouvé. Il t'a remercié d'une poignée de mains. +10 rép.", Color.Cyan1); break;
                            case 2: gs.Reputation -= 5; Narrator.Say("Le propriétaire t'a accusé d'avoir pris l'argent d'abord. T'avais rien fait.", Color.Red); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Signaler aux gardes et partir", gs => { gs.Reputation += 8; Narrator.Say("+8 rép. C'est tout.", Color.Green); Narrator.Pause(); }),
                ], Color.Grey), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ZONES INDUSTRIELLES (Forge Alpha, La Ferronnerie, Arc du Pic, Sanctum Machina)
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreIndustrial(GameState state)
    {
        Narrator.Say("Les entrailles mécaniques de la station grondent autour de toi.", Color.Grey);

        RollScene(state, [
            // Scène 1 : Technicien en détresse
            s => {
                ChoiceMenu.Resolve(new Situation("Un technicien coincé sous une machine — elle s'est décrochée et ça saigne.", [
                    new("Lever la machine et aider", gs => {
                        gs.ShipHp = Math.Min(gs.ShipMaxHp, gs.ShipHp + 20);
                        gs.Reputation += 20; var cr = Rng.Next(300, 800);
                        gs.Credits += cr;
                        Narrator.Say($"Il survit. Il répare ton vaisseau par gratitude. +20 PV vaisseau, +{cr}cr, +20 rép.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Voler ses outils pendant qu'il est coincé", gs => {
                        gs.Cargo.Add("Pièces techniques", 2); gs.Reputation -= 20;
                        Narrator.Say("T'as pris ses outils. Il pouvait pas faire grand chose. +2 Pièces techniques. -20 rép.", Color.Red);
                        Narrator.Pause();
                    }),
                    new("Appeler du secours et fouiller le secteur", gs => {
                        gs.Reputation += 8;
                        var cr = Rng.Next(100, 400); gs.Credits += cr;
                        gs.Cargo.Add("Pièces détachées", 1);
                        Narrator.Say($"T'as signalé ET fouillé le coin. +{cr}cr, +1 Pièces détachées, +8 rép.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                ], Color.SteelBlue1), s);
            },

            // Scène 2 : Accès à un terminal interdit
            s => {
                ChoiceMenu.Resolve(new Situation("Un terminal de maintenance laissé ouvert et sans surveillance.", [
                    new("Récupérer des données à revendre", gs => {
                        if (Rng.Next(2) == 0) { var cr = Rng.Next(500, 1500); gs.Credits += cr; Narrator.Say($"Données précieuses. +{cr}cr.", Color.Gold1); }
                        else { gs.Reputation -= 25; Narrator.Say("L'accès était tracé. Quelqu'un a noté ton passage. -25 rép.", Color.Red); }
                        Narrator.Pause();
                    }),
                    new("Désactiver les systèmes de sécurité — facilite l'exploration", gs => {
                        gs.ZoneDepth = Math.Max(0, gs.ZoneDepth - 1);
                        Narrator.Say("Les portes s'ouvrent. Les caméras se coupent. La profondeur effective diminue.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                    new("Couper l'alimentation d'une zone — chaos organisé", gs => {
                        switch (Rng.Next(3)) {
                            case 0: var cr = Rng.Next(400, 1000); gs.Credits += cr; Narrator.Say($"Dans le noir, t'as récupéré ce que tu cherchais. +{cr}cr.", Color.Gold1); break;
                            case 1: gs.PlayerHp = Math.Max(1, gs.PlayerHp - Rng.Next(15, 35)); Narrator.Say("T'as coupé quelque chose de critique. Le système de sécurité t'a envoyé une décharge. -PV.", Color.Red); break;
                            case 2: Narrator.Say("Tout le monde s'est retrouvé dans le noir. Confusion totale. Tu t'es éclipsé.", Color.Grey); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Fermer le terminal et s'éloigner", gs => { Narrator.Say("Sage. Ou peureux. Les deux fonctionnent pareil.", Color.Grey); Narrator.Pause(); }),
                ], Color.Yellow), s);
            },

            // Scène 3 : Pièces de valeur dans une benne
            s => {
                ChoiceMenu.Resolve(new Situation("Une benne remplie de pièces mécaniques. Un technicien surveille ça de loin.", [
                    new("Prendre discrètement ce qui a l'air de valoir qqchose", gs => {
                        if (Rng.Next(100) < 50) { gs.Cargo.Add("Pièces techniques", 2); gs.Cargo.Add("Pièces détachées", 1); Narrator.Say("+2 Pièces techniques, +1 Pièces détachées. Le technicien regardait ailleurs.", Color.Cyan1); }
                        else { gs.Reputation -= 15; Narrator.Say("Il t'a vu. Il appelle ses collègues. -15 rép. Tu files.", Color.Red); }
                        Narrator.Pause();
                    }),
                    new("Proposer de racheter les rebuts officiellement", gs => {
                        var cout = Rng.Next(80, 200); gs.Credits = Math.Max(0, gs.Credits - cout);
                        gs.Cargo.Add("Pièces techniques", 3); gs.Cargo.Add("Ferraille", 2);
                        Narrator.Say($"-{cout}cr. +3 Pièces techniques, +2 Ferraille. Propre.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Faire diversion et remplir tes poches", gs => {
                        var cr = Rng.Next(200, 600); gs.Credits += cr;
                        gs.Cargo.Add("Pièces détachées", 2); gs.Reputation -= 10;
                        Narrator.Say($"Diversion réussie. +{cr}cr de butin, +2 Pièces détachées. -10 rép.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                ], Color.Grey), s);
            },

            // Scène 4 : Accident — un ouvrier t'accuse
            s => {
                ChoiceMenu.Resolve(new Situation("Une explosion dans la salle d'à côté. Un ouvrier pointe son doigt vers toi. 'C'est lui !'", [
                    new("Nier fermement", gs => {
                        if (Rng.Next(100) < 50) { Narrator.Say("Les caméras le confirment. T'es blanchi. +5 rép.", Color.Green); gs.Reputation += 5; }
                        else { gs.Reputation -= 20; gs.Credits = Math.Max(0, gs.Credits - Rng.Next(300, 800)); Narrator.Say("Ils te croient pas. Tu paies les dégâts. -rép, -crédits.", Color.Red); }
                        Narrator.Pause();
                    }),
                    new("Payer pour fermer l'affaire vite", gs => {
                        var cout = Rng.Next(300, 900); gs.Credits = Math.Max(0, gs.Credits - cout);
                        Narrator.Say($"T'as payé. L'ouvrier s'est calmé. -{cout}cr.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("Attaquer l'ouvrier pour qu'il se taise", gs => {
                        gs.Reputation -= 40;
                        Narrator.Say("Mauvaise idée devant témoins. -40 rép.", Color.Red);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth + 1)));
                    }),
                ], Color.OrangeRed1), s);
            },

            // Scène 5 : Réparation vaisseau par un technicien véreux
            s => {
                ChoiceMenu.Resolve(new Situation("Un mécanicien propose de réparer ton vaisseau au noir. 'Moins cher. Moins de paperasse.'", [
                    new("Accepter — le prix est bas", gs => {
                        var cout = Rng.Next(100, 300); gs.Credits = Math.Max(0, gs.Credits - cout);
                        if (Rng.Next(100) < 60) {
                            gs.ShipHp = Math.Min(gs.ShipMaxHp, gs.ShipHp + 35);
                            Narrator.Say($"Il a bien bossé. -{cout}cr. +35 PV vaisseau.", Color.Green);
                        } else {
                            gs.ShipHp = Math.Max(1, gs.ShipHp - 20);
                            Narrator.Say($"Il a surtout empiré les choses. -{cout}cr. -20 PV vaisseau. 'Un problème en créant un autre.'", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Négocier le tarif à la baisse encore", gs => {
                        if (Rng.Next(2) == 0) {
                            var cout = Rng.Next(50, 150); gs.Credits = Math.Max(0, gs.Credits - cout);
                            gs.ShipHp = Math.Min(gs.ShipMaxHp, gs.ShipHp + 30);
                            Narrator.Say($"Il a accepté. -{cout}cr. +30 PV vaisseau.", Color.Gold1);
                        } else Narrator.Say("Il est parti vexé. T'as rien.", Color.Grey);
                        Narrator.Pause();
                    }),
                    new("Refuser", gs => { Narrator.Say("'Ton choix.' Il repart chercher un autre client.", Color.Grey); Narrator.Pause(); }),
                ], Color.Cyan1), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ZONES SCIENTIFIQUES (La Bulle, Les Abysses, L'Académie)
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreScientific(GameState state)
    {
        Narrator.Say("Les couloirs sentent les produits chimiques et quelque chose d'organique.", Color.Yellow);

        RollScene(state, [
            // Scène 1 : Expérience abandonnée
            s => {
                ChoiceMenu.Resolve(new Situation("Une expérience tourne en roue libre dans un labo vide. Des résultats sur les écrans. Des substances sur les tables.", [
                    new("Prendre les substances — valeur inconnue", gs => {
                        gs.Cargo.Add("Objets expérimentaux", 1);
                        if (Rng.Next(100) < 30) { gs.PlayerHp = Math.Max(1, gs.PlayerHp - Rng.Next(15, 35)); Narrator.Say("Contact cutané. Brûlure. -PV. +1 Objet expérimental.", Color.Red); }
                        else Narrator.Say("+1 Objet expérimental. La prudence était là, c'est tout.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                    new("Analyser les données sur les écrans", gs => {
                        switch (Rng.Next(3)) {
                            case 0: gs.Reputation += 20; Narrator.Say("Des données sur une nouvelle route commerciale. Utiles. +20 rép.", Color.Green); break;
                            case 1: var cr = Rng.Next(400, 1200); gs.Credits += cr; Narrator.Say($"Une formule que quelqu'un cherche. T'as noté. +{cr}cr à la revente.", Color.Gold1); break;
                            case 2: gs.PlayerHp = Math.Max(1, gs.PlayerHp - Rng.Next(10, 25)); Narrator.Say("Les données ont déclenché une alerte. Une décharge électrique sort de l'écran. Littéralement. -PV.", Color.Red); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Saboter l'expérience — quelque chose ne va pas", gs => {
                        if (Rng.Next(2) == 0) { gs.Reputation += 15; Narrator.Say("T'avais raison. L'expérience allait mal tourner. Des chercheurs te remercient. +15 rép.", Color.Green); }
                        else { gs.Reputation -= 20; gs.Credits = Math.Max(0, gs.Credits - Rng.Next(200, 600)); Narrator.Say("T'avais tort. L'expérience était valide. Ils sont pas contents. -rép, -crédits.", Color.Red); }
                        Narrator.Pause();
                    }),
                ], Color.Magenta1), s);
            },

            // Scène 2 : Chercheur fou
            s => {
                ChoiceMenu.Resolve(new Situation("Un scientifique t'accoste frénétiquement. 'Cobaye volontaire ? Payé. Effets secondaires : temporaires. Probablement.'", [
                    new("Accepter le test", gs => {
                        var paye = Rng.Next(300, 900);
                        gs.Credits += paye;
                        switch (Rng.Next(4)) {
                            case 0: gs.PlayerMaxHp += 8; Narrator.Say($"T'es sorti plus grand. +{paye}cr et +8 PV max permanent.", Color.Gold1); break;
                            case 1: gs.PlayerHp = Math.Max(1, gs.PlayerHp - Rng.Next(25, 55)); Narrator.Say($"+{paye}cr. -{Rng.Next(25, 55)} PV. 'Temporaire', il avait dit.", Color.Red); break;
                            case 2: gs.Stamina = gs.MaxStamina; gs.PlayerHp = Math.Min(gs.PlayerMaxHp, gs.PlayerHp + 30); Narrator.Say($"+{paye}cr. Tu te sens incroyablement bien. Ça durera probablement pas.", Color.Green); break;
                            case 3: gs.AddictionLevel++; Narrator.Say($"+{paye}cr. Le produit est... agréable. Trop agréable. Addiction +1.", Color.OrangeRed1); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Refuser mais écouter de quoi il s'agit", gs => {
                        var cr = Rng.Next(100, 400); gs.Credits += cr;
                        Narrator.Say($"T'as écouté sans te laisser piquer. L'info de son projet valait +{cr}cr à la revente discrète.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                    new("Lui voler ses notes et partir", gs => {
                        gs.Cargo.Add("Artefacts", 1); gs.Reputation -= 15;
                        Narrator.Say("T'as pris ses notes. Il aura une mauvaise journée. +1 Artefact. -15 rép.", Color.OrangeRed1);
                        if (Rng.Next(100) < 40) { Narrator.Say("Il appelle la sécurité.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth))); }
                        else Narrator.Pause();
                    }),
                ], Color.Yellow), s);
            },

            // Scène 3 : Créature échappée
            s => {
                Narrator.Say("Une alarme silencieuse clignote. Une cage vide dans la salle d'à côté. Quelque chose s'est échappé.", Color.Red);
                ChoiceMenu.Resolve(new Situation("Tu l'entends se déplacer dans le couloir.", [
                    new("Tendre une embuscade et l'attaquer", gs => {
                        var e = new Enemy("Créature de laboratoire", 45, 10, 22, 200, 600, "Elle a été modifiée pour quelque chose. On voit pas quoi.", KillChance: 10);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                    }),
                    new("La piéger avec de la nourriture (si tu en as)", gs => {
                        if (gs.Cargo.Get("Rations") > 0 || gs.Cargo.Get("Vivres") > 0) {
                            gs.Cargo.Remove("Rations", Math.Min(1, gs.Cargo.Get("Rations")));
                            gs.Reputation += 20; var cr = Rng.Next(400, 1000); gs.Credits += cr;
                            Narrator.Say($"Ça a marché. Les chercheurs sont soulagés. +{cr}cr, +20 rép.", Color.Green);
                        } else {
                            Narrator.Say("T'as rien pour la piéger. Elle t'attaque.", Color.Red);
                            var e = new Enemy("Créature de laboratoire", 45, 10, 22, 200, 600, "Affamée et effrayée.");
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                        }
                        Narrator.Pause();
                    }),
                    new("S'enfuir et signaler — pas ton problème", gs => {
                        gs.Reputation += 5;
                        if (Rng.Next(100) < 35) { gs.PlayerHp = Math.Max(1, gs.PlayerHp - Rng.Next(8, 20)); Narrator.Say("Elle t'a rattrapé sur quelques mètres. Juste quelques griffures. -PV.", Color.Yellow); }
                        else Narrator.Say("T'es sorti avant qu'elle te repère. +5 rép pour avoir signalé.", Color.Grey);
                        Narrator.Pause();
                    }),
                ], Color.OrangeRed1), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // RUINES ET ÉPAVES
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreRuins(GameState state)
    {
        var intros = new[]
        {
            "Le silence ici n'est pas naturel. Il est délibéré.",
            "L'histoire de cet endroit a laissé des cicatrices dans les murs.",
            "Tu t'aventures là où les gens prudents n'entrent pas.",
        };
        Narrator.Say(intros[Rng.Next(intros.Length)], Color.Grey);

        RollScene(state, [
            // Scène 1 : Chambre forte ancienne
            s => {
                ChoiceMenu.Resolve(new Situation("Une chambre forte d'avant-guerre. La serrure est usée. Elle peut céder.", [
                    new("Forcer la serrure maintenant", gs => {
                        switch (Rng.Next(4)) {
                            case 0: var cr = Rng.Next(800, 2500); gs.Credits += cr; Narrator.Say($"Elle s'ouvre. +{cr}cr en billets d'une époque révolue.", Color.Gold1); break;
                            case 1: gs.Cargo.Add("Artefacts", 2); Narrator.Say("+2 Artefacts. Des choses qui n'auraient pas dû survivre.", Color.Gold1); break;
                            case 2: var hp = Rng.Next(25, 55); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp); Narrator.Say($"Piège à l'intérieur. -{hp} PV.", Color.Red); break;
                            case 3: Narrator.Say("Vide. Propre. Volée depuis longtemps.", Color.Grey); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Chercher d'abord si quelqu'un la surveille", gs => {
                        if (Rng.Next(100) < 45) {
                            Narrator.Say("Oui. Un squatter. Il t'a vu chercher.", Color.Red);
                            var e = new Enemy("Gardien des ruines", 50, 8, 18, 300, 800, "Il est là depuis longtemps. Il a fait de cet endroit sa forteresse.");
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                        } else {
                            var cr = Rng.Next(600, 1800); gs.Credits += cr;
                            Narrator.Say($"Personne. Tu prends ton temps. +{cr}cr.", Color.Gold1);
                            Narrator.Pause();
                        }
                    }),
                    new("Laisser — cette chambre forte est là depuis des raisons", gs => { Narrator.Say("T'as laissé. Certaines choses sont mieux où elles sont.", Color.Grey); Narrator.Pause(); }),
                ], Color.Gold1), s);
            },

            // Scène 2 : Archives de guerre
            s => {
                ChoiceMenu.Resolve(new Situation("Des archives intactes d'avant la grande guerre. Données, journaux, plans.", [
                    new("Lire — comprendre ce qui s'est passé ici", gs => {
                        gs.Reputation += 20;
                        Narrator.Say("Ce que t'as lu change ta façon de voir cet endroit. Certaines vérités sont difficiles. +20 rép.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                    new("Copier les données pour les vendre", gs => {
                        var cr = Rng.Next(600, 2000); gs.Credits += cr; gs.Reputation -= 10;
                        Narrator.Say($"Des gens paient très bien pour effacer l'histoire. +{cr}cr. -10 rép.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new("Brûler les archives — certaines choses doivent disparaître", gs => {
                        if (Rng.Next(2) == 0) { gs.Reputation += 15; Narrator.Say("T'as peut-être fait le bon choix. Des gens qui surveilaient ça te remercient discrètement. +15 rép.", Color.Green); }
                        else { gs.Reputation -= 25; Narrator.Say("T'as détruit quelque chose d'irremplaçable. Les historiens t'haïront. -25 rép.", Color.Red); }
                        Narrator.Pause();
                    }),
                ], Color.SteelBlue1), s);
            },

            // Scène 3 : Combat dans les ruines — squatter
            s => {
                Narrator.Say("Tu entends des bruits de pas. Quelqu'un d'autre est là.", Color.Grey);
                ChoiceMenu.Resolve(new Situation("Les bruits se rapprochent.", [
                    new("Se cacher et observer", gs => {
                        if (Rng.Next(100) < 55) {
                            Narrator.Say("C'est un scavenger. Il ne t'a pas vu. Tu le regardes partir avec un butin.", Color.Grey);
                            if (Rng.Next(2) == 0) { var cr = Rng.Next(300, 800); gs.Credits += cr; Narrator.Say($"Il a laissé quelque chose tomber. +{cr}cr.", Color.Gold1); }
                        } else {
                            Narrator.Say("Il t'a repéré de loin. Il approche armé.", Color.Red);
                            var e = new Enemy("Squatter des ruines", 40, 7, 16, 200, 600, "Il protège ce territoire depuis des années.");
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                        }
                        Narrator.Pause();
                    }),
                    new("Appeler — signaler ta présence", gs => {
                        if (Rng.Next(100) < 40) {
                            gs.Reputation += 10; var cr = Rng.Next(300, 900); gs.Credits += cr;
                            Narrator.Say($"C'est quelqu'un de la station. Perdu. Reconnaissant. +{cr}cr, +10 rép.", Color.Green);
                        } else {
                            Narrator.Say("Mauvaise idée.", Color.Red);
                            var e = new Enemy("Squatter des ruines", 40, 7, 16, 200, 600, "Il protège ce territoire depuis des années.");
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                        }
                        Narrator.Pause();
                    }),
                    new("Attaquer en premier — l'avantage de la surprise", gs => {
                        var e = new Enemy("Squatter des ruines", 40, 7, 16, 200, 600, "Il ne t'a pas vu venir.");
                        var outcome = Combat.Start(gs, e);
                        if (outcome == CombatOutcome.Victory) { Display.ShowEvent("Tu récupères son butin.", Color.Gold1); }
                        Situations.ApplyCombatOutcome(gs, outcome);
                    }),
                ], Color.OrangeRed1), s);
            },

            // Scène 4 : Trappe cachée
            s => {
                Narrator.Say("Une plaque de métal sonne creux sous tes pieds. Il y a quelque chose en dessous.", Color.Yellow);
                ChoiceMenu.Resolve(new Situation("La trappe ?", [
                    new("Ouvrir et descendre", gs => {
                        switch (Rng.Next(4)) {
                            case 0: var cr = Rng.Next(500, 2000); gs.Credits += cr; gs.Cargo.Add("Artefacts", 1); Narrator.Say($"Un bunker d'urgence. Intouché. +{cr}cr, +1 Artefact.", Color.Gold1); break;
                            case 1: var hp = Rng.Next(20, 50); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp); Narrator.Say($"La trappe était un piège. -{hp} PV.", Color.Red); break;
                            case 2:
                                var e = new Enemy("Chose des profondeurs", 60, 12, 25, 400, 1000, "Elle a vécu dans le noir assez longtemps pour s'adapter.", KillChance: 20);
                                Narrator.Say("Quelque chose vit là-dedans depuis longtemps.", Color.Red);
                                Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                                return;
                            case 3: var cr2 = Rng.Next(200, 700); gs.Credits += cr2; Narrator.Say($"Un espace vide avec juste assez de butin pour valoir le détour. +{cr2}cr.", Color.Cyan1); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Taper pour tester avant d'ouvrir", gs => {
                        if (Rng.Next(100) < 40) { var cr = Rng.Next(400, 1200); gs.Credits += cr; Narrator.Say($"Précaution payante. +{cr}cr récupérés en toute sécurité.", Color.Green); }
                        else Narrator.Say("Rien d'alarmant. Vide.", Color.Grey);
                        Narrator.Pause();
                    }),
                    new("Laisser — trop de risques", gs => { Narrator.Say("Sage.", Color.Grey); Narrator.Pause(); }),
                ], Color.Yellow), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ESMERALDA — Nature
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreNature(GameState state)
    {
        Narrator.Say("La végétation dense d'Esmeralda te regarde entrer.", Color.Green);

        RollScene(state, [
            // Scène 1 : Animaux
            s => {
                ChoiceMenu.Resolve(new Situation("Un animal territorial de la taille d'un cheval te fixe. Oreilles droites. Muscles tendus.", [
                    new("Rester immobile — ne pas faire de mouvements brusques", gs => {
                        if (Rng.Next(100) < 60) { gs.Reputation += 10; Narrator.Say("Il finit par bouger. Tu passes. Le roi Maxance apprend que tu as respecté ses créatures. +10 rép.", Color.Green); }
                        else { var hp = Rng.Next(15, 35); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp); Narrator.Say($"Il charge quand même. -{hp} PV.", Color.Red); }
                        Narrator.Pause();
                    }),
                    new("Reculer doucement", gs => {
                        Narrator.Say("Tu recules. Il avance. T'accélères. Il accélère.", Color.Yellow);
                        var e = new Enemy("Fauve territorial", 55, 12, 24, 100, 400, "Il défend son territoire. Point.", KillChance: 15);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                    }),
                    new("Lui lancer de la nourriture", gs => {
                        if (gs.Cargo.Get("Vivres") > 0 || gs.Cargo.Get("Rations") > 0) {
                            gs.Cargo.Remove("Vivres", Math.Min(1, gs.Cargo.Get("Vivres")));
                            gs.Reputation += 15;
                            Narrator.Say("Il mange. Il s'éloigne. Maxance aimerait cette scène. +15 rép.", Color.Green);
                        } else {
                            Narrator.Say("T'as rien à lui donner. Il attaque.", Color.Red);
                            var e = new Enemy("Fauve territorial", 55, 12, 24, 100, 400, "Affamé ET territorial.");
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                        }
                        Narrator.Pause();
                    }),
                ], Color.Green), s);
            },

            // Scène 2 : Plantes rares
            s => {
                ChoiceMenu.Resolve(new Situation("Une clairière de plantes rares. Certaines sont protégées par décret du roi Maxance.", [
                    new("Récolter — tu en as besoin", gs => {
                        gs.Cargo.Add("Plantes médicinales", 3); gs.Reputation -= 20;
                        Narrator.Say("Tu prends. Quelqu'un t'observait depuis les arbres. -20 rép. +3 Plantes médicinales.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new("Documenter et repartir — les laisser en paix", gs => {
                        gs.Reputation += 15;
                        Narrator.Say("Tu les observes, tu t'en vas. Maxance a des espions partout. +15 rép.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Prendre juste une petite quantité discrètement", gs => {
                        gs.Cargo.Add("Plantes médicinales", 1); gs.Reputation -= 5;
                        if (Rng.Next(100) < 30) { gs.Reputation -= 15; Narrator.Say("Vu. -5 rép supplémentaires.", Color.Red); }
                        else Narrator.Say("+1 Plante médicinale. Personne a rien dit.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                ], Color.Green), s);
            },

            // Scène 3 : Animal blessé
            s => {
                ChoiceMenu.Resolve(new Situation("Un animal plus petit qu'un chat, griffes impressionnantes, coincé dans un piège.", [
                    new("Le libérer du piège", gs => {
                        if (gs.Cargo.Get("Médicaments") > 0) {
                            gs.Cargo.Remove("Médicaments", 1);
                            gs.Reputation += 30;
                            Narrator.Say("Tu le libères et tu le soignes. Maxance l'apprend. +30 rép.", Color.Gold1);
                        } else {
                            gs.Reputation += 15;
                            var hp = Rng.Next(5, 15); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp);
                            Narrator.Say($"Tu le libères. Il te griffe par réflexe. -{hp} PV. Mais tu as bien fait. +15 rép.", Color.Cyan1);
                        }
                        Narrator.Pause();
                    }),
                    new("Laisser le piège — c'est celui de quelqu'un", gs => {
                        gs.Reputation -= 10;
                        Narrator.Say("Tu pars. Il te regarde. -10 rép.", Color.Red);
                        Narrator.Pause();
                    }),
                    new("Prendre l'animal et le garder", gs => {
                        gs.Reputation -= 5;
                        Narrator.Say("Tu le mets dans ta veste. Il t'a mordu dès que vous avez atterri dans le vaisseau. -5 rép, +1 morsure.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                ], Color.Green), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ZONES DE LUXE (Scotty, Star Quest, La Couronne)
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreLuxury(GameState state)
    {
        Narrator.Say("Les dorures te font loucher. Les sourires sont encore plus faux.", Color.Gold1);

        RollScene(state, [
            // Scène 1 : Pari spontané
            s => {
                var mise = Rng.Next(200, 800);
                ChoiceMenu.Resolve(new Situation($"Un riche ennuyé te lance un défi stupide. Mise : {mise}cr chacun. Tu sembles hors de ta ligue.", [
                    new("Accepter le défi", gs => {
                        if (gs.Credits >= mise) {
                            gs.Credits -= mise;
                            if (Rng.Next(100) < 50) { gs.Credits += mise * 2; gs.Reputation += 15; Narrator.Say($"T'as gagné. +{mise}cr, +15 rép.", Color.Gold1); }
                            else { Narrator.Say($"T'as perdu. -{mise}cr.", Color.Red); }
                        } else Narrator.Say("Pas assez de crédits pour miser.", Color.Grey);
                        Narrator.Pause();
                    }),
                    new("Refuser mais les narguer", gs => {
                        gs.Reputation += 5;
                        Narrator.Say("Tu les ignores avec classe. Ça les énerve plus que perdre. +5 rép.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                    new("Tricher discrètement", gs => {
                        if (Rng.Next(100) < 60) { gs.Credits += (int)(mise * 1.5); Narrator.Say($"Trop facile. +{(int)(mise*1.5)}cr.", Color.Gold1); }
                        else { gs.Reputation -= 30; gs.Credits = Math.Max(0, gs.Credits - mise); Narrator.Say("Pris. Scène publique. -30 rép.", Color.Red); }
                        Narrator.Pause();
                    }),
                ], Color.Gold1), s);
            },

            // Scène 2 : Influent intéressé par toi
            s => {
                ChoiceMenu.Resolve(new Situation("Une personne manifestement puissante t'observe depuis un moment. Elle t'approche.", [
                    new("Engager la conversation", gs => {
                        switch (Rng.Next(3)) {
                            case 0: gs.Reputation += 30; var cr = Rng.Next(1000, 3000); gs.Credits += cr; Narrator.Say($"Deal. +{cr}cr, +30 rép.", Color.Gold1); break;
                            case 1: gs.IsDoubleAgent = true; gs.Reputation += 10; Narrator.Say("Elle te recrute pour quelque chose de délicat. Tu joues le double jeu maintenant.", Color.Magenta1); break;
                            case 2: gs.Reputation += 15; Narrator.Say("Conversation intéressante. Rien de concret mais tu as un nom dans sa tête. +15 rép.", Color.Cyan1); break;
                        }
                        Narrator.Pause();
                    }),
                    new("L'éviter — trop d'attention c'est dangereux", gs => {
                        Narrator.Say("Tu t'esquives. Elle note que t'as esquivé. C'est peut-être pire.", Color.Grey);
                        Narrator.Pause();
                    }),
                    new("La provoquer subtilement pour voir sa réaction", gs => {
                        if (Rng.Next(2) == 0) { gs.Reputation += 20; Narrator.Say("Elle apprécie le culot. +20 rép.", Color.Green); }
                        else { gs.Reputation -= 20; Narrator.Say("Mauvais calcul. -20 rép.", Color.Red); }
                        Narrator.Pause();
                    }),
                ], Color.Gold1), s);
            },

            // Scène 3 : Pickpocket de luxe
            s => {
                ChoiceMenu.Resolve(new Situation("Tu repères un pickpocket de haut niveau en train d'opérer sur une cible riche. Il t'a repéré aussi.", [
                    new("Dénoncer le pickpocket", gs => {
                        gs.Reputation += 20; var cr = Rng.Next(300, 800); gs.Credits += cr;
                        Narrator.Say($"La cible te remercie généreusement. +{cr}cr, +20 rép.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Distraire la cible pour faciliter le vol — deal tacite", gs => {
                        var cr = Rng.Next(200, 600); gs.Credits += cr; gs.Reputation -= 10;
                        Narrator.Say($"Le pickpocket partage. +{cr}cr, -10 rép.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new("Voler le pickpocket pendant qu'il s'occupe de la cible", gs => {
                        if (Rng.Next(100) < 50) { var cr = Rng.Next(300, 900); gs.Credits += cr; Narrator.Say($"Ironie de la situation. +{cr}cr.", Color.Gold1); }
                        else { gs.Reputation -= 15; Narrator.Say("Il t'a vu venir. Il a des amis. -15 rép.", Color.Red); }
                        Narrator.Pause();
                    }),
                    new("Ignorer les deux", gs => { Narrator.Say("Tu passes. Ce n'était pas ton affaire.", Color.Grey); Narrator.Pause(); }),
                ], Color.Gold1), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ZONES MILITAIRES (Emporium Requiem, La Citadelle, Fort Ossian...)
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreMilitary(GameState state)
    {
        Narrator.Say("Les zones restreintes n'ont pas ce nom pour rien. T'y es quand même.", Color.Red);

        RollScene(state, [
            // Scène 1 : Patrouille
            s => {
                ChoiceMenu.Resolve(new Situation("Une patrouille de 3 gardes approche. Zone interdite. Ils t'ont vu.", [
                    new("Prétendre être perdu — jouer l'innocent", gs => {
                        var chance = 35 + (gs.Reputation > 100 ? 20 : 0) + (gs.Class.Name == "Hackeur" ? 20 : 0);
                        if (Rng.Next(100) < chance) { Narrator.Say("Ils gobent. Ils t'escortent hors de la zone. Humiliant mais libre.", Color.Yellow); }
                        else { gs.IsImprisoned = true; Narrator.Say("Ils vérifient. L'histoire ne tient pas. Cellule.", Color.Red); }
                        Narrator.Pause();
                    }),
                    new("Montrer patte blanche — réputation ou badge", gs => {
                        if (gs.Reputation >= 200) { Narrator.Say("Ton nom leur dit quelque chose. Ils te laissent passer.", Color.Green); }
                        else { gs.Credits = Math.Max(0, gs.Credits - Rng.Next(300, 800)); Narrator.Say("Pas assez de réputation. Tu paies le pot-de-vin. -crédits.", Color.Yellow); }
                        Narrator.Pause();
                    }),
                    new("Foncer — traverser la patrouille en force", gs => {
                        var g = Combat.Scale(new Enemy("Garde militaire", 55, 10, 22, 200, 600, "Ils sont trois. Entraînés."), gs.ZoneDepth + 1);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, g));
                    }),
                    new("Se cacher et attendre qu'ils passent", gs => {
                        if (Rng.Next(100) < 50) { Narrator.Say("Ils passent. T'es libre.", Color.Grey); }
                        else { gs.Reputation -= 25; Narrator.Say("Ils t'ont quand même trouvé. -25 rép.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth))); return; }
                        Narrator.Pause();
                    }),
                ], Color.Red), s);
            },

            // Scène 2 : Armurerie
            s => {
                ChoiceMenu.Resolve(new Situation("Une armurerie militaire. Un technicien est absent. Caméras visibles — mais quelqu'un les regarde ?", [
                    new("Voler discrètement ce qui est accessible", gs => {
                        gs.Cargo.Add("Armes", 1); gs.Cargo.Add("Explosifs", 1);
                        gs.Reputation -= 30;
                        if (Rng.Next(100) < 35) { gs.IsImprisoned = true; Narrator.Say("+1 Arme, +1 Explosif. Caméras regardées. Cellule.", Color.Red); }
                        else Narrator.Say("+1 Arme, +1 Explosif. Caméras déconnectées pour maintenance. T'as eu de la chance.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new("Signaler une faille de sécurité", gs => {
                        gs.Reputation += 25;
                        Narrator.Say("C'est honnête. Ou malin. Les deux. +25 rép.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Attendre et surveiller qui vient", gs => {
                        if (Rng.Next(100) < 40) {
                            var cr = Rng.Next(400, 1000); gs.Credits += cr;
                            Narrator.Say($"Un militaire est venu prendre quelque chose d'illégal. T'as filmé. +{cr}cr de chantage discret.", Color.Gold1);
                        } else {
                            Narrator.Say("Personne de compromettant. T'as perdu du temps.", Color.Grey);
                        }
                        Narrator.Pause();
                    }),
                ], Color.Red), s);
            },

            // Scène 3 : Prisonnier politique
            s => {
                ChoiceMenu.Resolve(new Situation("Une cellule improvisée. Un détenu qui chuchote ton nom — il te connaît.", [
                    new("L'écouter — qui est-il ?", gs => {
                        var cr = Rng.Next(500, 1500); gs.Credits += cr; gs.Reputation += 10;
                        Narrator.Say($"Il a des informations précieuses et te paye pour les garder secrètes. +{cr}cr, +10 rép.", Color.Cyan1);
                        Narrator.Pause();
                    }),
                    new("Tenter de le libérer", gs => {
                        switch (Rng.Next(3)) {
                            case 0: gs.Reputation += 40; var cr = Rng.Next(1000, 2500); gs.Credits += cr; Narrator.Say($"Réussi. Il était important. +{cr}cr, +40 rép.", Color.Gold1); break;
                            case 1: gs.Reputation -= 20; gs.IsImprisoned = true; Narrator.Say("Pris en flagrant délit. -20 rép. Cellule.", Color.Red); break;
                            case 2: gs.Reputation += 10; Narrator.Say("Réussi mais il ne se souviendra pas de toi. C'est peut-être mieux. +10 rép.", Color.Green); break;
                        }
                        Narrator.Pause();
                    }),
                    new("Le livrer aux gardes — il peut rapporter quelque chose", gs => {
                        var cr = Rng.Next(200, 600); gs.Credits += cr; gs.Reputation -= 30;
                        Narrator.Say($"+{cr}cr de prime. -30 rép. Il savait ton nom et t'as quand même fait ça.", Color.Red);
                        Narrator.Pause();
                    }),
                    new("Passer sans s'arrêter", gs => { Narrator.Say("Il répète ton nom jusqu'à ce que tu t'éloignes.", Color.Grey); Narrator.Pause(); }),
                ], Color.SteelBlue1), s);
            },
        ]);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // GÉNÉRIQUE (toutes les autres stations)
    // ─────────────────────────────────────────────────────────────────────────────

    static void ExploreGeneric(GameState state)
    {
        Narrator.Say("Tu t'aventures dans les recoins moins fréquentés. Moins fréquentés pour une raison.", Color.Grey);

        RollScene(state, [
            s => ExploreDangerous(s),    // les zones génériques peuvent basculer vers n'importe quoi
            s => ExplorePeaceful(s),
            s => ExploreIndustrial(s),
            s => ExploreRuins(s),
            s => {
                // Rencontre PNJ neutre rapide avec un vrai choix
                ChoiceMenu.Resolve(new Situation("Un passant s'arrête. Il a quelque chose à offrir ou à demander.", [
                    new("L'écouter", gs => {
                        switch (Rng.Next(4)) {
                            case 0: var cr = Rng.Next(200, 700); gs.Credits += cr; gs.Reputation += 5; Narrator.Say($"Une info monnayable. +{cr}cr, +5 rép.", Color.Cyan1); break;
                            case 1: gs.Cargo.Add("Médicaments", 1); Narrator.Say("Il vend des médicaments au prix de la rue. +1 Médicaments.", Color.Green); break;
                            case 2: var l = Rng.Next(100, 400); gs.Credits = Math.Max(0, gs.Credits - l); Narrator.Say($"Arnaque. -{l}cr. T'aurais dû passer.", Color.Red); break;
                            case 3: var e = Combat.GetScaled(gs, gs.ZoneDepth); Narrator.Say("C'était un appât.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e)); return;
                        }
                        Narrator.Pause();
                    }),
                    new("Ignorer et continuer", gs => { Narrator.Say("Il repart chercher quelqu'un d'autre.", Color.Grey); Narrator.Pause(); }),
                ], Color.Grey), s);
            },
        ]);
    }
}
