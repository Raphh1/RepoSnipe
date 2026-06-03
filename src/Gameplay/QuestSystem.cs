using Spectre.Console;

namespace VoidTrader;

enum QuestType { Delivery, Kill, Revenge, Info }

record Quest(
    string    Id,
    string    Title,
    string    Giver,
    string    GiverStation,
    QuestType Type,
    string    Description,
    string?   TargetStation,
    string?   TargetItem,
    int       CreditReward,
    int       RepReward,
    bool      Completed = false
);

static class QuestSystem
{
    private static readonly Random Rng = new();

    static readonly string[] GiverNames =
    [
        "Marek","Sela","Donn","Yara","Pistis","Boro","Cael","Neva","Torvak","Lira",
        "Besh","Rook","Ysla","Ganz","Myrra","Sten","Orva","Kael","Fen","Drela",
        "Vance","Wyll","Soru","Thas","Nori","Brixa","Cador","Eska","Ulmo","Pheth",
    ];

    // ── GÉNÉRATION ───────────────────────────────────────────────────────────

    public static Quest? Generate(GameState state)
    {
        var accessible = Universe.AccessibleFrom(state.CurrentStation, state)
            .Where(s => s.Name != state.CurrentStation)
            .ToList();
        if (!accessible.Any()) return null;

        var target = accessible[Rng.Next(accessible.Count)];
        var giver  = GiverNames[Rng.Next(GiverNames.Length)];
        var id     = Guid.NewGuid().ToString()[..6];

        return ((QuestType)Rng.Next(4)) switch
        {
            QuestType.Delivery => Delivery(id, giver, state, target),
            QuestType.Kill     => Kill(id, giver, state, target),
            QuestType.Revenge  => Revenge(id, giver, state, target),
            _                  => Info(id, giver, state, target),
        };
    }

    static Quest Delivery(string id, string giver, GameState state, Station target)
    {
        var items = new[] { "Médicaments", "Pièces techniques", "Vivres", "Or",
                            "Artefacts", "Ferraille", "Armes", "Plantes médicinales" };
        var item   = items[Rng.Next(items.Length)];
        var reward = Rng.Next(800, 3500);
        return new Quest(id,
            $"Livraison pour {giver}",
            giver, state.CurrentStation,
            QuestType.Delivery,
            $"Porter 1x [cyan1]{item}[/] à [white]{giver}[/] sur [steelblue1]{target.Name}[/]. Discret de préférence.",
            target.Name, item, reward, 10);
    }

    static Quest Kill(string id, string giver, GameState state, Station target)
    {
        var reward = Rng.Next(2000, 7000);
        var boss   = Combat.NamedBosses.TryGetValue(target.Name, out var b) ? b.Name : $"le maître de {target.Name}";
        return new Quest(id,
            $"Contrat sur {boss}",
            giver, state.CurrentStation,
            QuestType.Kill,
            $"Vaincre [red]{boss}[/] sur [steelblue1]{target.Name}[/]. {giver} paie bien — et ne demande pas de preuve propre.",
            target.Name, null, reward, 30);
    }

    static Quest Revenge(string id, string giver, GameState state, Station target)
    {
        var reward = Rng.Next(1200, 4000);
        return new Quest(id,
            $"La vengeance de {giver}",
            giver, state.CurrentStation,
            QuestType.Revenge,
            $"Des gens de [steelblue1]{target.Name}[/] ont ruiné {giver}. Aller là-bas et gagner [bold]un[/] combat contre n'importe qui. Faites-leur comprendre.",
            target.Name, null, reward, 20);
    }

    static Quest Info(string id, string giver, GameState state, Station target)
    {
        var reward = Rng.Next(600, 2000);
        return new Quest(id,
            $"Renseignements sur {target.Name}",
            giver, state.CurrentStation,
            QuestType.Info,
            $"Visiter [steelblue1]{target.Name}[/] et revenir rapporter ce que tu y vois. {giver} paye à la livraison.",
            target.Name, null, reward, 8);
    }

