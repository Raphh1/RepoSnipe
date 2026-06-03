using Spectre.Console;

namespace VoidTrader;

static class Situations
{
    private static readonly Random Rng = new();

    // ── ARRIVÉE EN STATION ──────────────────────────────────────────────────

    static void ShowArrivalNpcReaction(GameState state)
    {
        if (state.Reputation == 0) return; // inconnu — pas de réaction particulière

        var msg = state.Reputation switch
        {
            >= 1000 => "[gold1]Des passants s'arrêtent. Des murmures. On te reconnaît. Une légende vivante qui marche parmi eux.[/]",
            >= 500  => "[green]Les gens s'écartent légèrement à ton passage. Certains hochent la tête. Tu es connu ici.[/]",
            >= 100  => "[cyan1]Un garde te salue. 'Bienvenue.' Ton nom circule dans cette station.[/]",
            <= -1000 => "[red bold]Un signal d'alerte discret passe entre les gardes en te voyant entrer. Ennemi public. Ils gardent leurs distances — pour l'instant.[/]",
            <= -500 => "[red]Des regards froids te suivent. Ta réputation t'a précédé. Personne ne te sourira ici.[/]",
            <= -100 => "[orange1]Un garde te dévisage plus longuement que nécessaire. Il note quelque chose sur son terminal.[/]",
            _       => null,
        };

        if (msg != null)
        {
            AnsiConsole.MarkupLine($"  {msg}");
            AnsiConsole.WriteLine();
        }

        // Réactions spécifiques si on est cannibale et connu
        if (state.IsCannibalistic && state.NpcsMet.Count > 5 && Rng.Next(100) < 20)
        {
            Narrator.Say("Quelqu'un te reconnaît et s'éloigne très vite. Un mot circule. Un mot que tu préfères ne pas entendre.", Color.DarkRed);
        }
    }

    static void ShowStationChanges(GameState state)
    {
        var ss       = state.GetStationState(state.CurrentStation);
        var daysSince = ss.LastVisitDay > 0 ? state.Day - ss.LastVisitDay : -1;

        if (daysSince > 3 && (ss.EventsHappened?.Count > 0 || ss.ControlledBy != null))
        {
            AnsiConsole.WriteLine();
            if (ss.ControlledBy != null)
            {
                var fName = Factions.Info.TryGetValue(ss.ControlledBy.Value, out var fi) ? fi.Name : ss.ControlledBy.ToString();
                AnsiConsole.MarkupLine($"  [magenta1]La station a changé de mains. Elle est maintenant sous contrôle de {fName}.[/]");
            }
            if (ss.EventsHappened?.Contains("Boss_Defeated") == true && daysSince > 0)
                AnsiConsole.MarkupLine($"  [grey]La station porte encore les marques de la bataille — l'ambiance est différente depuis que son chef est tombé.[/]");
            AnsiConsole.WriteLine();
        }

        // Met à jour le jour de visite
        state.UpdateStationState(state.CurrentStation,
            state.GetStationState(state.CurrentStation) with { LastVisitDay = state.Day });
    }

    public static Situation Arrival(GameState state)
    {
        var station = Universe.Get(state.CurrentStation);
        ShowArrivalNpcReaction(state);
        ShowStationChanges(state);
        // Déclencheurs d'arcs narratifs à chaque arrivée
        NarrativeArcs.CheckTriggers(state);
        // Quêtes complétées à l'arrivée
        QuestSystem.CheckOnArrival(state);
        Objectives.CheckAll(state);
        Narrator.Say($"Tu es à {state.CurrentStation}. Qu'est-ce que tu fais ?");

        var choices = new List<Choice>
        {
            new("Aller au marché",
                s => { Narrator.Say("Tu te diriges vers les stands du marché..."); Menus.OpenMarket(s); },
                Category: "Commerce"),

            new("Ravitailler en carburant",
                s => { Narrator.Say("Direction la station-service..."); Menus.DoRefuel(s); },
                s => s.Fuel < s.MaxFuel,
                Category: "Commerce"),

            new("Chercher du carburant — réservoir vide",
                s => ResolveScroungeFuel(s),
                s => s.Fuel == 0,
                "Survie"),

            new("Traîner dans le coin — voir ce qui se passe",
                s => { ResolveExplore(s); Clock.SpendAction(s); },
                Category: "Exploration"),

            new("Chercher un dealer",
                s => ResolveDealerSearch(s),
                s => Universe.Danger(Universe.Get(s.CurrentStation)) >= 3,
                "Exploration"),

            new("Chercher du travail ou une opportunité",
                s => ResolveJobSearch(s),
                Category: "Exploration"),

            new("Explorer la zone",
                s => { Narrator.Say("Tu pars à la découverte..."); Exploration.Explore(s); Clock.SpendAction(s); },
                Category: "Exploration"),

            new("Consommer des provisions",
                s => Consumables.OpenInventoryMenu(s),
                s => s.Cargo.All.Any(kv => Consumables.IsConsumable(kv.Key)),
                Category: "Survie"),

            new("Atelier vaisseau — réparer / améliorer",
                s => ShipWorkshop.Open(s),
                Category: "Vaisseau"),

            new("Repartir",
                s => Narrator.Say("Tu restes dans ton vaisseau et tu attends de repartir.")),
        };

        // Choix spécifiques aux stations
        AddStationSpecificChoices(choices, state);

        return new Situation($"Que fais-tu à {state.CurrentStation} ?", choices, Color.SteelBlue1);
    }

    static void AddStationSpecificChoices(List<Choice> choices, GameState state)
    {
        const string cat = "Interactions";
        switch (state.CurrentStation)
        {
            case "Scotty Golden North":
                choices.Add(new("Tenter sa chance au casino",                      s => ResolveCasino(s),      Category: cat)); break;

            case "Star Quest":
                choices.Add(new("Faire la fête avec Mister Eliotis",               s => ResolveParty(s),       Category: cat)); break;

            case "Arc Ouest Apocalypse":
                choices.Add(new("Chercher Alanossa",                               s => ResolveAlanossa(s),    Category: cat));
                choices.Add(new("Vendre au marché noir",                           s => ResolveBlackMarket(s), s => s.Cargo.All.Any(), cat)); break;

            case "L'Arc Perdu":
                choices.Add(new("Explorer les ruines silencieuses",                s => ResolveGhostShip(s),   Category: cat));
                choices.Add(new("Chercher Raphazarus",                             s => ResolveRaphazarus(s),  Category: cat)); break;

            case "L'Arc du Pic de l'Est":
                choices.Add(new("Demander une audience à Ramaster",                s => ResolveRamaster(s),    Category: cat)); break;

            case "Emporium Requiem":
                choices.Add(new("Négocier avec la douane",                         s => ResolveCustoms(s),     s => s.Reputation < 0, cat)); break;

            case "Le Sanctuaire des Dérives":
                choices.Add(new("Se faire soigner",                                s => ResolveSanctuary(s),   s => s.PlayerHp < s.PlayerMaxHp || s.ShipHp < s.ShipMaxHp, cat));
                choices.Add(new("Offrir des Médicaments à Sœur Valkara",
                    s => { s.Cargo.Remove("Médicaments", 1); s.Reputation += 40; Narrator.Say("Sœur Valkara te remercie chaleureusement. +40 réputation.", Color.Green); Narrator.Pause(); },
                    s => s.Cargo.Get("Médicaments") > 0, cat)); break;

            case "La Bulle":
                choices.Add(new("Consulter le Docteur Flinch pour une 'amélioration'", s => ResolveFlinch(s),  Category: cat)); break;

            case "Avant-Poste Kalem":
                choices.Add(new("Parler au Commandant Voss — missions de combat",  s => ResolveVoss(s),        Category: cat)); break;

            case "Port des Brumes":
                choices.Add(new("Acheter un faux certificat d'identité",           s => ResolveFakeId(s),      s => s.Reputation < -100 && s.Credits >= 800, cat)); break;

            case "Les Décombres de Vael":
                choices.Add(new("Parler à L'Ancien",                               s => ResolveAncien(s),      Category: cat)); break;

            case "Station Rocaille":
                choices.Add(new("Parler à Petite Mara",                            s => ResolveMara(s),        Category: cat)); break;

            case "Colonie Perséphone":
                choices.Add(new("Aider la colonie",                                s => ResolveColony(s),      Category: cat)); break;

            case "Le Purgatoire":
                choices.Add(new("Parler à Baruk",                                  s => ResolveBaruk(s),       Category: cat)); break;

            case "Nexus Aldara":
                choices.Add(new("Rencontrer la Directrice Aldara",                 s => ResolveAldara(s),      s => s.Reputation >= 100, cat)); break;

            case "Le Vaisseau Fantôme Errant":
                choices.Add(new("Explorer le vaisseau",                            s => ResolveGhostShip(s),   Category: cat)); break;

            case "La Station du Jugement":
                choices.Add(new("Rencontrer le Juge",                              s => ResolveJudgement(s),   Category: cat)); break;

            case "L'Île Volante de Marris":
                choices.Add(new("Parler à Marris et voir ce qu'il vend",           s => ResolveMarris(s),      Category: cat)); break;

            case "Le Conclave des Ombres":
                choices.Add(new("Acheter un secret",                               s => ResolveConclave(s),    s => s.Credits >= 1000, cat)); break;

            case "La Couronne d'Eos":
                choices.Add(new("Rencontrer le Président Eos",                     s => ResolveEos(s),         s => s.Reputation >= 200, cat)); break;

            case "Les Puits de Noctis":
                choices.Add(new("Libérer des mineurs esclaves",                    s => ResolveNoctis(s),      Category: cat)); break;

            case "L'Académie Stellaire":
                choices.Add(new("Parler à l'Archiviste Zenn",                      s => ResolveZenn(s),        Category: cat)); break;

            case "Le Nid des Faucons":
                choices.Add(new("Rencontrer La Faucon",                            s => ResolveFaucon(s),      Category: cat)); break;

            case "Sanctum Machina":
                choices.Add(new("Parler à ARIA",                                   s => ResolveARIA(s),        Category: cat)); break;

            case "La Citadelle Écarlate":
                choices.Add(new("Rencontrer Grand Gardien Sorath",                 s => ResolveSorath(s),      Category: cat)); break;

            case "Les Abysses de Velkor":
                choices.Add(new("Se soumettre aux expériences du Professeur Velkor", s => ResolveVelkor(s),    Category: cat)); break;

            case "Épave de l'Aurore Noire":
                choices.Add(new("Explorer l'épave légendaire",                     s => ResolveAuroreNoire(s), Category: cat)); break;

            case "Le Marché Flottant":
                choices.Add(new("Parler à Hamid le Chanceux",                      s => ResolveHamid(s),       Category: cat)); break;

            case "Terminus Sud":
                choices.Add(new("Parler à Yenna pour des infos de routes",         s => ResolveYenna(s),       Category: cat)); break;

            case "Fort Ossian":
                choices.Add(new("Parler au Général Ossian — contrats de guerre",   s => ResolveOssian(s),      Category: cat)); break;

            case "Station Belvédère":
                choices.Add(new("Parler à Lord Cassen",                            s => ResolveCassen(s),      Category: cat)); break;

            case "L'Entrepôt Zéro":
                choices.Add(new("Négocier avec Le Gérant",                         s => ResolveGerant(s),      s => s.Cargo.Get("Or") > 0, cat)); break;

            case "La Colonie Errante":
                choices.Add(new("Parler au Capitaine Vera",                        s => ResolveVera(s),        Category: cat)); break;

            case "La Forge des Damnés":
                choices.Add(new("Parler à l'Armurière Skade",                      s => ResolveSkade(s),       Category: cat));
                choices.Add(new("Libérer des travailleurs forcés",                 s => ResolveLibererOuvriers(s), Category: cat)); break;

            case "Le Phare de Vorn":
                choices.Add(new("Parler à l'Informateur Vorn",                     s => ResolveVorn(s),        Category: cat)); break;

            case "L'Arche de Sélène":
                choices.Add(new("Parler à la Coordinatrice Sélène",                s => ResolveSelene(s),      Category: cat)); break;

            case "Station Terminus Noir":
                choices.Add(new("Chercher quelqu'un qui veut disparaître",         s => ResolveTerminus(s),    Category: cat)); break;

            case "L'Observatoire":
                choices.Add(new("Parler à l'Astronome Lyra",                       s => ResolveLyra(s),        Category: cat)); break;

            case "Nid de Vorreth":
                choices.Add(new("Tenter une communication avec les Vorreth",       s => ResolveVorreth(s),     Category: cat)); break;

            case "La Station-Océan de Rhassil":
                choices.Add(new("Négocier par gestes avec les Rhassil",            s => ResolveRhassil(s),     Category: cat)); break;

            case "La République de Cellule 9":
                choices.Add(new("Rencontrer le Président-Condamné",                s => ResolveCellule9(s),    Category: cat)); break;

            case "Assemblée Première":
                choices.Add(new("Dialoguer avec les Premiers",                     s => ResolveAssemblee(s),   Category: cat)); break;

            case "Le Marché des Damnés":
                choices.Add(new("Plonger dans le marché — tout risquer",           s => ResolveMarcheDamnes(s),Category: cat)); break;
        }
    }

    // ── ÉVÉNEMENTS DE VOYAGE AVEC CHOIX ─────────────────────────────────────

    public static Situation PirateEncounter(int stolen)
    {
        Narrator.Say("Des signaux d'alerte s'allument sur ton tableau de bord. Des vaisseaux pirates te coupent la route...", Color.Red);
        return new Situation("Des pirates t'interceptent en plein vol.", new List<Choice>
        {
            new($"Payer ({stolen}cr)",
                s => { s.Credits = Math.Max(0, s.Credits - stolen); Display.ShowEvent($"Tu paies les {stolen}cr. Ils repartent en ricanant.", Color.Red); s.Reputation -= 5; Narrator.Pause(); },
                s => s.Credits >= stolen),

            new("Combattre",
                s =>
                {
                    var enemy   = Combat.GetForStation(s.CurrentStation);
                    var outcome = Combat.Start(s, enemy);
                    ApplyCombatOutcome(s, outcome);
                }),

            new("Fuir",
                s =>
                {
                    var ok = Rng.Next(100) < 55 + (s.Fuel > 2 ? 15 : 0);
                    if (ok) { s.Fuel--; s.Fuel = Math.Max(0, s.Fuel); Display.ShowEvent("Tu fuis à pleine puissance. -1 carburant mais tu t'en sors.", Color.Yellow); }
                    else { s.Credits = Math.Max(0, s.Credits - stolen); Display.ShowEvent($"Ils te rattrapent. -{stolen}cr.", Color.Red); }
                    Narrator.Pause();
                },
                s => s.Fuel > 0),

            new("Négocier",
                s =>
                {
                    var ok = Rng.Next(100) < 25 + Math.Max(0, s.Reputation / 8);
                    if (ok) { Display.ShowEvent("Tu les convaincs. Ils te laissent passer, impressionnés.", Color.Green); s.Reputation += 8; }
                    else { s.Credits = Math.Max(0, s.Credits - stolen); Display.ShowEvent($"La négociation échoue. -{stolen}cr.", Color.Red); }
                    Narrator.Pause();
                }),

            new("Intimider",
                s =>
                {
                    var ok = Rng.Next(100) < Math.Max(0, s.Reputation / 4);
                    if (ok) Display.ShowEvent("Ta réputation les précède. Ils repartent sans demander leur reste.", Color.Green);
                    else { s.Credits = Math.Max(0, s.Credits - stolen); Display.ShowEvent($"Ils se moquent de toi. -{stolen}cr.", Color.Red); }
                    Narrator.Pause();
                },
                s => s.Reputation > 50),
        }, Color.Red);
    }