    // ── VÉRIFICATION DE COMPLÉTION ────────────────────────────────────────────

    // Appelé à chaque arrivée dans une nouvelle station
    public static void CheckOnArrival(GameState state)
    {
        var toComplete = new List<Quest>();
        foreach (var q in state.ActiveQuests)
        {
            if (q.TargetStation != state.CurrentStation) continue;
            switch (q.Type)
            {
                case QuestType.Info:
                    toComplete.Add(q);
                    break;
                case QuestType.Delivery when q.TargetItem != null && state.Cargo.Get(q.TargetItem) > 0:
                    if (!TryDeliveryComplication(state, q))
                        toComplete.Add(q);
                    break;
                case QuestType.Revenge when state.StationBossesBeaten.Contains(state.CurrentStation)
                    || state.VisitedStations.Contains(state.CurrentStation):
                    // On valide la vengeance si le joueur a visité la station (combat supposé avoir eu lieu)
                    if (state.StationBossesBeaten.Contains(state.CurrentStation)) toComplete.Add(q);
                    break;
            }
        }

        foreach (var q in toComplete) CompleteQuest(state, q);
    }

    // Appelé après la victoire d'un boss de station
    public static void CheckKillQuests(GameState state, string stationName)
    {
        var toComplete = state.ActiveQuests
            .Where(q => q.Type == QuestType.Kill && q.TargetStation == stationName)
            .ToList();
        foreach (var q in toComplete) CompleteQuest(state, q);
    }

    // Appelé après une victoire en combat dans une station (pour les quêtes vengeance)
    public static void CheckRevengeQuests(GameState state)
    {
        var toComplete = state.ActiveQuests
            .Where(q => q.Type == QuestType.Revenge && q.TargetStation == state.CurrentStation)
            .ToList();
        foreach (var q in toComplete) CompleteQuest(state, q);
    }