    public static Situation MerchantDistress()
    {
        Narrator.Say("Une voix grésille sur les communications. Un signal de détresse. Quelqu'un a besoin d'aide...", Color.Yellow);
        return new Situation("Un vaisseau marchand envoie un signal de détresse. Que fais-tu ?", new List<Choice>
        {
            new("L'aider", s => { var r = Rng.Next(300, 900); s.Credits += r; s.Reputation += 25; Display.ShowEvent($"Tu le sauves. +{r}cr, +25 réputation.", Color.Green); Narrator.Pause(); }),
            new("Ignorer et continuer", s => { s.Reputation -= 5; Display.ShowEvent("Tu passes ton chemin.", Color.Grey); Narrator.Pause(); }),
            new("Le piller", s => { var l = Rng.Next(400, 1200); s.Credits += l; s.Reputation -= 35; Display.ShowEvent($"+{l}cr. -35 réputation.", Color.OrangeRed1); Narrator.Pause(); }, s => s.Reputation > -800),
        }, Color.Yellow);
    }

    public static Situation MysteriousCapsule()
    {
        Narrator.Say("Quelque chose dérive dans ta direction. Une capsule scellée. Pas de marquage identifiable...", Color.Gold1);
        return new Situation("Une capsule dérive dans l'espace. Que fais-tu ?", new List<Choice>
        {
            new("L'ouvrir", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0: var cr = Rng.Next(300, 1800); s.Credits += cr; Display.ShowEvent($"Des crédits ! +{cr}cr.", Color.Green); break;
                    case 1: var d = Rng.Next(10, 35); s.ShipHp = Math.Max(1, s.ShipHp - d); Display.ShowEvent($"Piégée ! Explosion. -{d} PV vaisseau.", Color.Red); break;
                    case 2: s.Cargo.Add("Artefacts", 1); Display.ShowEvent("Un artefact ancien. Ça vaut peut-être une fortune.", Color.Gold1); break;
                }
                Narrator.Pause();
            }),
            new("La remorquer et la vendre", s => { s.Fuel--; s.Fuel = Math.Max(0, s.Fuel); var v = Rng.Next(200, 700); s.Credits += v; Display.ShowEvent($"+{v}cr. -1 carburant.", Color.Green); Narrator.Pause(); }, s => s.Fuel > 1),
            new("L'ignorer", s => { Display.ShowEvent("Tu passes. C'était peut-être rien.", Color.Grey); Narrator.Pause(); }),
        }, Color.Gold1);
    }

    // ── RÉSOLUTIONS SPÉCIFIQUES AUX STATIONS ────────────────────────────────

    public static void ApplyCombatOutcome(GameState state, CombatOutcome outcome)
    {
        switch (outcome)
        {
            case CombatOutcome.Victory:
                QuestSystem.CheckRevengeQuests(state);
                break; // Loot déjà appliqué dans Combat.Start
            case CombatOutcome.Fled:
                // Fuite réussie : aucune pénalité, le message a déjà été affiché pendant le combat
                Narrator.Pause();
                break;
            case CombatOutcome.Stunned:
                state.PlayerHp = Math.Max(1, state.PlayerMaxHp / 4);
                state.Stamina  = 0;   // assommé : tu te réveilles vidé
                var lost = Rng.Next(100, 500);
                state.Credits = Math.Max(0, state.Credits - lost);
                Narrator.Say($"Le noir. Puis le néon d'un plafond inconnu. Tu te réveilles courbaturé, vidé, allégé de {lost}cr. Tu reviens de loin avec un quart de tes forces.", Color.Red);
                Narrator.Pause();
                break;
            case CombatOutcome.Captured:
                state.PlayerHp = Math.Max(1, state.PlayerMaxHp / 4);
                Narrator.Say("Tu reprends conscience, les mains liées. Une lumière t'aveugle.", Color.Red);
                Narrator.Pause();
                Interrogation(state);
                break;
            case CombatOutcome.Quiz:
                state.PlayerHp = Math.Max(1, state.PlayerMaxHp / 4);
                TriviaInterrogation(state);
                break;
            case CombatOutcome.Dead:
                state.PlayerHp  = 0;
                state.IsDead    = true;
                state.DeathCause = "Tombé au combat. Personne n'a rien fait pour l'empêcher.";
                Narrator.Say("Tu ne te relèves pas cette fois. Le vide récupère ce qui lui appartient.", Color.Red);
                Narrator.Pause();
                break;
        }
    }

    static void Interrogation(GameState state)
    {
        AnsiConsole.MarkupLine("\n[red bold]── INTERROGATOIRE ──[/]");
        Narrator.Say("Une silhouette s'assoit en face de toi. 'Tu vas nous dire ce qu'on veut savoir. Ou pas. Dans les deux cas ça va prendre du temps.'", Color.Red);
        AnsiConsole.WriteLine();

        state.InterrogationsSurvived++;

        ChoiceMenu.Resolve(new Situation("Comment tu joues ça ?",
        [
            new("Parler — donner des informations sur quelqu'un", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        s.Reputation -= 40;
                        Narrator.Say("Tu lâches un nom. Ils vérifient. C'est vrai. Ils te relâchent avec un avertissement. -40 réputation. Tu as vendu quelqu'un.", Color.Red);
                        break;
                    case 1:
                        s.Reputation -= 20;
                        var info = Rng.Next(300, 800);
                        s.Credits += info;
                        Narrator.Say($"Ils apprécient ce que tu sais. Ils te gardent une nuit puis te relâchent avec une 'compensation'. -{20} réputation. +{info}cr.", Color.Yellow);
                        break;
                    case 2:
                        s.Reputation -= 30;
                        s.IsImprisoned = true;
                        Narrator.Say("Tu parles mais ce que tu dis les intéresse encore plus. Ils t'envoient en cellule pour approfondir. -30 réputation.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("Garder le silence — ne rien dire", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(20, 45));
                        s.IsImprisoned = true;
                        Narrator.Say("Ils n'apprécient pas le silence. Ils t'envoient en cellule après avoir montré leur mécontentement. -PV joueur.", Color.Red);
                        break;
                    case 1:
                        s.Reputation += 15;
                        Narrator.Say("Tu gardes le silence pendant trois heures. Ils se lassent. Ils te relâchent faute de preuves. +15 réputation. Tu n'as trahi personne.", Color.Green);
                        break;
                    case 2:
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(10, 25));
                        s.Reputation += 5;
                        Narrator.Say("Rien. Tu dis rien. Ils te relâchent après douze heures. -PV. +5 réputation. Respect minimal.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),

            new("Mentir — donner de fausses informations", s =>
            {
                var hackBonus = s.Class.Name == "Hackeur" ? 30 : s.Class.Name == "Contrebandier" ? 20 : 0;
                var chance    = 40 + hackBonus;
                if (Rng.Next(100) < chance)
                {
                    Narrator.Say("Ils te croient. Ils s'en vont vérifier. Tu as du temps devant toi pour partir. Bien joué.", Color.Green);
                    s.Reputation += 5;
                }
                else
                {
                    s.Reputation -= 25;
                    s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(20, 40));
                    s.IsImprisoned = true;
                    Narrator.Say("Ils ont vérifié immédiatement. Tu es en cellule, la tête en moins bonne forme. -25 réputation, -PV.", Color.Red);
                }
                Narrator.Pause();
            }),

            new("Tenter de négocier — proposer de travailler pour eux", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        s.Reputation -= 15;
                        var deal = Rng.Next(500, 1500);
                        s.Credits += deal;
                        Narrator.Say($"Intéressant. Ils acceptent. Tu sors avec une mission et un paiement d'avance. +{deal}cr. -15 réputation.", Color.Yellow);
                        break;
                    case 1:
                        s.IsImprisoned = true;
                        Narrator.Say("'Travailler pour nous ? Non.' Cellule. Directement.", Color.Red);
                        break;
                    case 2:
                        s.Reputation += 10;
                        Narrator.Say("Ils réfléchissent. Ils te relâchent pour l'instant. 'On te recontactera.' Ça sonne pas bien. +10 réputation.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),
        ], Color.Red), state);
    }

    // ── INTERROGATOIRE CULTURE G À LA CON ─────────────────────────────────────
    // Tu te fais bloquer par des types qui s'ennuient et te cuisinent sur des
    // questions débiles. Bonne réponse → tu files. Mauvaise → ça tourne mal.

    record TriviaQ(string Question, string[] Options, int Correct, string Theme);

    static readonly List<TriviaQ> TriviaPool =
    [
        new("Combien font 7 × 8 ?",                        ["54", "56", "58", "48"],            1, "Maths"),
        new("Combien font 12 × 12 ?",                      ["124", "144", "121", "132"],        1, "Maths"),
        new("Quelle est la racine carrée de 81 ?",         ["7", "8", "9", "11"],               2, "Maths"),
        new("Combien de côtés a un hexagone ?",            ["5", "6", "7", "8"],                1, "Maths"),
        new("Combien font 15 % de 200 ?",                  ["25", "30", "35", "40"],            1, "Maths"),
        new("Combien de joueurs dans une équipe de foot sur le terrain ?", ["9", "10", "11", "12"], 2, "Sport"),
        new("Dans quel sport marque-t-on un 'strike' ?",   ["Tennis", "Bowling", "Boxe", "Golf"], 1, "Sport"),
        new("Un marathon, c'est environ combien de km ?",  ["21", "32", "42", "50"],            2, "Sport"),
        new("Combien de points vaut un panier à 3 points ?", ["1", "2", "3", "4"],              2, "Sport"),
        new("Combien de planètes dans le système solaire d'origine ?", ["7", "8", "9", "10"],   1, "Espace"),
        new("Quel astre est au centre du système solaire ?", ["La Lune", "Mars", "Le Soleil", "Jupiter"], 2, "Espace"),
        new("Combien de continents sur la Terre d'origine ?", ["5", "6", "7", "8"],             2, "Géo"),
        new("Quelle est la couleur obtenue en mélangeant bleu et jaune ?", ["Vert", "Orange", "Violet", "Marron"], 0, "Culture"),
        new("Combien de cordes sur une guitare classique ?", ["4", "5", "6", "7"],              2, "Culture"),
        new("Combien de jours dans une semaine ?",         ["5", "6", "7", "8"],                2, "Culture"),
    ];

    static void TriviaInterrogation(GameState state)
    {
        AnsiConsole.MarkupLine("\n[orange1 bold]── INTERROGATOIRE ──[/]");
        Narrator.Say("On ne te tue pas. Pire : on te bloque sur une chaise sous une lampe trop forte. Un type s'assoit, croque une pomme, et ouvre un cahier de quiz tout corné. 'Si t'es si malin, réponds à ça. Une erreur et la soirée va être longue.'", Color.OrangeRed1);

        state.InterrogationsSurvived++;   // tu y survis quoi qu'il arrive — reste à savoir dans quel état

        var q = TriviaPool[Rng.Next(TriviaPool.Count)];

        var choices = new List<Choice>();
        for (int i = 0; i < q.Options.Length; i++)
        {
            var idx = i;
            choices.Add(new Choice(q.Options[idx], s => ResolveTrivia(s, idx == q.Correct)));
        }

        ChoiceMenu.Resolve(new Situation($"[grey]({q.Theme})[/] {q.Question}", choices, Color.OrangeRed1), state);
    }

    static void ResolveTrivia(GameState state, bool correct)
    {
        if (correct)
        {
            Narrator.Say("Il vérifie sur son cahier, déçu. 'Ouais... c'est ça.' Il te détache à contrecœur. 'Dégage avant que j'trouve une question plus dure.'", Color.Green);
            state.Reputation += 5;
            Display.ShowEvent("Bonne réponse. Tu files. +5 réputation (ils sont presque impressionnés).", Color.Green);
            Narrator.Pause();
            return;
        }

        Narrator.Say("Il claque le cahier. 'FAUX. Tout le monde sait ça, voyons.' L'ambiance se dégrade.", Color.Red);
        switch (Rng.Next(3))
        {
            case 0:
                var lost = Rng.Next(200, 700);
                state.Credits = Math.Max(0, state.Credits - lost);
                Display.ShowEvent($"Ils te 'facturent' la leçon. -{lost}cr. Tu repars humilié mais libre.", Color.Red);
                break;
            case 1:
                var dmg = Rng.Next(15, 35);
                state.PlayerHp = Math.Max(1, state.PlayerHp - dmg);
                Display.ShowEvent($"Une claque derrière la tête 'pour t'aider à réfléchir'. -{dmg} PV. Puis on te jette dehors.", Color.Red);
                break;
            case 2:
                state.IsImprisoned = true;
                Display.ShowEvent("'Un ignorant ET un criminel.' Ils t'enferment le temps de décider quoi faire de toi.", Color.Red);
                break;
        }
        Narrator.Pause();
    }

    // ── DEALER DANS LES VILLES MALFAMÉES ────────────────────────────────────
    // Disponible uniquement dans les zones danger ≥ 3.
    // La règle : si tu es mal équipé (pas d'arme, PV bas), le combat est
    // une option désespérée. Les dealers sérieux ne rigolent pas.
    static void ResolveDealerSearch(GameState state)
    {
        var danger = Universe.Danger(Universe.Get(state.CurrentStation));
        Narrator.Say("Tu traînes dans les couloirs sombres, tu fais des signes discrets aux bonnes personnes...", Color.Magenta1);

        switch (Rng.Next(4))
        {
            // Dealer de base — transaction possible
            case 0:
                Narrator.Say("Un type s'extrait d'une ombre et s'approche sans se presser. Il t'a déjà jaugé. Il sait ce que tu veux ou pas.", Color.Magenta1);
                DealerTransaction(state, danger, hostile: false);
                break;

            // Dealer hostile — deal qui tourne mal
            case 1:
                Narrator.Say("Tu trouves quelqu'un. La transaction démarre. Quelque chose ne va pas.", Color.Red);
                DealerTransaction(state, danger, hostile: true);
                break;

            // Dealer qui t'invite dans son réseau
            case 2:
                Narrator.Say("Une femme t'accoste avant que tu cherches. 'T'as pas besoin de chercher. C'est moi que tu cherches.'", Color.Magenta1);
                ChoiceMenu.Resolve(new Situation("Elle te propose une affaire plus importante.",
                [
                    new("Écouter sa proposition", s =>
                    {
                        var paye = Rng.Next(1200, 3500);
                        Narrator.Say($"Un convoi à escorter. Pas de questions. {paye}cr. Elle attend ta réponse.", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Tu acceptes ?",
                        [
                            new("Oui — c'est bien payé", gs => {
                                if (Rng.Next(100) < 60) { gs.Credits += paye; gs.Reputation -= 20; Narrator.Say($"Fait. +{paye}cr. -20 rép. Ce que transportait le convoi, tu préfères pas savoir.", Color.Gold1); }
                                else { gs.Reputation -= 40; gs.IsImprisoned = true; Narrator.Say("Une embuscade. Des gardes. Elle t'a vendu.", Color.Red); }
                                Narrator.Pause();
                            }),
                            new("Non — trop risqué", gs => { Narrator.Say("'Dommage. Porte-toi bien.' Elle disparaît.", Color.Grey); Narrator.Pause(); }),
                        ], Color.Yellow), s);
                    }),
                    new("Refuser sans l'écouter", s => { Narrator.Say("Elle hausse les épaules. 'La prochaine fois peut-être.'", Color.Grey); Narrator.Pause(); }),
                ], Color.Magenta1), state);
                break;

            // Pas de dealer disponible
            default:
                Narrator.Say("T'as cherché. Personne. Soit ils sont occupés, soit quelqu'un a préparé une descente et tout le monde le sait sauf toi.", Color.Grey);
                if (Rng.Next(100) < 30)
                {
                    Narrator.Say("Ton insistance attire une attention indésirable.", Color.Red);
                    var e = Combat.GetScaled(state, 1);
                    ApplyCombatOutcome(state, Combat.Start(state, e));
                    return;
                }
                Narrator.Pause();
                break;
        }
    }

    static void DealerTransaction(GameState state, int danger, bool hostile)
    {
        var hasWeapon = state.EquippedWeapon != null;
        var lowHp     = state.PlayerHp < state.PlayerMaxHp * 0.35f;

        var choices = new List<Choice>();

        // Option achat
        choices.Add(new("Acheter et consommer sur place", s =>
        {
            var cost = Rng.Next(100, 350);
            if (s.Credits < cost) { Narrator.Say("T'as même pas les crédits. Il te regarde partir.", Color.Grey); Narrator.Pause(); return; }
            s.Credits -= cost; s.AddictionLevel++;
            switch (Rng.Next(5))
            {
                case 0: Narrator.Say("Clarity absolue. Tu vois les patterns, les connexions. Puis ça s'arrête.", Color.Cyan1); s.Credits += Rng.Next(300, 900); break;
                case 1: Narrator.Say("T'as perdu trois heures. Quelqu'un t'a allégé pendant ce temps.", Color.Red); s.Credits = Math.Max(0, s.Credits - Rng.Next(200, 700)); break;
                case 2: s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 30); s.Stamina = s.MaxStamina; Narrator.Say("Un pic. Puis le calme. Reposé, alerte. Pour l'instant.", Color.Green); break;
                case 3: s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(25, 60)); Narrator.Say("Réaction allergique. Sévère. -PV.", Color.Red); break;
                case 4: Narrator.Say("T'as dormi seize heures. T'as envie de recommencer. Signe de mauvais présage.", Color.Yellow); break;
            }
            Narrator.Pause();
        }));

        // Option achat pour revente
        choices.Add(new("Acheter pour revendre plus loin", s =>
        {
            var cost = Rng.Next(200, 500);
            if (s.Credits < cost) { Narrator.Say("Pas assez.", Color.Grey); Narrator.Pause(); return; }
            s.Credits -= cost; s.Cargo.Add("Marchandises illégales", 1);
            s.Reputation -= 10;
            Narrator.Say($"-{cost}cr. +1 Marchandises illégales. -10 rép.", Color.OrangeRed1);
            Narrator.Pause();
        }));

        // Si hostile — une option de combat MAIS clairement plus difficile sans arme
        if (hostile)
        {
            choices.Add(new("La transaction tourne mal — se battre", s =>
            {
                if (!hasWeapon)
                {
                    Narrator.Say("T'es à mains nues face à quelqu'un qui vit de ça. Il t'a regardé une seconde. Il sait. La suite est courte.", Color.Red);
                    s.PlayerHp = Math.Max(1, s.PlayerHp / 4);
                    s.Stamina  = 0;
                    var vol = Rng.Next(200, 800); s.Credits = Math.Max(0, s.Credits - vol);
                    Narrator.Say($"Tu te réveilles dans un couloir. -{vol}cr. 0 stamina. Un quart de ta vie.", Color.Red);
                    Narrator.Pause();
                    return;
                }
                if (lowHp)
                    Narrator.Say("T'es à moins de 35% de tes PV. Cette décision pourrait être la dernière.", Color.Red);

                var e = Combat.Scale(Combat.GetScaled(s, 1), danger - 2);
                ApplyCombatOutcome(s, Combat.Start(s, e));
            }));
        }

        // Toujours : partir sans transaction
        choices.Add(new("Refuser — repartir", s =>
        {
            if (hostile && Rng.Next(100) < 40)
            {
                Narrator.Say("Il n'aime pas qu'on vienne perdre son temps.", Color.Red);
                var e = Combat.GetScaled(s, 1);
                ApplyCombatOutcome(s, Combat.Start(s, e));
                return;
            }
            Narrator.Say("T'es parti. Il t'a regardé partir.", Color.Grey);
            Narrator.Pause();
        }));

        ChoiceMenu.Resolve(new Situation("Le dealer t'attend. Que fais-tu ?", choices, Color.Magenta1), state);
    }

    static void ResolveExplore(GameState state)
    {
        StationAmbiance.Show(state.CurrentStation, isExploration: false);
        Encounters.Roll(state);
    }

    static void ResolveJobSearch(GameState state)
    {
        Narrator.Say("Tu frappes aux portes, tu tends l'oreille dans les bars...");
        switch (Rng.Next(4))
        {
            case 0: var p = Rng.Next(200, 600); state.Credits += p; Display.ShowEvent($"Livraison locale. +{p}cr.", Color.Green); break;
            case 1: Display.ShowEvent("Personne n'embauche. Ou alors pas pour toi.", Color.Grey); break;
            case 2: var s = Rng.Next(400, 1000); state.Credits += s; state.Reputation -= 15; Display.ShowEvent($"Un boulot douteux. +{s}cr, -15 réputation.", Color.OrangeRed1); break;
            case 3: var b = Rng.Next(300, 700); state.Credits += b; state.Reputation += 10; Display.ShowEvent($"Tu sauves quelqu'un d'une arnaque. +{b}cr, +10 réputation.", Color.Green); break;
        }
        Narrator.Pause();
    }

    // ── PNJ NOUVELLES STATIONS ───────────────────────────────────────────────

    static void ResolveVorreth(GameState state)
    {
        state.NpcsMet.Add("Vorreth");
        Narrator.Say("Tu t'avances. Un Vorreth s'extrait d'une cellule de cire — corps chitineux, yeux multiples, membres articulés. Il t'observe sans bouger.", Color.Yellow);
        ChoiceMenu.Resolve(new Situation("Communication avec l'entité Vorreth.",
        [
            new("Lui offrir quelque chose — un objet de ta cargaison", s =>
            {
                if (!s.Cargo.All.Any()) { Narrator.Say("T'as rien à offrir. Il recule dans sa cellule.", Color.Grey); Narrator.Pause(); return; }
                var item = s.Cargo.All.Keys.First();
                s.Cargo.Remove(item, 1);
                switch (Rng.Next(3)) {
                    case 0: s.Cargo.Add("Artefacts", 2); s.Reputation += 20; Narrator.Say($"Il prend le {item}. Il disparaît. Un moment plus tard, deux artefacts tombent devant toi. +2 Artefacts, +20 rép.", Color.Gold1); break;
                    case 1: var cr = Rng.Next(800, 2500); s.Credits += cr; Narrator.Say($"Il t'offre un équivalent en crédits. +{cr}cr.", Color.Cyan1); break;
                    case 2: Narrator.Say("Il prend le {item}. Et repart. Tu as offert sans recevoir. C'était peut-être un test.", Color.Grey); break;
                }
                Narrator.Pause();
            }, s => s.Cargo.All.Any()),
            new("Tenter de toucher sa carapace — par curiosité", s =>
            {
                if (Rng.Next(100) < 40) { s.Reputation += 15; Narrator.Say("Il laisse faire. Un bourdonnement grave parcourt ses mandibules. C'était peut-être un signe de bienvenue. +15 rép.", Color.Green); }
                else { var hp = Rng.Next(15, 35); s.PlayerHp = Math.Max(1, s.PlayerHp - hp); Narrator.Say($"Mauvaise idée. -{hp} PV. Vite.", Color.Red); }
                Narrator.Pause();
            }),
            new("Repartir — trop étrange", s => { Narrator.Say("Il te regarde partir sans bouger. Tu seras dans sa mémoire de cire.", Color.Grey); Narrator.Pause(); }),
        ], Color.Yellow), state);
    }

    static void ResolveRhassil(GameState state)
    {
        state.NpcsMet.Add("Rhassil");
        Narrator.Say("Ils glissent dans l'eau qui recouvre les couloirs. L'un d'eux émerge à mi-corps et te fait face. Ses yeux latéraux clignotent en alternance — une question, peut-être.", Color.Cyan1);
        ChoiceMenu.Resolve(new Situation("Commerce par gestes. Qu'est-ce que tu tentes de communiquer ?",
        [
            new("Montrer tes crédits — proposer un échange", s =>
            {
                switch (Rng.Next(3)) {
                    case 0: var cr = Rng.Next(500, 2000); s.Credits += cr; s.Reputation += 15; Narrator.Say($"Ils comprennent. La transaction a lieu par gestes complexes. +{cr}cr, +15 rép.", Color.Cyan1); break;
                    case 1: s.Cargo.Add("Plantes médicinales", 3); Narrator.Say("Ils te tendent trois plantes d'une couleur inconnue. +3 Plantes médicinales.", Color.Green); break;
                    case 2: Narrator.Say("Il y a eu un malentendu dans les gestes. Quelque chose te revient moins cher que prévu mais tu sais pas quoi.", Color.Grey); break;
                }
                Narrator.Pause();
            }),
            new("Mimer que tu cherches de l'aide pour ton vaisseau", s =>
            {
                s.ShipHp = Math.Min(s.ShipMaxHp, s.ShipHp + Rng.Next(20, 45));
                Narrator.Say("Quatre Rhassil se déplacent vers ton vaisseau. Deux heures plus tard, il est partiellement réparé. La méthode était organique. Efficace.", Color.Green);
                Narrator.Pause();
            }),
            new("Essayer de parler — voir s'ils comprennent les langues humaines", s =>
            {
                if (Rng.Next(100) < 25) { s.Reputation += 25; Narrator.Say("Un Rhassil plus vieux s'approche. Il parle. Lentement. Dans plusieurs langues. Il en connaît une que tu reconnais. +25 rép.", Color.Gold1); }
                else Narrator.Say("Incompréhension totale. Ils se regardent. Il y a un bruit qui ressemble peut-être à de la politesse.", Color.Grey);
                Narrator.Pause();
            }),
        ], Color.Cyan1), state);
    }

    static void ResolveCellule9(GameState state)
    {
        state.NpcsMet.Add("Président-Condamné");
        Narrator.Say("Le Président-Condamné te reçoit dans son bureau — une ancienne cellule dont les murs sont couverts de texte légal. Il a trois tatouages de sentence sur l'avant-bras.", Color.OrangeRed1);
        ChoiceMenu.Resolve(new Situation("Le Président-Condamné te regarde. Il a une proposition.",
        [
            new("Écouter sa proposition", s =>
            {
                var paye = Rng.Next(2000, 6000);
                Narrator.Say($"Une extraction. Quelqu'un est emprisonné ailleurs. {paye}cr si tu le sors. 'Ici on sait mieux que personne comment faire ça.'", Color.OrangeRed1);
                ChoiceMenu.Resolve(new Situation("Tu acceptes ?",
                [
                    new("Oui", gs => { if (Rng.Next(100) < 55) { gs.Credits += paye; gs.Reputation -= 20; Narrator.Say($"+{paye}cr. -20 rép.", Color.Gold1); } else { gs.IsImprisoned = true; Narrator.Say("Le prisonnier était un appât.", Color.Red); } Narrator.Pause(); }),
                    new("Non", gs => { Narrator.Say("'La porte est ouverte.' Il ne te regarde plus.", Color.Grey); Narrator.Pause(); }),
                ], Color.OrangeRed1), s);
            }),
            new("Lui demander la constitution de la République", s =>
            {
                s.Reputation += 20;
                Narrator.Say("Il te lit les 47 articles. C'est épique, naïf, et cohérent. +20 rép.", Color.Cyan1);
                Narrator.Pause();
            }),
            new("Lui proposer de travailler pour lui", s =>
            {
                s.FactionMissions++;
                var cr = Rng.Next(800, 2000); s.Credits += cr; s.Reputation -= 10;
                Narrator.Say($"Mission discrète faite. +{cr}cr. -10 rép. La République te doit quelque chose.", Color.OrangeRed1);
                Narrator.Pause();
            }),
        ], Color.OrangeRed1), state);
    }

    static void ResolveAssemblee(GameState state)
    {
        state.NpcsMet.Add("Les Premiers");
        Narrator.Say("Un robot s'approche. Ses yeux s'allument en bleu. 'Identification : organique. Catégorie : visiteur. Probabilité de menace : 12%. Dialogue engagé.'", Color.SteelBlue1);
        ChoiceMenu.Resolve(new Situation("Les Premiers t'évaluent. Comment tu te comportes ?",
        [
            new("Proposer un échange d'informations", s =>
            {
                switch (Rng.Next(3)) {
                    case 0: s.Reputation += 30; var cr = Rng.Next(1000, 3000); s.Credits += cr; Narrator.Say($"Ils ont jugé tes informations pertinentes. +{cr}cr, +30 rép.", Color.SteelBlue1); break;
                    case 1: s.Cargo.Add("Cartes stellaires", 2); Narrator.Say("Ils te fournissent des données de navigation en retour. +2 Cartes stellaires.", Color.Cyan1); break;
                    case 2: Narrator.Say("Tes informations n'ont aucune valeur pour eux. Logique, mais humiliant.", Color.Grey); break;
                }
                Narrator.Pause();
            }),
            new("Demander une amélioration de vaisseau", s =>
            {
                var cout = Rng.Next(2000, 5000);
                if (s.Credits >= cout) {
                    s.Credits -= cout; s.ShipMaxHp += 25; s.ShipHp = Math.Min(s.ShipMaxHp, s.ShipHp + 25);
                    Narrator.Say($"Upgrade vaisseau. -{cout}cr. +25 PV max vaisseau permanent.", Color.Green);
                } else Narrator.Say("Fonds insuffisants. Ils archiveront la demande.", Color.Grey);
                Narrator.Pause();
            }),
            new("Les tester — déclencher une réaction", s =>
            {
                if (Rng.Next(100) < 30) { s.Reputation += 10; Narrator.Say("Ils enregistrent le comportement. 'Atypique. Intéressant.' +10 rép.", Color.Cyan1); }
                else { s.Reputation -= 25; Narrator.Say("'Comportement hostile. Menace reclassifiée à 78%.' Ils t'expulsent efficacement. -25 rép.", Color.Red); }
                Narrator.Pause();
            }),
        ], Color.SteelBlue1), state);
    }

    static void ResolveMarcheDamnes(GameState state)
    {
        state.NpcsMet.Add("Marché des Damnés");
        Narrator.Say("Le Marché des Damnés : une jungle de stands illégaux, de corps en mouvement, de cris et d'odeurs non identifiées. Tout est à vendre. Tout.", Color.OrangeRed1);
        ChoiceMenu.Resolve(new Situation("Que cherches-tu dans ce chaos ?",
        [
            new("Des armes de haute qualité", s =>
            {
                var cout = Rng.Next(2000, 5000);
                if (s.Credits >= cout) {
                    s.Credits -= cout;
                    var w = WeaponPool.RollForTier(Math.Min(5, Combat.MaxLootTier(s) + 1));
                    s.Weapons.Add(w);
                    Narrator.Say($"-{cout}cr. Le vendeur disparaît dans la foule.");
                    Combat.ShowWeaponDrop(w);
                } else Narrator.Say("T'as pas assez.", Color.Red);
                Narrator.Pause();
            }, s => s.Credits >= 2000),
            new("Des informations sur quelqu'un", s =>
            {
                var cout = Rng.Next(500, 1500); s.Credits = Math.Max(0, s.Credits - cout);
                switch (Rng.Next(3)) {
                    case 0: s.Reputation += 20; Narrator.Say($"-{cout}cr. L'info valait chaque crédit. +20 rép.", Color.Gold1); break;
                    case 1: Narrator.Say($"-{cout}cr. L'info était fausse. Quelqu'un vend des mensonges.", Color.Red); break;
                    case 2: s.Reputation -= 15; Narrator.Say($"-{cout}cr. L'info t'implique dans quelque chose que t'aurais préféré pas savoir. -15 rép.", Color.OrangeRed1); break;
                }
                Narrator.Pause();
            }),
            new("Juste fouiller — voir ce qui tombe", s =>
            {
                switch (Rng.Next(4)) {
                    case 0: var cr = Rng.Next(500, 1800); s.Credits += cr; Narrator.Say($"+{cr}cr trouvés dans la confusion.", Color.Gold1); break;
                    case 1: s.Cargo.Add("Marchandises illégales", 2); Narrator.Say("+2 Marchandises illégales. Quelqu'un les a perdues. Ou oubliées. T'as préféré pas demander.", Color.OrangeRed1); break;
                    case 2: var perdu = Rng.Next(300, 900); s.Credits = Math.Max(0, s.Credits - perdu); Narrator.Say($"Pickpocket pro. -{perdu}cr.", Color.Red); break;
                    case 3: Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.GetScaled(s, 2))); return;
                }
                Narrator.Pause();
            }),
            new("Repartir — trop de monde, trop de danger", s =>
            {
                Narrator.Say("Sage. Ce marché a avalé des gens meilleurs que toi.", Color.Grey);
                Narrator.Pause();
            }),
        ], Color.OrangeRed1), state);
    }

    public static void ResolveScroungeFuel(GameState state)
    {
        Narrator.Say("Réservoir vide. Tu pars à la recherche de carburant avec l'énergie du désespoir...", Color.Yellow);

        var zone = state.CurrentStation switch
        {
            "Forge Alpha" or "La Ferronnerie" or "L'Arc du Pic de l'Est" or "Sanctum Machina"
                or "L'Entrepôt Zéro" => FuelZone.Industrial,

            "La Carcasse" or "Les Bas-Fonds de Vega" or "Fort Kharos" or "Port des Brumes"
                or "Arc Ouest Apocalypse" or "Le Purgatoire" => FuelZone.Dangerous,

            "Les Décombres de Vael" or "Épave de l'Aurore Noire"
                or "Le Vaisseau Fantôme Errant" or "L'Arc Perdu" => FuelZone.Ruins,

            "Scotty Golden North" or "Star Quest" or "La Couronne d'Eos"
                or "Station Belvédère" or "Emporium Requiem" => FuelZone.Luxury,

            "Emporium Requiem" or "La Citadelle Écarlate" or "Fort Ossian"
                or "La Citadelle" or "Avant-Poste Kalem" => FuelZone.Military,

            _ => FuelZone.Generic,
        };

        switch (zone)
        {
            case FuelZone.Industrial:
                ScroungeFuelIndustrial(state); break;
            case FuelZone.Dangerous:
                ScroungeFuelDangerous(state); break;
            case FuelZone.Ruins:
                ScroungeFuelRuins(state); break;
            case FuelZone.Luxury:
                ScroungeFuelLuxury(state); break;
            case FuelZone.Military:
                ScroungeFuelMilitary(state); break;
            default:
                ScroungeFuelGeneric(state); break;
        }
    }

    enum FuelZone { Industrial, Dangerous, Ruins, Luxury, Military, Generic }

    static void ScroungeFuelIndustrial(GameState state)
    {
        Narrator.Say("Dans une zone industrielle, le carburant traîne partout. Il suffit de savoir chercher — et d'accepter les questions des techniciens.", Color.Grey);
        switch (Rng.Next(5))
        {
            case 0:
                var qty0 = Rng.Next(2, 5);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty0);
                Narrator.Say($"Un technicien sympa te laisse siphonner ce dont t'as besoin depuis un réservoir de surplus. +{qty0} carburant.", Color.Green);
                break;
            case 1:
                var cost1 = Rng.Next(100, 300);
                var qty1  = Rng.Next(1, 4);
                if (state.Credits >= cost1)
                {
                    state.Credits -= cost1;
                    state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty1);
                    Narrator.Say($"Un magasinier te vend du carburant 'non répertorié'. -{cost1}cr, +{qty1} carburant.", Color.Yellow);
                }
                else
                {
                    Narrator.Say("Il y a du carburant partout mais tout est verrouillé et tu peux pas te permettre les pots-de-vin.", Color.Red);
                }
                break;
            case 2:
                var qty2 = Rng.Next(1, 3);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty2);
                state.ShipHp = Math.Max(1, state.ShipHp - Rng.Next(10, 25));
                Narrator.Say($"T'as siphonné ce que tu pouvais depuis une canalisation qui était peut-être pas faite pour ça. +{qty2} carburant, -PV vaisseau.", Color.Yellow);
                break;
            case 3:
                var dmg3 = Rng.Next(15, 35);
                state.PlayerHp = Math.Max(1, state.PlayerHp - dmg3);
                Narrator.Say($"Réservoir sous pression. Tu l'as ouvert trop vite. Explosion partielle. -{dmg3} PV joueur, toujours à sec.", Color.Red);
                break;
            case 4:
                var workPay = Rng.Next(150, 400);
                var qty4 = Rng.Next(2, 4);
                state.Credits += workPay;
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty4);
                Narrator.Say($"Tu proposes tes bras pour quelques heures. Ils te paient et te laissent remplir ton réservoir. +{workPay}cr, +{qty4} carburant.", Color.Green);
                break;
        }
        Narrator.Pause();
    }

    static void ScroungeFuelDangerous(GameState state)
    {
        Narrator.Say("Dans cette zone, le carburant ne s'obtient pas proprement. Mais il s'obtient.", Color.Red);
        switch (Rng.Next(5))
        {
            case 0:
                var qty0 = Rng.Next(1, 3);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty0);
                state.Reputation -= 15;
                Narrator.Say($"T'as volé du carburant à quelqu'un qui avait l'air suffisamment occupé pour pas s'en apercevoir tout de suite. +{qty0} carburant, -15 réputation.", Color.Yellow);
                break;
            case 1:
                var cost1 = Rng.Next(200, 500);
                var qty1  = Rng.Next(1, 3);
                if (state.Credits >= cost1)
                {
                    state.Credits -= cost1;
                    state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty1);
                    Narrator.Say($"Un dealer de carburant frelaté. C'est de l'essence, en gros. -{cost1}cr, +{qty1} carburant. Qualité non garantie.", Color.Yellow);
                }
                else
                {
                    Narrator.Say("Il y a des gens qui vendent du carburant ici mais ils acceptent pas les dettes.", Color.Red);
                }
                break;
            case 2:
                Narrator.Say("Tu trouves quelqu'un. La négociation tourne mal.", Color.Red);
                Situations.ApplyCombatOutcome(state, Combat.Start(state, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                var qty2 = Rng.Next(1, 3);
                if (state.PlayerHp > 0)
                {
                    state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty2);
                    Narrator.Say($"Tu récupères du carburant sur lui après le combat. +{qty2} carburant.", Color.Gold1);
                }
                return;
            case 3:
                var qty3 = Rng.Next(2, 5);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty3);
                var cargo = state.Cargo.All.Keys.FirstOrDefault();
                if (cargo != null)
                {
                    state.Cargo.Remove(cargo, 1);
                    Narrator.Say($"Un type accepte d'échanger du carburant contre ta cargaison. +{qty3} carburant, -1 {cargo}.", Color.Yellow);
                }
                else
                {
                    state.Credits -= Math.Min(state.Credits, Rng.Next(150, 400));
                    Narrator.Say($"T'as pas grand chose à échanger. Il prend tes crédits et te donne le minimum. +{qty3} carburant.", Color.Yellow);
                }
                break;
            case 4:
                var dmg4 = Rng.Next(20, 45);
                state.PlayerHp = Math.Max(1, state.PlayerHp - dmg4);
                var volé = Rng.Next(100, 400);
                state.Credits = Math.Max(0, state.Credits - volé);
                Narrator.Say($"T'as cherché du carburant dans la mauvaise ruelle. Quelqu'un t'a cherché dessus. -{dmg4} PV, -{volé}cr. Toujours à sec.", Color.Red);
                break;
        }
        Narrator.Pause();
    }

    static void ScroungeFuelRuins(GameState state)
    {
        Narrator.Say("Les épaves et les ruines cachent parfois de vieilles réserves. Pas toujours utilisables. Pas toujours sans danger.", Color.Grey);
        switch (Rng.Next(5))
        {
            case 0:
                var qty0 = Rng.Next(2, 5);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty0);
                Narrator.Say($"Un réservoir de secours intact depuis des décennies. La valve est rouillée mais ça passe. +{qty0} carburant.", Color.Green);
                break;
            case 1:
                var qty1 = Rng.Next(1, 3);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty1);
                state.ShipHp = Math.Max(1, state.ShipHp - Rng.Next(15, 35));
                Narrator.Say($"Du carburant récupéré mais il était contaminé. Ça marche mais ça va pas faire du bien aux moteurs. +{qty1} carburant, -PV vaisseau.", Color.Yellow);
                break;
            case 2:
                var dmg2 = Rng.Next(20, 50);
                state.ShipHp = Math.Max(1, state.ShipHp - dmg2);
                Narrator.Say($"Réservoir explosif. T'aurais dû vérifier la pression avant. -{dmg2} PV vaisseau, toujours à sec.", Color.Red);
                break;
            case 3:
                Narrator.Say("Rien. Les épaves ont été vidées avant toi. Par qui et quand, aucune idée.", Color.Grey);
                break;
            case 4:
                var qty4 = Rng.Next(3, 6);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty4);
                state.Cargo.Add("Artefacts", 1);
                Narrator.Say($"Une salle entière de réserves préservées. Du carburant, et autre chose que tu embarques sans trop poser de questions. +{qty4} carburant, +1 Artefact.", Color.Gold1);
                break;
        }
        Narrator.Pause();
    }

    static void ScroungeFuelLuxury(GameState state)
    {
        Narrator.Say("Dans une station de luxe, le carburant existe. Il est propre, il est fiable, et il coûte exactement ce que tu peux pas te permettre.", Color.Gold1);
        switch (Rng.Next(4))
        {
            case 0:
                var cost0 = Rng.Next(300, 700);
                var qty0  = Rng.Next(2, 4);
                if (state.Credits >= cost0)
                {
                    state.Credits -= cost0;
                    state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty0);
                    Narrator.Say($"Tu paies le prix fort dans une station-service de standing. -{cost0}cr, +{qty0} carburant. Propre et efficace.", Color.Green);
                }
                else
                {
                    Narrator.Say("Le pompiste te regarde comme si tu lui avais proposé de payer en boutons. T'as pas les moyens ici.", Color.Red);
                }
                break;
            case 1:
                var qty1 = Rng.Next(1, 3);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty1);
                state.Reputation += 10;
                Narrator.Say($"Tu expliques ta situation à quelqu'un qui a visiblement les moyens de s'en moquer. Il te donne du carburant par philanthropie ou par ennui. +{qty1} carburant, +10 réputation.", Color.Cyan1);
                break;
            case 2:
                var cost2 = Rng.Next(200, 500);
                var qty2  = Rng.Next(1, 3);
                if (state.Credits >= cost2)
                {
                    state.Credits -= cost2;
                    state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty2);
                    state.Reputation -= 10;
                    Narrator.Say($"Un employé accepte un arrangement discret. -{cost2}cr, +{qty2} carburant, -10 réputation.", Color.Yellow);
                }
                else
                {
                    Narrator.Say("Même les arrangements discrets coûtent cher ici.", Color.Red);
                }
                break;
            case 3:
                Narrator.Say("Personne ne t'aide sans que ça te coûte quelque chose que t'as pas. Tu restes à sec.", Color.Red);
                state.Reputation -= 5;
                break;
        }
        Narrator.Pause();
    }

    static void ScroungeFuelMilitary(GameState state)
    {
        Narrator.Say("Du carburant militaire dans une zone restreinte. Techniquement interdit. Techniquement pas impossible.", Color.Red);
        switch (Rng.Next(5))
        {
            case 0:
                var qty0 = Rng.Next(2, 4);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty0);
                state.Reputation -= 20;
                Narrator.Say($"Tu voles du carburant militaire pendant un changement de garde. +{qty0} carburant, -20 réputation. Quelqu'un a vu mais a décidé de pas s'impliquer.", Color.Yellow);
                break;
            case 1:
                var pay1 = Rng.Next(150, 400);
                var qty1 = Rng.Next(1, 3);
                if (state.Credits >= pay1)
                {
                    state.Credits -= pay1;
                    state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty1);
                    Narrator.Say($"Un soldat corrompu te vend du carburant de la réserve d'urgence. -{pay1}cr, +{qty1} carburant. Affaire conclue.", Color.Yellow);
                }
                else
                {
                    Narrator.Say("Les soldats corrompus coûtent quand même quelque chose.", Color.Red);
                }
                break;
            case 2:
                Narrator.Say("Une patrouille te surprend près des réservoirs. Ils posent des questions.", Color.Red);
                state.IsImprisoned = true;
                Narrator.Pause(); return;
            case 3:
                var qty3 = Rng.Next(3, 5);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty3);
                state.Reputation += 15;
                Narrator.Say($"Un officier t'autorise à utiliser la réserve en échange d'une information utile. +{qty3} carburant, +15 réputation.", Color.Green);
                break;
            case 4:
                var dmg4 = Rng.Next(15, 40);
                state.PlayerHp = Math.Max(1, state.PlayerHp - dmg4);
                Narrator.Say($"La sécurité ne plaisante pas avec les intrus près des réservoirs. -{dmg4} PV joueur. Tu repars bredouille.", Color.Red);
                break;
        }
        Narrator.Pause();
    }

    static void ScroungeFuelGeneric(GameState state)
    {
        Narrator.Say("T'as rien de spécial ici. Tu te débrouilles.", Color.Grey);
        switch (Rng.Next(5))
        {
            case 0:
                var qty0 = Rng.Next(1, 3);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty0);
                Narrator.Say($"T'as trouvé des cellules de carburant abandonnées dans un couloir de service. +{qty0} carburant.", Color.Green);
                break;
            case 1:
                var cost1 = Rng.Next(120, 350);
                var qty1  = Rng.Next(1, 3);
                if (state.Credits >= cost1)
                {
                    state.Credits -= cost1;
                    state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty1);
                    Narrator.Say($"Un passant accepte de te vendre ce qu'il avait en trop. -{cost1}cr, +{qty1} carburant.", Color.Yellow);
                }
                else
                {
                    Narrator.Say("Pas assez de crédits pour convaincre qui que ce soit.", Color.Red);
                }
                break;
            case 2:
                var qty2 = Rng.Next(1, 2);
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + qty2);
                state.Reputation -= 10;
                Narrator.Say($"Tu en as pris là où tu pouvais. C'était pas à toi. +{qty2} carburant, -10 réputation.", Color.Yellow);
                break;
            case 3:
                Narrator.Say("Deux heures de recherche pour rien. La station n'a rien à t'offrir aujourd'hui.", Color.Grey);
                break;
            case 4:
                var dmg4 = Rng.Next(10, 25);
                state.PlayerHp = Math.Max(1, state.PlayerHp - dmg4);
                Narrator.Say($"T'as cherché dans un endroit que t'aurais mieux fait d'éviter. -{dmg4} PV joueur, toujours à sec.", Color.Red);
                break;
        }
        Narrator.Pause();
    }

    static void ResolveCasino(GameState state)
    {
        Narrator.Say("Les lumières du casino clignotent. Samy Scotty surveille tout depuis son balcon. Son regard te suit.", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Quel jeu ?",
        [
            new("Machine à jackpot (mise libre)", s => ResolveSlotsGame(s)),
            new("Dés clandestins — haute mise", s => ResolveDiceGame(s), s => s.Credits >= 300),
            new("Cartes avec Samy Scotty en personne", s => ResolveCardGame(s), s => s.Credits >= 1000),
            new("Tricher — si t'as le culot", s => ResolveCheating(s)),
            new("← Quitter le casino", _ => { }),
        ], Color.Gold1), state);
    }

    static void ResolveSlotsGame(GameState state)
    {
        ChoiceMenu.Resolve(new Situation("La machine attend ta mise.",
        [
            new("Mise légère (100cr)",  s => ResolveBet(s, 100,  2.2), s => s.Credits >= 100),
            new("Mise moyenne (500cr)", s => ResolveBet(s, 500,  2.8), s => s.Credits >= 500),
            new("Mise haute (1500cr)",  s => ResolveBet(s, 1500, 3.5), s => s.Credits >= 1500),
            new("Mise folle (5000cr)",  s => ResolveBet(s, 5000, 5.0), s => s.Credits >= 5000),
            new("← Changer de jeu",    _ => { }),
        ], Color.Gold1), state);
    }

    static void ResolveDiceGame(GameState state)
    {
        // Jeu de dés : tu paries sur pair/impair puis sur un chiffre exact
        var mise = Math.Min(state.Credits, Rng.Next(300, 1500));
        Narrator.Say($"Un jet de dés. Mise automatique : {mise}cr. Pair ou impair ?", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Pair ou impair ?",
        [
            new("Pair", s =>
            {
                s.Credits -= mise;
                var resultat = Rng.Next(1, 13);
                Narrator.Say($"Le dé donne {resultat}.", Color.Gold1);
                if (resultat % 2 == 0) { var gain = (int)(mise * 1.9); s.Credits += gain; Display.ShowEvent($"Pair ! +{gain}cr.", Color.Gold1); }
                else Display.ShowEvent($"Impair. Perdu. -{mise}cr.", Color.Red);
                if (Rng.Next(100) < 15) ResolveBonusRound(s, mise);
                Narrator.Pause();
            }, s => s.Credits >= mise),
            new("Impair", s =>
            {
                s.Credits -= mise;
                var resultat = Rng.Next(1, 13);
                Narrator.Say($"Le dé donne {resultat}.", Color.Gold1);
                if (resultat % 2 != 0) { var gain = (int)(mise * 1.9); s.Credits += gain; Display.ShowEvent($"Impair ! +{gain}cr.", Color.Gold1); }
                else Display.ShowEvent($"Pair. Perdu. -{mise}cr.", Color.Red);
                if (Rng.Next(100) < 15) ResolveBonusRound(s, mise);
                Narrator.Pause();
            }, s => s.Credits >= mise),
            new("← Passer", _ => { }),
        ], Color.Gold1), state);
    }

    static void ResolveBonusRound(GameState state, int mise)
    {
        Narrator.Say("Bonus ! Le croupier offre un second jet gratuit.", Color.Gold1);
        if (Rng.Next(2) == 0) { var bonus = (int)(mise * 1.5); state.Credits += bonus; Display.ShowEvent($"BONUS VALIDÉ ! +{bonus}cr.", Color.Gold1); }
        else Display.ShowEvent("Bonus raté. La maison reprend.", Color.Red);
    }

    static void ResolveCardGame(GameState state)
    {
        // Partie de cartes contre Samy Scotty en personne
        state.NpcsMet.Add("Samy Scotty");
        Narrator.Say("Samy Scotty descend de son balcon. Il sourit. C'est jamais bon signe quand il sourit.", Color.Gold1);

        var mise = Rng.Next(1000, 4000);
        Narrator.Say($"Il pose {mise}cr sur la table. 'Tu suis ou tu regardes ?'", Color.Gold1);

        ChoiceMenu.Resolve(new Situation($"Samy Scotty mise {mise}cr. Tu fais quoi ?",
        [
            new("Suivre la mise", s =>
            {
                if (s.Credits < mise) { Narrator.Say("T'as pas la mise.", Color.Red); Narrator.Pause(); return; }
                s.Credits -= mise;
                var outcome = Rng.Next(100);
                if (outcome < 30) // Victoire
                {
                    var gain = (int)(mise * 2.5);
                    s.Credits += gain; s.Reputation += 20;
                    Narrator.Say($"Tu bats Samy Scotty. Il applaudit. +{gain}cr, +20 réputation. 'Encore une fois ?'", Color.Gold1);
                }
                else if (outcome < 60) // Défaite normale
                    Narrator.Say($"Il gagne. -{mise}cr. Il hoche la tête. 'T'es pas mauvais.'", Color.Red);
                else // Défaite et Scotty se fout de ta gueule en public
                {
                    s.Reputation -= 15;
                    Narrator.Say($"Il t'écrase. -{mise}cr, -15 réputation. Il raconte l'histoire à toute la salle.", Color.Red);
                }
                Narrator.Pause();
            }, s => s.Credits >= mise),
            new("Refuser la mise — regarder jouer", s =>
            {
                Narrator.Say("Il hausse les épaules. 'La prochaine fois.' Il repart sans te regarder.", Color.Grey);
                Narrator.Pause();
            }),
            new("Lui proposer une mise en marchandises", s =>
            {
                if (!s.Cargo.All.Any()) { Narrator.Say("T'as rien à proposer.", Color.Grey); Narrator.Pause(); return; }
                var item = s.Cargo.All.Keys.First();
                if (Rng.Next(100) < 45)
                {
                    s.Cargo.Remove(item, 1);
                    var cr = Rng.Next(800, 2500); s.Credits += cr; s.Reputation += 10;
                    Narrator.Say($"Il accepte. Il gagne votre {item}. Tu récupères {cr}cr en compensation. +10 rép.", Color.Cyan1);
                }
                else
                {
                    var cr = Rng.Next(800, 2500); s.Credits += cr;
                    s.Reputation += 15;
                    Narrator.Say($"Tu gagnes le {item} de Scotty. +{cr}cr estimés. +15 rép.", Color.Gold1);
                }
                Narrator.Pause();
            }, s => s.Cargo.All.Any()),
        ], Color.Gold1), state);
    }

    static void ResolveCheating(GameState state)
    {
        Narrator.Say("Les gardes de Scotty te regardent. Les caméras aussi. T'as pas l'air d'un tricheur professionnel.", Color.Yellow);
        ChoiceMenu.Resolve(new Situation("Comment tu triches ?",
        [
            new("Cartes marquées — subtil", s =>
            {
                if (Rng.Next(100) < 55) { var gain = Rng.Next(1000, 3500); s.Credits += gain; Narrator.Say($"Ça passe. +{gain}cr.", Color.Gold1); }
                else { s.Reputation -= 40; Narrator.Say("Détecté. Samy Scotty descend lui-même. Les gardes t'escortent vers la sortie de façon très physique. -40 rép.", Color.Red); var hp = Rng.Next(25, 55); s.PlayerHp = Math.Max(1, s.PlayerHp - hp); Display.ShowEvent($"-{hp} PV.", Color.Red); }
                Narrator.Pause();
            }),
            new("Interférence électronique — hacker les machines", s =>
            {
                if (s.Class.Name == "Hackeur" || Rng.Next(100) < 40)
                {
                    var gain = Rng.Next(2000, 6000); s.Credits += gain;
                    Narrator.Say($"Les machines débloquent. +{gain}cr avant que les alarmes se déclenchent.", Color.Gold1);
                }
                else { s.Reputation -= 50; s.IsImprisoned = true; Narrator.Say("Le système de sécurité t'a tracé. -50 rép. Cellule.", Color.Red); }
                Narrator.Pause();
            }),
            new("Voler les crédits d'une autre table — direct", s =>
            {
                if (Rng.Next(100) < 30) { var gain = Rng.Next(500, 1500); s.Credits += gain; Narrator.Say($"+{gain}cr volés. Tu files avant que quelqu'un réagisse.", Color.OrangeRed1); }
                else { s.Reputation -= 30; Narrator.Say("Quelqu'un crie. Les gardes arrivent.", Color.Red); Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)])); return; }
                Narrator.Pause();
            }),
            new("← Finalement non", _ => { Narrator.Pause(); }),
        ], Color.Gold1), state);
    }

    static void ResolveBet(GameState state, int bet, double multiplier)
    {
        state.Credits = Math.Max(0, state.Credits - bet);
        var roll = Rng.Next(100);
        if (roll < 38)       { var gain = (int)(bet * multiplier); state.Credits += gain; Display.ShowEvent($"Jackpot ! +{gain}cr !", Color.Gold1); }
        else if (roll < 55)  { Display.ShowEvent($"Presque. La machine garde tout. -{bet}cr.", Color.Red); }
        else if (roll < 65)  { var partial = bet / 2; state.Credits += partial; Display.ShowEvent($"Remboursé à moitié. -{bet / 2}cr net.", Color.Yellow); }
        else                 { Display.ShowEvent($"Rien. -{bet}cr. La maison gagne toujours.", Color.Red); }
        Narrator.Pause();
    }

    static void ResolveParty(GameState state)
    {
        Narrator.Say("La musique résonne dans toute la station. Mister Eliotis accueille chacun avec un verre...", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Star Quest. Un seul but : la fête.", new List<Choice>
        {
            new("Se laisser aller à la fête", s =>
            {
                var cost = Rng.Next(100, 700);
                s.Credits = Math.Max(0, s.Credits - cost);
                Display.ShowEvent($"La fête bat son plein. -{cost}cr dépensés.", Color.Gold1);
                switch (Rng.Next(4))
                {
                    case 0: s.Reputation += 20; Display.ShowEvent("Tu rencontres quelqu'un d'important. +20 réputation.", Color.Green); break;
                    case 1: var b = Rng.Next(200, 600); s.Credits += b; Display.ShowEvent($"Un inconnu règle une partie de ta note. +{b}cr.", Color.Green); break;
                    case 2: Display.ShowEvent("Bonne soirée. Rien de plus.", Color.Grey); break;
                    case 3: var extra = Rng.Next(100, 400); s.Credits = Math.Max(0, s.Credits - extra); Display.ShowEvent($"La soirée a dégénéré. -{extra}cr supplémentaires.", Color.Red); break;
                }
                Narrator.Pause();
            }),
            new("Troubler la fête d'Eliotis", s =>
            {
                Narrator.Say("Tu renverses une table, tu coupes la musique. Eliotis ne pardonne pas qu'on trouble sa fête. Son garde du corps s'avance.", Color.Red);
                ApplyCombatOutcome(s, Combat.Start(s, Combat.TierBoss.First(b => b.Name == "Garde du Corps d'Eliotis")));
            }),
            new("Repartir", _ => { Narrator.Pause(); }),
        }, Color.Gold1), state);
    }

    static void ResolveAlanossa(GameState state)
    {
        state.NpcsMet.Add("Alanossa");
        var reaction = NpcTracker.ShowGreeting(state, "alanossa", "Alanossa", "Arc Ouest Apocalypse");
        if (reaction == NpcReaction.Hostile)
        {
            Narrator.Say("Elle te voit entrer. Ses hommes se lèvent. 'Je t'avais dit de ne plus revenir.' Le combat est inévitable.", Color.Red);
            Situations.ApplyCombatOutcome(state, Combat.Start(state, Combat.TierBoss.First(b => b.Name == "Alanossa")));
            return;
        }
        ChoiceMenu.Resolve(new Situation("Alanossa, le pirate le plus dangereux de l'univers connu.", new List<Choice>
        {
            new("Tenter de traiter avec lui", s =>
            {
                switch (Rng.Next(4))
                {
                    case 0: Display.ShowEvent("Alanossa n'est pas d'humeur. Son garde te pousse dehors.", Color.Red); break;
                    case 1: var d = Rng.Next(500, 1400); s.Credits += d; s.Reputation -= 20; Display.ShowEvent($"Alanossa te propose un deal. +{d}cr, -20 réputation.", Color.OrangeRed1); break;
                    case 2: s.Reputation += 15; Display.ShowEvent("'T'as de la gueule.' Il te laisse passer. +15 réputation.", Color.Cyan1); break;
                    case 3: var loot = Rng.Next(800, 2000); s.Credits += loot; s.Reputation -= 10; Display.ShowEvent($"Alanossa t'offre une part d'un butin récent. +{loot}cr.", Color.Gold1); break;
                }
                Narrator.Pause();
            }),
            new("[gold1]Parler de la Station Nexus — arc principal[/]",
                s => { StationNexus.ResolveFragmentAlanossa(s); StationNexus.CheckNexusComplete(s); }),
            new("Le défier en combat", s =>
            {
                Narrator.Say("Le silence tombe dans le repaire. Alanossa se lève lentement. 'On va bien rigoler.'", Color.Red);
                ApplyCombatOutcome(s, Combat.Start(s, Combat.TierBoss.First(b => b.Name == "Alanossa")));
            }),
            new("Partir", _ => { Narrator.Pause(); }),
        }, Color.Red), state);
    }

    static void ResolveBlackMarket(GameState state)
    {
        if (!state.Cargo.All.Any()) { Display.ShowEvent("T'as rien à vendre.", Color.Grey); Narrator.Pause(); return; }
        Narrator.Say("Tu trouves un receleur dans l'ombre...");
        var item  = state.Cargo.All.Keys.First();
        var price = (int)(Market.GetPrice(item) * Rng.Next(120, 210) / 100.0);
        state.Cargo.Remove(item, 1);
        state.Credits += price;
        state.Reputation -= 8;
        // Vendre au marché noir = monter dans l'estime des Faucons, descendre chez les Gardiens
        FactionSystem.AddStanding(state, FactionId.Faucons, 15);
        Display.ShowEvent($"Vendu {item} au marché noir pour {price}cr. [grey](+15 standing Faucons)[/]", Color.Gold1);
        Narrator.Pause();
    }

    static void ResolveRaphazarus(GameState state)
    {
        state.NpcsMet.Add("Raphazarus");
        var reaction = NpcTracker.ShowGreeting(state, "raphazarus", "Raphazarus", "L'Arc Perdu");
        if (reaction == NpcReaction.Ally)
            Narrator.Say("Il est là. Il attendait. Vous avez maintenant une histoire commune.", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Raphazarus est là. Que fais-tu ?",
        [
            new("Interaction aléatoire — voir ce qu'il veut", s =>
            {
                switch (Rng.Next(5))
                {
                    case 0: var cr = Rng.Next(1000, 4000); s.Credits += cr; Display.ShowEvent($"Raphazarus pose un coffre à tes pieds et repart sans un mot. +{cr}cr.", Color.Gold1); break;
                    case 1: var dmg = Rng.Next(20, 50); s.ShipHp = Math.Max(1, s.ShipHp - dmg); Display.ShowEvent($"Raphazarus pose quelque chose qui explose. -{dmg} PV vaisseau.", Color.Red); break;
                    case 2: s.Cargo.Add("Artefacts", 2); Display.ShowEvent("Il te remet deux artefacts. 'Tu en auras besoin.' Il disparaît.", Color.Cyan1); break;
                    case 3: s.Reputation += 50; Display.ShowEvent("Raphazarus prononce ton nom dans l'ombre. Ta réputation vient de changer partout. +50.", Color.Gold1); break;
                    case 4: Display.ShowEvent("Raphazarus t'observe longuement puis repart. Il ne dit rien. Tu te sens jugé.", Color.Grey); break;
                }
                Narrator.Pause();
            }),
            new("[gold1]Parler de la Station Nexus — arc principal[/]",
                s => { StationNexus.ResolveFragmentRaphazarus(s); StationNexus.CheckNexusComplete(s); }),
            new("[magenta1]Parler des symboles sur les murs[/]  [grey dim](arc Prophète du Vide)[/]",
                s =>
                {
                    if (!s.UnlockedSecrets.Contains("symboles_raphazarus"))
                    { Narrator.Say("Il te regarde fixement. 'Tu n'as pas encore vu les symboles.' Il repart.", Color.Grey); Narrator.Pause(); return; }
                    NpcTracker.RecordMeeting(s, "raphazarus", "Raphazarus", "L'Arc Perdu", 10, "parle_symboles");
                    Narrator.Say("Ses yeux changent légèrement. 'Tu les as trouvés. Et tu es revenu.' Il attend la suite.", Color.Magenta1);
                    Narrator.Pause();
                }, s => s.UnlockedSecrets.Contains("symboles_raphazarus")),
            new("Partir", _ => { Narrator.Pause(); }),
        ], Color.Grey), state);
    }

    static void ResolveRamaster(GameState state)
    {
        state.NpcsMet.Add("Ramaster");
        Narrator.Say("Ramaster t'accueille dans son atelier en jetant à peine un regard...", Color.SteelBlue1);
        ChoiceMenu.Resolve(new Situation("Que veux-tu ?", new List<Choice>
        {
            new("Réparer le vaisseau (500cr/20PV)", s =>
            {
                if (s.Credits < 500) { Display.ShowEvent("Pas assez de crédits.", Color.Red); return; }
                s.Credits -= 500; s.ShipHp = Math.Min(s.ShipMaxHp, s.ShipHp + 20);
                Display.ShowEvent("Ramaster répare. 'Propre.' +20 PV vaisseau.", Color.Green);
                Narrator.Pause();
            }, s => s.ShipHp < s.ShipMaxHp),

            new("Modification expérimentale (1000cr)", s =>
            {
                if (s.Credits < 1000) { Display.ShowEvent("Pas assez de crédits.", Color.Red); return; }
                s.Credits -= 1000;
                var ok = Rng.Next(2) == 0;
                if (ok) { s.ShipMaxHp += 20; s.ShipHp = Math.Min(s.ShipMaxHp, s.ShipHp + 20); Display.ShowEvent("Ça marche ! +20 PV max vaisseau permanent.", Color.Gold1); }
                else { s.ShipHp = Math.Max(1, s.ShipHp - 30); Display.ShowEvent("Ça explose pendant la démo. -30 PV vaisseau. Ramaster en rit.", Color.Red); }
                Narrator.Pause();
            }),

            new("[gold1]Parler de la Station Nexus — arc principal[/]",
                s => { StationNexus.ResolveFragmentRamaster(s); StationNexus.CheckNexusComplete(s); }),

            new("← Partir", _ => { }),
        }), state);
    }

    static void ResolveCustoms(GameState state)
    {
        Narrator.Say("Les douaniers de l'Emporium t'examinent avec méfiance...");
        var bribe = Math.Abs(state.Reputation) * 12;
        if (state.Credits >= bribe)
        {
            state.Credits -= bribe;
            state.Reputation += 25;
            Display.ShowEvent($"Tu graisses la patte. -{bribe}cr, +25 réputation.", Color.Yellow);
        }
        else
        {
            Display.ShowEvent("T'as pas assez. Ils notent ton passage avec intérêt.", Color.Red);
            state.Reputation -= 15;
        }
        Narrator.Pause();
    }

    static void ResolveSanctuary(GameState state)
    {
        // Valkara se souvient du joueur selon sa réputation
        var intro = state.Reputation switch
        {
            >= 500  => "Sœur Valkara te reconnaît dès l'entrée. 'La Légende. Nous sommes honorés.' Elle t'accueille sans attendre.",
            >= 100  => "Sœur Valkara hoche la tête en te voyant. 'Tu es connu ici. Approche.'",
            >= 0    => "Sœur Valkara t'examine en silence...",
            >= -500 => "Sœur Valkara te scrute avec méfiance. 'Tes actes nous sont parvenus. Nous soignons quand même. C'est notre vœu.'",
            _       => "Sœur Valkara te dévisage froidement. 'Un ennemi public dans notre Sanctuaire. Tu as de l'audace. On te soigne — mais pas de faveurs.'"
        };
        Narrator.Say(intro, Color.Green);
        // Standing Gardiens si réputé positif
        if (state.Reputation >= 100) FactionSystem.AddStanding(state, FactionId.Gardiens, 10);

        var choices = new List<Choice>();
        var healCost = 200;
        choices.Add(new($"Soins complets ({healCost}cr)", s =>
        {
            if (s.Credits < healCost) { Display.ShowEvent("Pas les moyens.", Color.Red); Narrator.Pause(); return; }
            s.Credits -= healCost;
            s.PlayerHp = s.PlayerMaxHp;
            s.ShipHp   = Math.Min(s.ShipMaxHp, s.ShipHp + 30);
            Display.ShowEvent($"PV joueur au max, +30 PV vaisseau. -{healCost}cr.", Color.Green);
            Narrator.Pause();
        }, s => s.Credits >= healCost));

        if (state.AddictionLevel > 0)
        {
            var detoxCost = state.AddictionLevel * 400;
            choices.Add(new($"Cure de désintoxication ({detoxCost}cr — niveau {state.AddictionLevel})", s =>
            {
                if (s.Credits < detoxCost) { Display.ShowEvent($"Il faut {detoxCost}cr pour cette cure.", Color.Red); Narrator.Pause(); return; }
                s.Credits -= detoxCost;
                var reduced = Math.Max(0, s.AddictionLevel - Rng.Next(2, 4));
                s.AddictionLevel = reduced;
                s.AddictionDaysSinceDose = 0;
                if (reduced == 0)
                    Narrator.Say("Valkara t'accompagne pendant plusieurs jours. Tu ressors propre. L'envie est encore là, mais tu peux la gérer.", Color.Green);
                else
                    Narrator.Say($"La cure réduit l'addiction. Niveau restant : {reduced}. Continue les soins pour t'en sortir complètement.", Color.Yellow);
                Narrator.Pause();
            }, s => s.Credits >= detoxCost));
        }

        choices.Add(new("Partir", _ => { Narrator.Pause(); }));
        ChoiceMenu.Resolve(new Situation("Que fais-tu au Sanctuaire ?", choices, Color.Green), state);
    }

    static void ResolveFlinch(GameState state)
    {
        Narrator.Say("Le Docteur Flinch frotte ses mains avec un enthousiasme inquiétant...", Color.Yellow);
        ChoiceMenu.Resolve(new Situation("Que propose-t-il ?", new List<Choice>
        {
            new("Amélioration des réflexes (500cr — 50/50)", s =>
            {
                if (s.Credits < 500) { Display.ShowEvent("Pas assez de crédits.", Color.Red); return; }
                s.Credits -= 500;
                if (Rng.Next(2) == 0) { s.PlayerMaxHp += 15; s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 15); Display.ShowEvent("+15 PV joueur permanent !", Color.Gold1); }
                else { s.PlayerHp = Math.Max(1, s.PlayerHp - 20); Display.ShowEvent("Réaction allergique. -20 PV joueur.", Color.Red); }
                Narrator.Pause();
            }, s => s.Credits >= 500),

            new("Injection de carburant métabolique (300cr)", s =>
            {
                if (s.Credits < 300) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= 300;
                s.Reputation += Rng.Next(-10, 25);
                Display.ShowEvent("L'effet est... difficile à décrire. Réputation modifiée aléatoirement.", Color.Gold1);
                Narrator.Pause();
            }, s => s.Credits >= 300),

            new("← Non merci", _ => { }),
        }), state);
    }

    static void ResolveVoss(GameState state)
    {
        Narrator.Say("Le Commandant Voss te reçoit debout, les mains dans le dos...", Color.SteelBlue1);
        var pay = Rng.Next(600, 1500);
        ChoiceMenu.Resolve(new Situation($"Voss propose une mission de combat. Paye : {pay}cr.", new List<Choice>
        {
            new("Accepter", s =>
            {
                var ok = Rng.Next(100) < 50 + Math.Max(0, s.Reputation / 10);
                if (ok) { s.Credits += pay; s.Reputation += 20; Display.ShowEvent($"Mission réussie. +{pay}cr, +20 réputation.", Color.Green); }
                else { var d = Rng.Next(20, 50); s.ShipHp = Math.Max(1, s.ShipHp - d); Display.ShowEvent($"Mission échouée. -{d} PV vaisseau.", Color.Red); }
                Narrator.Pause();
            }),
            new("Refuser", _ => { Display.ShowEvent("Voss hoche la tête. 'Une autre fois peut-être.'", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveFakeId(GameState state)
    {
        Narrator.Say("La Silhouette glisse une enveloppe sur la table sans te regarder...");
        state.Credits -= 800;
        state.Reputation = Math.Min(state.Reputation + 150, 0); // Efface jusqu'à 0, jamais en positif via ça
        Display.ShowEvent("Faux certificat acheté. -800cr. Ta réputation négative est partiellement effacée.", Color.Cyan1);
        Narrator.Pause();
    }

    static void ResolveAncien(GameState state)
    {
        Narrator.Say("L'Ancien te regarde longuement avant de parler. Ses yeux ont vu la destruction de toute une civilisation...", Color.Grey);
        switch (Rng.Next(3))
        {
            case 0: Display.ShowEvent("'La grande station n'a pas été détruite par accident. Quelqu'un l'a voulu.' Il se tait.", Color.Grey); break;
            case 1: state.Cargo.Add("Artefacts", 1); Display.ShowEvent("Il te donne un artefact de la cité ancienne. 'Garde-le.'", Color.Gold1); break;
            case 2: state.Reputation += 30; Display.ShowEvent("Il te bénit au nom de la cité perdue. +30 réputation.", Color.Green); break;
        }
        Narrator.Pause();
    }

    static void ResolveMara(GameState state)
    {
        Narrator.Say("Petite Mara t'observe avec des yeux bien plus vieux que son âge...");
        if (state.Cargo.Get("Rations") >= 2)
        {
            state.Cargo.Remove("Rations", 2);
            var cr = Rng.Next(500, 1500);
            state.Credits += cr;
            Display.ShowEvent($"Tu lui donnes des Rations. Elle te donne des coordonnées d'une épave. +{cr}cr.", Color.Gold1);
        }
        else
        {
            Display.ShowEvent("'T'as pas de Rations ? Alors j'ai rien pour toi.' Elle retourne à ses stocks.", Color.Grey);
        }
        Narrator.Pause();
    }

    static void ResolveColony(GameState state)
    {
        Narrator.Say("Mère Thessa t'observe depuis le seuil de la ferme...", Color.Green);
        ChoiceMenu.Resolve(new Situation("La colonie a besoin d'aide.", new List<Choice>
        {
            new("Donner des Médicaments", s =>
            {
                if (s.Cargo.Get("Médicaments") == 0) { Display.ShowEvent("T'as pas de Médicaments.", Color.Red); return; }
                s.Cargo.Remove("Médicaments", 1); s.Reputation += 35;
                Display.ShowEvent("Mère Thessa te remercie. +35 réputation.", Color.Green);
                Narrator.Pause();
            }, s => s.Cargo.Get("Médicaments") > 0),

            new("Donner des Vivres", s =>
            {
                if (s.Cargo.Get("Vivres") == 0) { Display.ShowEvent("T'as pas de Vivres.", Color.Red); return; }
                s.Cargo.Remove("Vivres", 1); s.Reputation += 25;
                Display.ShowEvent("+25 réputation. La colonie se souvient.", Color.Green);
                Narrator.Pause();
            }, s => s.Cargo.Get("Vivres") > 0),

            new("Partir sans aider", _ => { Display.ShowEvent("Thessa te regarde partir sans rien dire.", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveBaruk(GameState state)
    {
        Narrator.Say("Baruk t'accueille comme si il t'attendait depuis longtemps...", Color.OrangeRed1);
        if (state.Reputation >= -100) { Display.ShowEvent("'T'as pas assez de casier pour qu'on puisse faire affaire.' Il te tourne le dos.", Color.Grey); Narrator.Pause(); return; }
        var cost = Rng.Next(800, 2000);
        ChoiceMenu.Resolve(new Situation($"Baruk peut effacer un avis de recherche. Prix : {cost}cr.", new List<Choice>
        {
            new($"Payer ({cost}cr)", s =>
            {
                if (s.Credits < cost) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= cost; s.Reputation = Math.Min(s.Reputation + 200, -50);
                Display.ShowEvent("L'avis de recherche disparaît des registres.", Color.Green);
                Narrator.Pause();
            }, s => s.Credits >= cost),
            new("Refuser", _ => { Narrator.Pause(); }),
        }), state);
    }

    static void ResolveAldara(GameState state)
    {
        Narrator.Say("La Directrice Aldara te reçoit dans son bureau au sommet de la station...", Color.SteelBlue1);
        var contract = Rng.Next(2000, 6000);
        ChoiceMenu.Resolve(new Situation("Aldara propose un contrat exclusif.", new List<Choice>
        {
            new($"Accepter le contrat ({contract}cr)", s =>
            {
                var ok = Rng.Next(100) < 65;
                if (ok) { s.Credits += contract; s.Reputation += 30; Display.ShowEvent($"+{contract}cr, +30 réputation.", Color.Gold1); }
                else { s.Reputation -= 20; Display.ShowEvent("Contrat raté. -20 réputation.", Color.Red); }
                Narrator.Pause();
            }),
            new("[gold1]Parler de la Station Nexus — arc principal[/]",
                s => { StationNexus.ResolveFragmentAldara(s); StationNexus.CheckNexusComplete(s); }),
            new("Refuser poliment", _ => { Display.ShowEvent("'Une autre fois.' Elle note quelque chose.", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveGhostShip(GameState state)
    {
        Narrator.Say("Le vaisseau fantôme est silencieux. Les lumières clignotent. L'air est froid...", Color.Grey);
        var roll = Rng.Next(4);
        switch (roll)
        {
            case 0: var cr = Rng.Next(2000, 5000); state.Credits += cr; Display.ShowEvent($"Cargaison intacte. +{cr}cr.", Color.Gold1); break;
            case 1: var d = Rng.Next(30, 60); state.ShipHp = Math.Max(1, state.ShipHp - d); Display.ShowEvent($"Quelque chose t'attaque dans le noir. -{d} PV vaisseau.", Color.Red); break;
            case 2: state.Cargo.Add("Artefacts", 2); Display.ShowEvent("Deux artefacts anciens trouvés dans la salle des capitaines.", Color.Cyan1); break;
            case 3: Display.ShowEvent("Rien. Le vaisseau est vide. Complètement vide. Ça n'a aucun sens.", Color.Grey); break;
        }
        Narrator.Pause();
    }

    static void ResolveJudgement(GameState state)
    {
        Narrator.Say("Une silhouette en robe t'attend. 'Nous avons suivi ta route.'", Color.Grey);
        var score = state.Credits / 100 + state.Reputation + state.Day * 2;
        if (score > 500) { var reward = Rng.Next(3000, 8000); state.Credits += reward; Display.ShowEvent($"'Tu as bien vécu.' +{reward}cr.", Color.Gold1); }
        else if (score > 0) { var small = Rng.Next(500, 1500); state.Credits += small; Display.ShowEvent($"'Tu survives. C'est déjà quelque chose.' +{small}cr.", Color.Yellow); }
        else { var loss = Rng.Next(500, 2000); state.Credits = Math.Max(0, state.Credits - loss); Display.ShowEvent($"'Tu as gâché ce qu'on t'avait donné.' -{loss}cr.", Color.Red); }
        Narrator.Pause();
    }

    static void ResolveMarris(GameState state)
    {
        Narrator.Say("Marris est en train de parler à une plante. Il te remarque enfin.", Color.Grey);
        var items = new[] { "Or", "Artefacts", "Armes", "Pièces détachées", "Objets expérimentaux", "Rations", "Eau" };
        var item  = items[Rng.Next(items.Length)];
        var price = (int)(Market.GetPrice(item) * Rng.Next(50, 150) / 100.0);
        Display.ShowEvent($"Marris propose : {item} pour {price}cr. Il ne sait pas ce que c'est.", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Que fais-tu ?", new List<Choice>
        {
            new($"Acheter ({price}cr)", s =>
            {
                if (s.Credits < price) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= price; s.Cargo.Add(item, 1);
                Display.ShowEvent($"Acheté. Marris retourne parler à sa plante.", Color.Cyan1);
                Narrator.Pause();
            }, s => s.Credits >= price),
            new("Partir", _ => { Narrator.Pause(); }),
        }), state);
    }

    static void ResolveConclave(GameState state)
    {
        Narrator.Say("Une silhouette masquée s'approche. 'Tu veux savoir quelque chose.'", Color.Grey);
        state.Credits -= 1000;
        switch (Rng.Next(4))
        {
            case 0: state.Reputation += 60; Display.ShowEvent("Le secret efface tes erreurs passées aux yeux de nombreuses factions. +60 réputation.", Color.Gold1); break;
            case 1: var cr = Rng.Next(3000, 7000); state.Credits += cr; Display.ShowEvent($"L'information te mène directement à une fortune cachée. +{cr}cr.", Color.Gold1); break;
            case 2: Display.ShowEvent("Le secret te perturbe profondément. Tu sais quelque chose que tu aurais préféré ignorer.", Color.Grey); break;
            case 3: state.Reputation -= 40; Display.ShowEvent("Le Conclave t'a menti. -40 réputation.", Color.Red); break;
        }
        Narrator.Pause();
    }

    static void ResolveEos(GameState state)
    {
        state.NpcsMet.Add("Eos");
        Narrator.Say("Le Président Eos t'accueille avec un sourire qui ne monte pas jusqu'aux yeux...", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Eos propose un deal massif.", new List<Choice>
        {
            new("Investir 10 000cr (x3 ou tout perdre)", s =>
            {
                if (s.Credits < 10_000) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= 10_000;
                if (Rng.Next(2) == 0) { s.Credits += 30_000; Display.ShowEvent("Triple mise ! +30 000cr.", Color.Gold1); }
                else Display.ShowEvent("Eos sourit. 'Les affaires, c'est les affaires.'", Color.Red);
                Narrator.Pause();
            }, s => s.Credits >= 10_000),
            new("Décliner", _ => { Display.ShowEvent("'Prudent. On se reverra.'", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveNoctis(GameState state)
    {
        state.NpcsMet.Add("Directeur Pale");
        Narrator.Say("Les mineurs t'observent avec des yeux sans espoir. Le Directeur Pale surveille tout...", Color.Grey);
        ChoiceMenu.Resolve(new Situation("Que fais-tu ?", new List<Choice>
        {
            new("Libérer des mineurs — attaquer Pale", s =>
            {
                var ok = Rng.Next(100) < 40 + Math.Max(0, s.Reputation / 10);
                if (ok) { s.Reputation += 80; s.Credits += Rng.Next(1000, 3000); Display.ShowEvent("+80 réputation. Les mineurs libérés te donnent ce qu'ils ont. Pale est maintenant ton ennemi.", Color.Gold1); }
                else { s.ShipHp = Math.Max(1, s.ShipHp - 40); Display.ShowEvent("Échec. La sécurité de Pale te repousse. -40 PV vaisseau.", Color.Red); }
                Narrator.Pause();
            }),
            new("Affronter le Directeur Pale en personne", s =>
            {
                Narrator.Say("Tu forces le passage jusqu'à son bureau. Pale repose sa tasse, calme. C'est pire que la colère. Ses gardes se mettent en position.", Color.Red);
                ApplyCombatOutcome(s, Combat.Start(s, Combat.TierBoss.First(b => b.Name == "Directeur Pale")));
            }),
            new("Ignorer et commerce uniquement", _ => { Display.ShowEvent("Tu fais tes affaires en regardant ailleurs.", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveZenn(GameState state)
    {
        state.NpcsMet.Add("Zenn");
        Narrator.Say("L'Archiviste Zenn te regarde par-dessus ses lunettes...", Color.SteelBlue1);
        if (state.Cargo.Get("Artefacts") >= 3)
        {
            for (int i = 0; i < 3; i++) state.Cargo.Remove("Artefacts", 1);
            state.Reputation += 40;
            Display.ShowEvent("Zenn accepte les artefacts. 'L'Arc Perdu. Souviens-toi de ces coordonnées.' +40 réputation.", Color.Gold1);
        }
        else
        {
            var info = Rng.Next(500, 1200);
            if (state.Credits >= info) { state.Credits -= info; Display.ShowEvent($"Zenn vend une information sur les routes. -{info}cr.", Color.Cyan1); }
            else Display.ShowEvent("'Reviens avec 3 Artefacts ou des crédits.'", Color.Grey);
        }
        Narrator.Pause();
    }

    static void ResolveFaucon(GameState state)
    {
        state.NpcsMet.Add("La Faucon");
        Narrator.Say("La Faucon t'observe entrer dans son repaire sans bouger...", Color.Red);
        var choices = new List<Choice>
        {
            new("Accepter un contrat contre Alanossa", s =>
            {
                var pay = Rng.Next(3000, 7000);
                s.Credits += pay; s.Reputation -= 25;
                Display.ShowEvent($"+{pay}cr. Alanossa est maintenant ton ennemi. -25 réputation.", Color.OrangeRed1);
                Narrator.Pause();
            }),
        };

        if (state.Faction == FactionId.None)
        {
            choices.Add(new("Rejoindre les Faucons Noirs", s =>
            {
                s.Faction = FactionId.Faucons;
                Narrator.Say("La Faucon te tend la main. 'Bienvenue dans les Faucons. On ne pardonne pas les trahisons.' Tu sais ce que ça veut dire.", Color.Red);
                Display.ShowEvent("Tu rejoins les Faucons Noirs. Les pirates en voyage te laissent passer.", Color.Gold1);
                Narrator.Pause();
            }));
        }
        else if (state.Faction == FactionId.Faucons)
        {
            choices.Add(new("Mission Faucons — intercepter un convoi", s =>
            {
                var ok  = Rng.Next(100) < 55;
                var pay = Rng.Next(2000, 5000);
                if (ok) { s.Credits += pay; s.Reputation -= 10; s.FactionMissions++; Display.ShowEvent($"Convoi intercepté. +{pay}cr. -10 réputation.", Color.Gold1); }
                else { s.ShipHp = Math.Max(1, s.ShipHp - 35); Display.ShowEvent("Embuscade. Le convoi était escorté. -35 PV vaisseau.", Color.Red); }
                Narrator.Pause();
            }));
        }

        choices.Add(new("Défier La Faucon en duel — c'est elle ou toi", s =>
        {
            Narrator.Say("'Tu veux ma place ? Viens la prendre.' Elle dégaine avant la fin de sa phrase.", Color.Red);
            ApplyCombatOutcome(s, Combat.Start(s, Combat.TierBoss.First(b => b.Name == "La Faucon")));
        }));

        choices.Add(new("Refuser", _ => { Display.ShowEvent("'Peut-être une autre fois.'", Color.Grey); Narrator.Pause(); }));
        ChoiceMenu.Resolve(new Situation("La Faucon parle.", choices), state);
    }

    static void ResolveARIA(GameState state)
    {
        Narrator.Say("ARIA parle dans ta tête avant même que tu ouvres la bouche.", Color.Cyan1);
        ChoiceMenu.Resolve(new Situation("ARIA propose une fusion partielle avec ton vaisseau.", new List<Choice>
        {
            new("Accepter la fusion", s =>
            {
                s.ShipMaxHp += 30; s.ShipHp = Math.Min(s.ShipMaxHp, s.ShipHp + 30);
                Display.ShowEvent("+30 PV max vaisseau. ARIA est maintenant présente dans tes systèmes.", Color.Cyan1);
                Narrator.Pause();
            }),
            new("Refuser", _ => { Display.ShowEvent("'Intéressant. Un autre jour peut-être.' ARIA disparaît.", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveSorath(GameState state)
    {
        state.NpcsMet.Add("Sorath");
        Narrator.Say("Grand Gardien Sorath te jauge sans dire un mot pendant de longues secondes...", Color.SteelBlue1);
        if (state.Reputation < 0) { Display.ShowEvent("'Tu n'es pas prêt.' Il te tourne le dos.", Color.Red); Narrator.Pause(); return; }

        var choices = new List<Choice>();

        if (state.Reputation >= 1000)
            choices.Add(new("Recevoir la récompense", s => { var r = Rng.Next(5000, 12000); s.Credits += r; s.AddReputation(50); Display.ShowEvent($"'Légende.' +{r}cr, +rep.", Color.Gold1); Narrator.Pause(); }));
        else
            choices.Add(new("Parler à Sorath", s => { var r = Rng.Next(500, 2000); s.Credits += r; Display.ShowEvent($"'Honorable.' +{r}cr.", Color.Green); Narrator.Pause(); }));

        if (state.Faction == FactionId.None && state.Reputation >= 200)
        {
            choices.Add(new("Rejoindre les Gardiens Écarlates", s =>
            {
                s.Faction = FactionId.Gardiens;
                Narrator.Say("Sorath pose sa main sur ton épaule. 'L'Ordre t'accueille. Chaque acte de justice porte maintenant le poids de notre héritage.'", Color.SteelBlue1);
                Display.ShowEvent("Tu rejoins les Gardiens. Gains de réputation +50% sur actes héroïques.", Color.Gold1);
                Narrator.Pause();
            }));
        }
        else if (state.Faction == FactionId.Gardiens)
        {
            choices.Add(new("Mission Gardiens — protéger un convoi civil", s =>
            {
                var ok  = Rng.Next(100) < 60 + Math.Max(0, s.Reputation / 15);
                var pay = Rng.Next(1500, 4000);
                if (ok) { s.Credits += pay; s.AddReputation(40); s.FactionMissions++; Display.ShowEvent($"Convoi protégé. +{pay}cr. +40 réputation.", Color.Green); }
                else { s.ShipHp = Math.Max(1, s.ShipHp - 30); s.AddReputation(-15); Display.ShowEvent("Échec. Pertes civiles. -30 PV vaisseau, -réputation.", Color.Red); }
                Narrator.Pause();
            }));
        }

        choices.Add(new("Partir", _ => { Narrator.Pause(); }));
        ChoiceMenu.Resolve(new Situation("Sorath t'observe.", choices, Color.SteelBlue1), state);
    }

    static void ResolveVelkor(GameState state)
    {
        Narrator.Say("Le Professeur Velkor te tend un formulaire de consentement avec un crayon qui fuit...", Color.Yellow);
        ChoiceMenu.Resolve(new Situation("Quelle expérience ?", new List<Choice>
        {
            new("Expérience A — amélioration physique (800cr)", s =>
            {
                if (s.Credits < 800) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= 800;
                switch (Rng.Next(3))
                {
                    case 0: s.PlayerMaxHp += 25; Display.ShowEvent("+25 PV joueur permanent !", Color.Gold1); break;
                    case 1: s.PlayerHp = Math.Max(1, s.PlayerHp - 30); Display.ShowEvent("Réaction adverse. -30 PV joueur.", Color.Red); break;
                    case 2: Display.ShowEvent("Rien de visible pour l'instant. Velkor note quelque chose.", Color.Grey); break;
                }
                Narrator.Pause();
            }, s => s.Credits >= 800),
            new("← Sortir en courant", _ => { Display.ShowEvent("Velkor crie 'Reviens !' dans ton dos.", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveAuroreNoire(GameState state)
    {
        Narrator.Say("L'Aurore Noire. Des dizaines de milliers de morts. Tu peux presque les entendre...", Color.Grey);
        switch (Rng.Next(4))
        {
            case 0: state.Cargo.Add("Armes", 1); state.Cargo.Add("Artefacts", 1); Display.ShowEvent("Armurerie intacte. Une arme récupérée. Un artefact.", Color.Gold1); break;
            case 1: var cr = Rng.Next(2000, 5000); state.Credits += cr; Display.ShowEvent($"Coffre-fort du capitaine. +{cr}cr.", Color.Gold1); break;
            case 2: state.Reputation += 30; Display.ShowEvent("Les journaux de bord révèlent un secret sur la grande guerre. +30 réputation.", Color.Cyan1); break;
            case 3: var d = Rng.Next(25, 55); state.ShipHp = Math.Max(1, state.ShipHp - d); Display.ShowEvent($"Le vaisseau n'est pas aussi inerte que prévu. -{d} PV vaisseau.", Color.Red); break;
        }
        Narrator.Pause();
    }

    static void ResolveHamid(GameState state)
    {
        Narrator.Say("Hamid le Chanceux écarte les bras : 'Mon ami, j'attendais quelqu'un comme toi !'", Color.Gold1);
        var item  = Universe.Get(state.CurrentStation).Goods[Rng.Next(Universe.Get(state.CurrentStation).Goods.Count)];
        var price = (int)(Market.GetPrice(item) * Rng.Next(50, 200) / 100.0);
        Display.ShowEvent($"Hamid propose : {item} pour {price}cr. 'Ça porte bonheur, je le jure !'", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Tu achètes ?", new List<Choice>
        {
            new($"Oui ({price}cr)", s =>
            {
                if (s.Credits < price) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= price; s.Cargo.Add(item, 1);
                if (Rng.Next(3) == 0) { s.Reputation += 10; Display.ShowEvent("Ça porte vraiment bonheur. +10 réputation.", Color.Gold1); }
                else Display.ShowEvent("Achat standard. Hamid te fait un clin d'œil.", Color.Grey);
                Narrator.Pause();
            }, s => s.Credits >= price),
            new("Non", _ => { Display.ShowEvent("'Tu reviendras !'", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveYenna(GameState state)
    {
        Narrator.Say("Yenna tape sur plusieurs écrans simultanément sans te regarder...", Color.SteelBlue1);
        ChoiceMenu.Resolve(new Situation("Yenna peut t'aider.", new List<Choice>
        {
            new("Info sur les prix (300cr)", s =>
            {
                if (s.Credits < 300) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= 300;
                var stations = Universe.Stations.Where(st => !st.IsStarting && !st.IsRare).OrderBy(_ => Rng.Next()).Take(2).ToList();
                foreach (var st in stations) Display.ShowEvent($"{st.Name} : {string.Join(", ", st.Goods.Select(g => $"{g} {Market.GetPrice(g)}cr"))}", Color.Cyan1);
                Narrator.Pause();
            }, s => s.Credits >= 300),

            new("Signaler un convoi vulnérable (500cr)", s =>
            {
                if (s.Credits < 500) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= 500;
                var loot = Rng.Next(1000, 3000);
                s.Credits += loot; s.Reputation -= 15;
                Display.ShowEvent($"Tu interceptes le convoi. +{loot}cr. -15 réputation.", Color.OrangeRed1);
                Narrator.Pause();
            }, s => s.Credits >= 500),

            new("← Partir", _ => { Narrator.Pause(); }),
        }), state);
    }

    static void ResolveOssian(GameState state)
    {
        Narrator.Say("Le Général Ossian t'accueille d'une poignée de main qui broie les os...", Color.Red);
        var pay = Rng.Next(1500, 4000);
        ChoiceMenu.Resolve(new Situation($"Ossian propose un contrat militaire. Paye : {pay}cr.", new List<Choice>
        {
            new("Accepter", s =>
            {
                var ok = Rng.Next(100) < 55;
                if (ok) { s.Credits += pay; s.Reputation += 25; Display.ShowEvent($"+{pay}cr, +25 réputation.", Color.Green); }
                else { s.ShipHp = Math.Max(1, s.ShipHp - 45); Display.ShowEvent("Embuscade. -45 PV vaisseau.", Color.Red); }
                Narrator.Pause();
            }),
            new("Refuser", _ => { Display.ShowEvent("'Dommage. La porte est ouverte si tu changes d'avis.'", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveCassen(GameState state)
    {
        Narrator.Say("Lord Cassen contemple la nébuleuse en sirotant quelque chose de probablement très cher...", Color.SteelBlue1);
        var invest = Rng.Next(2000, 5000);
        ChoiceMenu.Resolve(new Situation("Lord Cassen cherche du divertissement.", new List<Choice>
        {
            new($"Accepter de financer une expédition ({invest}cr en jeu)", s =>
            {
                if (s.Credits < invest) { Display.ShowEvent("Pas assez.", Color.Red); return; }
                s.Credits -= invest;
                var mult = Rng.Next(2) == 0 ? Rng.Next(2, 5) : 0;
                if (mult > 0) { var r = invest * mult; s.Credits += r; Display.ShowEvent($"L'expédition rapporte. +{r}cr.", Color.Gold1); }
                else Display.ShowEvent("L'expédition échoue. Cassen hausse les épaules. 'On réessaiera.'", Color.Red);
                Narrator.Pause();
            }, s => s.Credits >= invest),
            new("Décliner poliment", _ => { Display.ShowEvent("'Comme tu voudras.' Il retourne à sa nébuleuse.", Color.Grey); Narrator.Pause(); }),
        }), state);
    }

    static void ResolveGerant(GameState state)
    {
        Narrator.Say("Le Gérant émerge de l'ombre. 'Tu as de l'Or. Bien.'", Color.Grey);
        var orQty = state.Cargo.Get("Or");
        var price = Market.GetPrice("Or") * 2;
        Display.ShowEvent($"Le Gérant achète ton Or au double du prix du marché. {orQty}x Or = {orQty * price}cr.", Color.Gold1);
        ChoiceMenu.Resolve(new Situation("Tu vends tout ton Or ?", new List<Choice>
        {
            new("Oui, tout vendre", s =>
            {
                var qty = s.Cargo.Get("Or");
                for (int i = 0; i < qty; i++) s.Cargo.Remove("Or", 1);
                s.Credits += qty * price;
                Display.ShowEvent($"+{qty * price}cr. Transaction en Or réglée.", Color.Gold1);
                Narrator.Pause();
            }),
            new("Non, garder son Or", _ => { Narrator.Pause(); }),
        }), state);
    }

    // ── NOUVELLES STATIONS ──────────────────────────────────────────────────

    static void ResolveVera(GameState state)
    {
        state.NpcsMet.Add("Vera");
        Narrator.Say("Le Capitaine Vera dirige ce vaisseau depuis vingt ans. Elle fuit quelque chose qu'elle ne nomme jamais.", Color.Grey);
        ChoiceMenu.Resolve(new Situation("Vera propose un contrat.", new List<Choice>
        {
            new("Escorter la colonie jusqu'à la prochaine station", s =>
            {
                var ok  = Rng.Next(100) < 55 + Math.Max(0, s.Reputation / 10);
                var pay = Rng.Next(1500, 4000);
                if (ok)
                {
                    s.Credits += pay; s.AddReputation(35);
                    Narrator.Say($"La colonie arrive à bon port. Vera te serre la main longuement. +{pay}cr, +35 réputation.", Color.Green);
                }
                else
                {
                    s.ShipHp = Math.Max(1, s.ShipHp - Rng.Next(25, 50));
                    Narrator.Say("Une attaque en route. La colonie s'en sort. Ton vaisseau moins bien. -PV vaisseau.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("Demander ce qu'elle fuit", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Elle te regarde longuement. 'Les mêmes gens qui ont détruit la grande station. Ils ne s'arrêtent pas.' Elle ne dit rien de plus.", Color.Grey);
                        s.AddReputation(10);
                        break;
                    case 1:
                        var cr = Rng.Next(800, 2000);
                        s.Credits += cr;
                        Narrator.Say($"Elle te montre des documents. Des coordonnées. Un dépôt de ressources abandonné. +{cr}cr d'exploitation.", Color.Gold1);
                        break;
                    case 2:
                        Narrator.Say("'Certaines questions n'ont pas de bonnes réponses.' Elle retourne à son poste.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),
            new("Partir", _ => { Narrator.Pause(); }),
        }), state);
    }

    static void ResolveSkade(GameState state)
    {
        state.NpcsMet.Add("Skade");
        Narrator.Say("Skade forge sans s'arrêter. Elle lève à peine les yeux. 'T'as de l'argent ou des matériaux ? Sinon, dégâge.'", Color.OrangeRed1);
        ChoiceMenu.Resolve(new Situation("Skade peut te vendre quelque chose.", new List<Choice>
        {
            new("Acheter une arme tier 3+ (1200cr)", s =>
            {
                if (s.Credits < 1200) { Narrator.Say("'Reviens quand t'as les moyens.'", Color.Red); Narrator.Pause(); return; }
                s.Credits -= 1200;
                var weapon = WeaponPool.RollForTier(3);
                s.Weapons.Add(weapon);
                Narrator.Say($"Skade pose {weapon.Name} sur l'enclume sans un mot. T1200cr. C'est du bon travail.", Color.Gold1);
                Narrator.Pause();
            }, s => s.Credits >= 1200),
            new("Vendre de la ferraille (×10 requis)", s =>
            {
                if (s.Cargo.Get("Ferraille") < 10) { Narrator.Say("'T'as pas assez.' Elle replonge dans son travail.", Color.Red); Narrator.Pause(); return; }
                for (int i = 0; i < 10; i++) s.Cargo.Remove("Ferraille", 1);
                var weapon = WeaponPool.RollForTier(2);
                s.Weapons.Add(weapon);
                Narrator.Say($"Skade fond ta ferraille et en fait {weapon.Name}. T2. Correct.", Color.Cyan1);
                Narrator.Pause();
            }, s => s.Cargo.Get("Ferraille") >= 10),
            new("Partir", _ => { Narrator.Pause(); }),
        }), state);
    }

    static void ResolveLibererOuvriers(GameState state)
    {
        Narrator.Say("Les ouvriers travaillent dans des conditions de survie minimale. Ils t'ont vu entrer. Certains te regardent avec espoir.", Color.Grey);
        ChoiceMenu.Resolve(new Situation("Que fais-tu ?", new List<Choice>
        {
            new("Créer une diversion et les aider à fuir", s =>
            {
                var ok = Rng.Next(100) < 40 + Math.Max(0, s.Reputation / 10);
                if (ok)
                {
                    s.AddReputation(80);
                    var cr = Rng.Next(500, 1500);
                    s.Credits += cr;
                    Narrator.Say($"Les ouvriers fuient dans le chaos. Certains te donnent ce qu'ils ont. +{cr}cr. +80 réputation.", Color.Gold1);
                }
                else
                {
                    s.ShipHp = Math.Max(1, s.ShipHp - Rng.Next(30, 60));
                    s.AddReputation(20);
                    Narrator.Say("La sécurité intervient. Tu recules en courant. -PV vaisseau. +20 réputation quand même.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("Ignorer — ce n'est pas ton problème", s =>
            {
                s.AddReputation(-10);
                Narrator.Say("Tu passes. Un ouvrier te regarde partir. Tu n'oublieras pas son regard. -10 réputation.", Color.Grey);
                Narrator.Pause();
            }),
            new("Dénoncer aux responsables pour une récompense", s =>
            {
                var cr = Rng.Next(300, 700);
                s.Credits += cr;
                s.AddReputation(-35);
                Narrator.Say($"Ils te paient. +{cr}cr. -35 réputation. T'as vendu des gens déjà malheureux.", Color.Red);
                Narrator.Pause();
            }),
        }), state);
    }

    static void ResolveVorn(GameState state)
    {
        state.NpcsMet.Add("Vorn");
        Narrator.Say("Vorn vit dans cette station depuis quarante ans. Il sait tout. Tout coûte quelque chose.", Color.SteelBlue1);
        ChoiceMenu.Resolve(new Situation("Que cherches-tu à savoir ?", new List<Choice>
        {
            new("Info sur les convois vulnérables (600cr)", s =>
            {
                if (s.Credits < 600) { Narrator.Say("'T'as pas les moyens de la bonne info.'", Color.Red); Narrator.Pause(); return; }
                s.Credits -= 600;
                var loot = Rng.Next(1200, 3500);
                s.Credits += loot; s.AddReputation(-15);
                Narrator.Say($"L'info était bonne. Tu interceptes le convoi. +{loot}cr. -15 réputation.", Color.Gold1);
                Narrator.Pause();
            }, s => s.Credits >= 600),
            new("Info sur une faction (500cr)", s =>
            {
                if (s.Credits < 500) { Narrator.Say("'Reviens avec des crédits.'", Color.Red); Narrator.Pause(); return; }
                s.Credits -= 500;
                var factionId = (FactionId)Rng.Next(1, 5);
                var (nom, desc, bonus) = Factions.Info[factionId];
                Narrator.Say($"Vorn souffle : '{nom} — {desc} Bonus membres : {bonus}.'", Color.Cyan1);
                Narrator.Pause();
            }, s => s.Credits >= 500),
            new("Rien — juste regarder la vue", s =>
            {
                Narrator.Say("La vue depuis Le Phare est extraordinaire. Dix routes commerciales visibles à l'œil nu.", Color.Grey);
                s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 10);
                Display.ShowEvent("+10 PV joueur. Le calme a ses vertus.", Color.Green);
                Narrator.Pause();
            }),
        }), state);
    }

    static void ResolveSelene(GameState state)
    {
        Narrator.Say("Sélène gère des millions de vies à bord de l'Arche avec un calme déconcertant.", Color.Green);
        ChoiceMenu.Resolve(new Situation("Sélène t'écoute.", new List<Choice>
        {
            new("Proposer des ressources à l'Arche", s =>
            {
                if (!s.Cargo.All.Any()) { Narrator.Say("T'as rien à donner.", Color.Grey); Narrator.Pause(); return; }
                var item = s.Cargo.All.Keys.First();
                s.Cargo.Remove(item, 1);
                s.AddReputation(45);
                var cr = Rng.Next(600, 1800);
                s.Credits += cr;
                Narrator.Say($"Sélène accepte les {item}. L'Arche t'en est reconnaissante. +{cr}cr. +45 réputation.", Color.Green);
                Narrator.Pause();
            }, s => s.Cargo.All.Any()),
            new("Demander à embarquer temporairement", s =>
            {
                var days = Rng.Next(2, 4);
                for (int i = 0; i < days; i++) { s.Day++; Events.ApplyTravelEffects(s); }
                s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 40);
                s.AddReputation(20);
                Narrator.Say($"Tu passes {days} jours dans l'Arche. La vie à bord est paisible. +40 PV joueur, +20 réputation.", Color.Green);
                Narrator.Pause();
            }),
            new("Partir", _ => { Narrator.Pause(); }),
        }), state);
    }

    static void ResolveTerminus(GameState state)
    {
        Narrator.Say("Station Terminus Noir. Le bout du bout. Tous ceux qui sont là ont une raison de pas être ailleurs.", Color.Grey);
        switch (Rng.Next(4))
        {
            case 0:
                var missionnaire = Rng.Next(2000, 5000);
                state.Credits += missionnaire;
                Narrator.Say($"Un fugitif de l'Emporium te paye pour garder le silence sur sa présence. +{missionnaire}cr.", Color.Gold1);
                break;
            case 1:
                var weapon = WeaponPool.RollForTier(4);
                state.Weapons.Add(weapon);
                Narrator.Say($"Quelqu'un qui ne reviendra jamais t'abandonne son arme. {weapon.Name} (T4).", Color.Cyan1);
                break;
            case 2:
                state.AddReputation(-20);
                var vol = Rng.Next(300, 900);
                state.Credits = Math.Max(0, state.Credits - vol);
                Narrator.Say($"T'as été identifié. Quelqu'un avait une vieille dette avec toi. -{vol}cr, -20 réputation.", Color.Red);
                break;
            case 3:
                state.AddReputation(25);
                Narrator.Say("Un inconnu te remet un message chiffré. 'Tu sauras quoi faire avec.' +25 réputation. Mystère complet.", Color.Magenta1);
                break;
        }
        Narrator.Pause();
    }

    static void ResolveLyra(GameState state)
    {
        state.NpcsMet.Add("Lyra");
        Narrator.Say("L'astronome Lyra ne t'accueille pas. Elle te parle avant même que tu ouvres la bouche.", Color.SteelBlue1);
        Narrator.Say("'J'ai vu quelque chose dans les données. Quelque chose qui bouge à la lisière du système connu. Quelque chose de grand.'", Color.SteelBlue1);
        ChoiceMenu.Resolve(new Situation("Que fais-tu de cette information ?", new List<Choice>
        {
            new("L'écouter — demander plus de détails", s =>
            {
                s.AddReputation(15);
                var cr = Rng.Next(1000, 3000);
                s.Credits += cr;
                Narrator.Say($"Les données valent quelque chose à quelqu'un qui sait quoi en faire. +{cr}cr. +15 réputation. Tu sais quelque chose que personne d'autre ne sait encore.", Color.Cyan1);
                Narrator.Pause();
            }),
            new("Proposer de transporter les données ailleurs", s =>
            {
                var ok  = Rng.Next(100) < 60;
                var pay = Rng.Next(2000, 5000);
                if (ok) { s.Credits += pay; s.AddReputation(30); Narrator.Say($"Lyra accepte. +{pay}cr. +30 réputation. Ces données vont changer des choses.", Color.Gold1); }
                else { Narrator.Say("Elle hésite trop longtemps. L'opportunité passe.", Color.Grey); }
                Narrator.Pause();
            }),
            new("Ignorer — ça ne te concerne pas", s =>
            {
                Narrator.Say("'Tout le monde dit ça.' Elle retourne à ses données. Elle a l'air habituée à être ignorée.", Color.Grey);
                Narrator.Pause();
            }),
        }), state);
    }
}