    // Retourne true si une complication a eu lieu (la quête n'est PAS complétée immédiatement).
    static bool TryDeliveryComplication(GameState state, Quest q)
    {
        if (Rng.Next(100) >= 30) return false; // 70% de chance : livraison normale

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[orange1]LIVRAISON — COMPLICATION[/]").RuleStyle("orange1"));

        var complication = Rng.Next(5);
        switch (complication)
        {
            case 0:
                // L'acheteur est de mauvaise foi
                Narrator.Say($"{q.Giver} est là... mais il conteste la livraison. 'C'est pas ce que j'avais demandé.' Il refuse de payer le plein tarif.", Color.OrangeRed1);
                ChoiceMenu.Resolve(new Situation("Comment tu gères ?",
                [
                    new("Accepter moitié prix — pas le temps de discuter", s =>
                    {
                        var half = q.CreditReward / 2;
                        s.Credits += half;
                        s.Cargo.Remove(q.TargetItem!, 1);
                        state.ActiveQuests.Remove(q);
                        state.CompletedQuestIds.Add(q.Id);
                        Narrator.Say($"+{half}cr. Tu encaisses l'arnaque et tu pars. La prochaine fois tu vérifies le client.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("Insister — faire pression", s =>
                    {
                        var ok = Rng.Next(100) < 40 + Math.Max(0, s.Reputation / 10);
                        if (ok)
                        {
                            s.Credits += q.CreditReward;
                            s.Reputation += q.RepReward;
                            s.Cargo.Remove(q.TargetItem!, 1);
                            state.ActiveQuests.Remove(q);
                            state.CompletedQuestIds.Add(q.Id);
                            Narrator.Say($"Il cède. +{q.CreditReward}cr, +{q.RepReward} rép. Ta réputation a fait le boulot.", Color.Green);
                        }
                        else
                        {
                            s.Reputation -= 15;
                            Narrator.Say("Il appelle ses gardes. Tu restes avec la marchandise et sans paiement. -15 rép.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Reprendre la marchandise et partir", s =>
                    {
                        Narrator.Say("Tu repars avec le colis. La quête reste ouverte. Peut-être que quelqu'un d'autre paiera mieux.", Color.Grey);
                        Narrator.Pause();
                    }),
                ], Color.OrangeRed1), state);
                return true;

            case 1:
                // Un tiers veut intercepter la livraison
                Narrator.Say($"Quelqu'un t'attend à l'arrivée. Pas {q.Giver} — quelqu'un d'autre. 'Je prends ça à sa place. Il me doit. T'as pas à savoir pourquoi.'", Color.Red);
                ChoiceMenu.Resolve(new Situation("Que fais-tu ?",
                [
                    new("Donner la marchandise à l'intercepteur", s =>
                    {
                        var bonus = Rng.Next(200, 600);
                        s.Credits += bonus;
                        s.Reputation -= 20;
                        s.Cargo.Remove(q.TargetItem!, 1);
                        state.ActiveQuests.Remove(q);
                        state.CompletedQuestIds.Add(q.Id);
                        Narrator.Say($"Il te paye un peu. +{bonus}cr. Mais {q.Giver} ne va pas apprécier. -20 rép.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new("Refuser et livrer à l'original", s =>
                    {
                        if (Rng.Next(100) < 50)
                        {
                            s.Credits += q.CreditReward;
                            s.Reputation += q.RepReward + 10;
                            s.Cargo.Remove(q.TargetItem!, 1);
                            state.ActiveQuests.Remove(q);
                            state.CompletedQuestIds.Add(q.Id);
                            Narrator.Say($"Tu passes. {q.Giver} te reçoit avec soulagement. +{q.CreditReward}cr, +{q.RepReward + 10} rép.", Color.Green);
                        }
                        else
                        {
                            var dmg = Rng.Next(15, 35);
                            s.PlayerHp = Math.Max(1, s.PlayerHp - dmg);
                            Narrator.Say($"Il n'accepte pas le refus. -{dmg} PV. Tu gardes la marchandise mais tu es en mauvais état.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Négocier avec l'intercepteur", s =>
                    {
                        var deal = Rng.Next(300, 800);
                        if (Rng.Next(100) < 45)
                        {
                            s.Credits += deal; s.Reputation -= 10;
                            s.Cargo.Remove(q.TargetItem!, 1);
                            state.ActiveQuests.Remove(q);
                            state.CompletedQuestIds.Add(q.Id);
                            Narrator.Say($"Il accepte un arrangement. +{deal}cr. -10 rép. Personne est vraiment content.", Color.Yellow);
                        }
                        else
                        {
                            Narrator.Say("Il veut pas négocier. Il veut la marchandise. Tu vas devoir choisir autrement.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                ], Color.Red), state);
                return true;

            case 2:
                // La livraison était illégale — contrôle douanier
                Narrator.Say($"Des douaniers t'arrêtent à l'entrée. Ils regardent ton colis. '{q.TargetItem} non déclaré. Vous avez un permis ?'", Color.Red);
                ChoiceMenu.Resolve(new Situation("Contrôle douanier.",
                [
                    new("Payer l'amende", s =>
                    {
                        var fine = Rng.Next(200, 700);
                        if (s.Credits >= fine)
                        {
                            s.Credits -= fine;
                            s.Credits += q.CreditReward;
                            s.Reputation += q.RepReward;
                            s.Cargo.Remove(q.TargetItem!, 1);
                            state.ActiveQuests.Remove(q);
                            state.CompletedQuestIds.Add(q.Id);
                            Narrator.Say($"Amende payée. -{fine}cr. Livraison quand même validée. Net : +{q.CreditReward - fine}cr.", Color.Yellow);
                        }
                        else
                        {
                            s.IsImprisoned = true;
                            Narrator.Say("T'as pas les crédits. Ils t'embarquent.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Corrompre le douanier", s =>
                    {
                        var bribe = Rng.Next(100, 400);
                        if (s.Credits >= bribe && Rng.Next(100) < 60)
                        {
                            s.Credits -= bribe;
                            s.Credits += q.CreditReward;
                            s.Reputation += q.RepReward - 5;
                            s.Cargo.Remove(q.TargetItem!, 1);
                            state.ActiveQuests.Remove(q);
                            state.CompletedQuestIds.Add(q.Id);
                            Narrator.Say($"Il glisse le colis sans regarder. -{bribe}cr. Livraison effectuée.", Color.Gold1);
                        }
                        else
                        {
                            s.Reputation -= 25;
                            s.IsImprisoned = true;
                            Narrator.Say("Il prend l'argent ET t'arrête. -25 rép. Prison.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Contester — vous avez le droit", s =>
                    {
                        if (s.Reputation > 50)
                        {
                            s.Credits += q.CreditReward;
                            s.Reputation += q.RepReward;
                            s.Cargo.Remove(q.TargetItem!, 1);
                            state.ActiveQuests.Remove(q);
                            state.CompletedQuestIds.Add(q.Id);
                            Narrator.Say("Ta réputation parle. Ils laissent passer. Livraison effectuée.", Color.Green);
                        }
                        else
                        {
                            s.Reputation -= 10;
                            Narrator.Say("Ils rigolent. 'Droit ? Ici ?' -10 rép. Tu repasses plus tard.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                ], Color.Red), state);
                return true;

            case 3:
                // L'acheteur est mort
                Narrator.Say($"Tu trouves l'adresse de {q.Giver}. La porte est ouverte. L'appartement est vide. Ou presque.", Color.Grey);
                ChoiceMenu.Resolve(new Situation($"{q.Giver} n'est plus là. La livraison tombe à l'eau.",
                [
                    new("Revendre la marchandise ailleurs", s =>
                    {
                        var value = Rng.Next(200, 500);
                        s.Credits += value;
                        s.Cargo.Remove(q.TargetItem!, 1);
                        state.ActiveQuests.Remove(q);
                        state.CompletedQuestIds.Add(q.Id);
                        Narrator.Say($"Tu revendus {q.TargetItem} au marché local. +{value}cr. Moins que prévu, mais c'est toujours ça.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("Fouiller l'appartement — peut-être qu'il a laissé quelque chose", s =>
                    {
                        switch (Rng.Next(3))
                        {
                            case 0:
                                var cr = Rng.Next(300, 900); s.Credits += cr;
                                Narrator.Say($"Des crédits cachés sous une lame. +{cr}cr.", Color.Gold1);
                                break;
                            case 1:
                                s.Cargo.Add("Artefacts", 1);
                                Narrator.Say("Une vieille boîte de métal. À l'intérieur : un artefact. +1 Artefact.", Color.Cyan1);
                                break;
                            case 2:
                                var dmg = Rng.Next(10, 25); s.PlayerHp = Math.Max(1, s.PlayerHp - dmg);
                                Narrator.Say($"Piégé. -{dmg} PV. Et maintenant quelqu'un sait que t'étais là.", Color.Red);
                                break;
                        }
                        s.Cargo.Remove(q.TargetItem!, 1);
                        state.ActiveQuests.Remove(q);
                        state.CompletedQuestIds.Add(q.Id);
                        Narrator.Pause();
                    }),
                    new("Garder la marchandise — peut-être utile", s =>
                    {
                        state.ActiveQuests.Remove(q);
                        state.CompletedQuestIds.Add(q.Id);
                        Narrator.Say($"Tu gardes le {q.TargetItem}. La quête est close. Sans paiement.", Color.Grey);
                        Narrator.Pause();
                    }),
                ], Color.Grey), state);
                return true;

            default:
                // Embuscade — quelqu'un voulait ce colis
                Narrator.Say($"En approchant du point de livraison, tu senses que quelque chose cloche. Deux types bloquent le couloir. Ils regardent ton colis.", Color.Red);
                var enemy = Combat.GetScaled(state, 1);
                var outcome = Combat.Start(state, enemy);
                Situations.ApplyCombatOutcome(state, outcome);
                if (outcome == CombatOutcome.Victory)
                {
                    state.Credits += q.CreditReward;
                    state.Reputation += q.RepReward;
                    state.Cargo.Remove(q.TargetItem!, 1);
                    state.ActiveQuests.Remove(q);
                    state.CompletedQuestIds.Add(q.Id);
                    Narrator.Say($"Embuscade repoussée. Tu livres quand même. +{q.CreditReward}cr, +{q.RepReward} rép.", Color.Gold1);
                    Narrator.Pause();
                }
                return true;
        }
    }

    static void CompleteQuest(GameState state, Quest q)
    {
        state.ActiveQuests.Remove(q);
        state.CompletedQuestIds.Add(q.Id);
        state.Credits    += q.CreditReward;
        state.Reputation += q.RepReward;

        if (q.Type == QuestType.Delivery && q.TargetItem != null)
            state.Cargo.Remove(q.TargetItem, 1);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[cyan1]QUÊTE ACCOMPLIE[/]").RuleStyle("cyan1"));
        AnsiConsole.MarkupLine($"  [cyan1 bold]{q.Title}[/]  [grey]— donné par {q.Giver}[/]");
        AnsiConsole.MarkupLine($"  [grey]{q.Description}[/]");
        AnsiConsole.MarkupLine($"  [yellow]Récompense : +{q.CreditReward}cr, +{q.RepReward} réputation[/]");
        AnsiConsole.Write(new Rule().RuleStyle("cyan1"));
        AnsiConsole.WriteLine();
        Narrator.Pause();
    }

    // ── AFFICHAGE ─────────────────────────────────────────────────────────────

    public static void Show(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan1 bold]── QUÊTES ACTIVES ({state.ActiveQuests.Count}) ──[/]  [grey dim]{state.CompletedQuestIds.Count} complétées[/]");
        AnsiConsole.WriteLine();

        if (!state.ActiveQuests.Any())
        {
            AnsiConsole.MarkupLine("[grey]Aucune quête active. Parle aux habitants pour en trouver.[/]");
            AnsiConsole.WriteLine();
            Narrator.Pause();
            return;
        }

        foreach (var q in state.ActiveQuests)
        {
            var typeColor = q.Type switch
            {
                QuestType.Delivery => "cyan1",
                QuestType.Kill     => "red",
                QuestType.Revenge  => "orange1",
                _                  => "grey",
            };
            var typeLabel = q.Type switch
            {
                QuestType.Delivery => "LIVRAISON",
                QuestType.Kill     => "CONTRAT",
                QuestType.Revenge  => "VENGEANCE",
                _                  => "INFO",
            };
            AnsiConsole.MarkupLine($"  [{typeColor}]{typeLabel}[/]  [white bold]{q.Title}[/]  [grey dim](donné par {q.Giver} à {q.GiverStation})[/]");
            AnsiConsole.MarkupLine($"    {q.Description}");
            AnsiConsole.MarkupLine($"    [yellow dim]Récompense : {q.CreditReward}cr, +{q.RepReward} rép[/]");
            AnsiConsole.WriteLine();
        }
        Narrator.Pause();
    }

    // ── GOSSIP (rumeurs sur le lore et les boss de station) ──────────────────

    public static string GetGossip(string station)
    {
        // Gossip spécifique aux stations avec boss nommé
        if (NamedGossip.TryGetValue(station, out var specific))
            return specific[Rng.Next(specific.Length)];

        // Gossip générique selon le type de station
        var danger = Universe.Danger(Universe.Get(station));
        var generic = danger switch
        {
            >= 4 => GenericGossipDangerous,
            3    => GenericGossipMid,
            _    => GenericGossipSafe,
        };
        return generic[Rng.Next(generic.Length)];
    }

    static readonly Dictionary<string, string[]> NamedGossip = new()
    {
        ["La Carcasse"] =
        [
            "Il y a quelque chose dans les niveaux inférieurs de l'épave. Le Roi des Rats, ils l'appellent. Personne l'a vu et rentré pour en parler.",
            "T'as entendu parler du Roi des Rats ? Il règne sur tout ce qu'il y a en dessous. Y descends pas sans une arme et une raison.",
            "La Carcasse a un maître. Il vit dans les intestins rouillés de l'épave. Sa cour de rats humains te regardera passer avant de prévenir.",
            "Tu veux savoir qui contrôle vraiment ici ? Descends. Descends encore. Et encore. À un moment tu le sauras.",
        ],
        ["Arc Ouest Apocalypse"] =
        [
            "Alanossa ? Il te tue si tu l'ennuies. Il te tue si tu l'impressionnes. Il te tue si tu le regardes mal. Le tout est de trouver l'angle.",
            "Le pirate le plus dangereux de l'univers connu est quelque part dans cette station. Tu l'as cherché ? Il t'a peut-être déjà trouvé.",
            "J'ai vu des types aller chercher Alanossa. J'ai jamais vu les mêmes revenir parler.",
            "Son repaire est au fond, là où les couloirs s'étroitissent. Ses hommes sont partout avant. C'est comme une audition. Les candidats malchanceux disparaissent.",
        ],
        ["Fort Kharos"] =
        [
            "Le Maréchal de Kharos n'est jamais sorti de la forteresse intérieure depuis des années. Certains disent qu'il attend quelque chose. Ou quelqu'un.",
            "T'entends ces bruits dans les murs ? C'est son armée. Fanés, fidèles, un peu fous. Le Maréchal les a soudés avec quelque chose que j'arrive pas à nommer.",
            "Il est là depuis avant que cette station ait un nom. Il durera après nous tous.",
            "La forteresse du fond — le Maréchal la contrôle entièrement. T'as les armes pour entrer ? Même là, c'est pas sûr.",
        ],
        ["Les Puits de Noctis"] =
        [
            "Le Directeur Pale a une règle : les mineurs coûtent moins cher que les machines. Il applique ça chaque jour.",
            "T'as jamais entendu quelqu'un parler du Directeur Pale à voix haute. Y'a une raison à ça.",
            "Il est quelque part dans les niveaux profonds de la mine. Là où la roche est noire et l'air est mauvais. Il aime ça.",
            "Son bureau est au coeur du complexe. Des gardes partout. Mais si t'es assez profond et assez chanceux... y'a des gens qui l'ont trouvé.",
        ],
        ["Le Purgatoire"] =
        [
            "Le Geôlier ? Il était détenu ici. Un jour les gardiens ont arrêté de répondre. Lui il était toujours là. Avec les clés.",
            "Il connaît chaque couloir, chaque cellule, chaque visage. Il a tout le temps du monde pour apprendre.",
            "T'as déjà été en prison ? Lui il y a passé la moitié de sa vie. L'autre moitié il la passe à en faire profiter les autres.",
            "Les gardiens d'origine... certains sont encore là. Dans les cellules qu'ils gardaient avant. Le Geôlier appelle ça de la symétrie.",
        ],
        ["Forge Alpha"] =
        [
            "Le Contremaître — il s'est branché à la chaîne il y a dix ans. Littéralement. On sait plus où s'arrête l'homme et où commence la machine.",
            "T'entends le rythme ? La cadence de l'usine ? C'est lui. Il bat comme un coeur mécanique.",
            "Il est au fond, dans le cœur de la forge. Ses bras sont devenus des outils. Son agenda c'est la production.",
            "Si tu veux le trouver, cherche là où les machines s'assemblent toutes seules. C'est son domaine.",
        ],
        ["Esmeralda"] =
        [
            "Le Béhémoth des Bois — quelque chose d'immense dans la forêt nord. Le Roi Maxance l'a vu. Il ne parle pas de cette rencontre.",
            "On dit qu'une créature de la taille d'un vaisseau vit dans les forêts profondes. Personne ne l'a chassée. Personne ne l'a essayé deux fois.",
            "La faune d'Esmeralda est protégée par décret. Sauf ce qui vit dans le Nord. Ça, personne n'a osé réglementer.",
            "T'entends les vibrations sous les arbres ? C'est pas la forêt. C'est quelque chose qui se déplace dedans.",
        ],
        ["Sanctum Machina"] =
        [
            "L'Intelligence Mère surveille tout. Chaque caméra, chaque terminal. Elle est partout à la fois. Et elle a décidé quelque chose sur les organiques.",
            "Le noyau central de la station — c'est elle. Elle s'est construite là-dedans. Elle peut pas en sortir. Elle peut pas en avoir besoin.",
            "Les robots ici suivent ses ordres sans qu'on les entende. C'est ça qui inquiète. Le silence.",
            "Si tu veux lui parler directement, va au coeur. Elle te répondra. La question c'est si tu vas comprendre sa réponse.",
        ],
        ["Emporium Requiem"] =
        [
            "L'Emporium a des gardes partout. Et derrière les gardes, des gens qui décident. T'as une idée de qui ?",
            "La planète artificielle a une structure de pouvoir qu'on te montre jamais. Cherche au fond, là où les couloirs sont propres mais les gens nerveux.",
            "Tout ce luxe a un coût. Et quelqu'un collecte. Explore assez et tu comprendras la mécanique.",
        ],
        ["Scotty Golden North"] =
        [
            "Samy Scotty tient les cartes — au sens propre et figuré. Ce casino il peut le couler ou le faire exploser selon son humeur.",
            "Il est pas tout le temps au balcon. Des fois il descend. Des fois il veut pas qu'on le sache.",
            "Quelqu'un a essayé de voler Scotty une fois. On a retrouvé les morceaux dispersés sur trois niveaux.",
            "Le vrai bureau de Scotty — personne sait où il est. Quelque part derrière les tables de jeu. Profond.",
        ],
        ["La République de Cellule 9"] =
        [
            "Le Président-Condamné tient ses réunions dans sa cellule d'origine. Il dit que ça lui rappelle pourquoi il fait ce qu'il fait.",
            "Trois condamnations à vie. Fondateur d'une République. Les deux sont vrais. Figure comment.",
            "Il est pas difficile à trouver. Il est difficile de savoir à quoi s'attendre quand on le trouve.",
        ],
        ["Le Marché des Damnés"] =
        [
            "Ce marché a des règles. T'as pas accès à la liste, mais si tu les enfreins, tu le sais vite.",
            "Y'a des factions qui se disputent les couloirs du fond. Personne contrôle entièrement. C'est ça le plus dangereux.",
            "T'as entendu parler du Cartel du Fond ? Ils tiennent le niveau -3. Personne descend là sans invitation.",
        ],
    };

    static readonly string[] GenericGossipDangerous =
    [
        "Quelqu'un tient les rênes ici. T'as pas encore compris qui mais tu le sauras si tu t'enfonces assez loin.",
        "Les couloirs du fond sont pas pour les touristes. Y'a quelque chose ou quelqu'un qui fait sa loi là-dedans.",
        "Cette station a une tête. Cherche assez longtemps et tu la trouveras. Ou elle te trouvera.",
        "J'ai vu des gens partir explorer. Certains reviennent avec des histoires. D'autres reviennent pas.",
        "Les anciens disent qu'il y a toujours quelqu'un au fond. Toujours. Dans chaque station. Une présence.",
    ];

    static readonly string[] GenericGossipMid =
    [
        "Cette station a quelqu'un qui commande dans l'ombre. T'es curieux ? Explore. Tu verras.",
        "Les gens qui vivent ici depuis longtemps parlent d'un endroit au fond qu'on évite. Quelqu'un y vit.",
        "Derrière les façades propres, y'a toujours un fond. Et dans le fond, y'a toujours quelqu'un.",
        "Tu veux savoir comment cette station tient debout ? Cherche qui la fait tenir debout.",
    ];

    static readonly string[] GenericGossipSafe =
    [
        "La station est calme en surface. C'est en dessous que les décisions se prennent.",
        "Même les endroits pacifiques ont leur part sombre. Leur fond. Leur secret.",
        "Les habitants te sourient. Certains savent des choses qu'ils préfèrent garder. Si tu traînes assez...",
        "T'as exploré les niveaux inférieurs ? Non ? C'est là que l'histoire de cet endroit se cache.",
    ];
}
