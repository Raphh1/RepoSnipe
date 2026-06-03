using Spectre.Console;

namespace VoidTrader;

static class Encounters
{
    private static readonly Random Rng = new();

    // ── POINT D'ENTRÉE ──────────────────────────────────────────────────────

    public static void Roll(GameState state)
    {
        // Maudit : les bons événements ont 50% de chance de fizzler
        if (state.Class.CursedEvents && Rng.Next(2) == 0)
        {
            Narrator.Say("Quelque chose allait se passer. Mais rien ne se passe. La malédiction fait son travail.", Color.Grey);
            Narrator.Pause();
            return;
        }

        // ── Événement hostile imposé par le niveau de danger ──────────────────
        // Plus la station est dangereuse, plus la chance qu'un mauvais truc arrive
        // AVANT même de piocher dans le pool normal est élevée.
        // danger 1→5%, 2→15%, 3→30%, 4→45%, 5→62%
        var danger = Universe.Danger(Universe.Get(state.CurrentStation));
        var hostileChance = danger switch { 1 => 5, 2 => 15, 3 => 30, 4 => 45, _ => 62 };

        if (Rng.Next(100) < hostileChance)
        {
            ChoiceMenu.Resolve(DangerEvent(state, danger), state);
            return;
        }

        // Stalker — le PNJ obsessionnel peut apparaître ou déclencher un événement
        if (StalkerSystem.MaybeTrigger(state)) return;

        // Humiliation publique — peut arriver dans toutes les zones, fréquence légère
        // mais augmente avec le niveau de danger (les gens sont plus irrespectueux)
        var humiliationChance = danger * 4;   // 4% danger1 … 20% danger5
        if (Rng.Next(100) < humiliationChance)
        {
            ChoiceMenu.Resolve(PublicHumiliation(state), state);
            return;
        }

        // 35% de chance : event JSON (nouveau contenu varié), 65% : pool C# existant
        if (Rng.Next(100) < 35)
        {
            JsonEvents.RunWanderEvent(state, danger);
            return;
        }

        var pool = GetPool(state.CurrentStation);
        ChoiceMenu.Resolve(pool[Rng.Next(pool.Count)](state), state);
    }

    // ── HUMILIATION PUBLIQUE ─────────────────────────────────────────────────
    // Quelqu'un t'humilie en public. Perte de réputation garantie.
    // Le joueur choisit comment réagir.

    static readonly string[] Insults =
    [
        "Sale parasite spatial, va crever dans le vide !",
        "T'as acheté ton laissez-passer au fond d'une benne ?",
        "Même les rats de cale ont plus de dignité que toi.",
        "Va te faire recycler, t'es bon qu'à ça.",
        "On dirait que ta mère t'a construit avec des restes.",
        "Je t'ai vu arriver — j'aurais dû fermer la porte.",
        "T'as volé le vaisseau ou on te l'a refusé dans les déchetteries ?",
        "T'es le genre de type qui réussit rien mais qui parle beaucoup.",
        "Ta réputation te précède. C'est pas un compliment.",
        "Ton vaisseau sent autant que ta carrière : la désintégration.",
        "Sérieusement, t'es qui pour être là ?",
        "J'ai vu des scavengers avec plus de classe.",
        "Va chercher ton carburant ailleurs, t'enlèves de l'air aux gens normaux.",
        "Même les Puits de Noctis refuseraient de toi.",
        "T'as la gueule de quelqu'un qui doit de l'argent à tout le monde.",
        "Ta présence ici baisse le niveau de la pièce de 40%.",
        "On t'a pas appris à frapper avant d'entrer ? Dans ta culture, c'est quoi la politesse ?",
        "J'ai vu des épaves plus présentables que toi.",
        "Le seul truc impressionnant chez toi c'est que t'es encore en vie.",
        "Les gardes vont finir par te ramasser comme les autres déchets.",
        "T'as un contrat sur ta tête ou t'es juste naturellement repoussant ?",
        "Ton vaisseau est aussi nul que ta façon de parler.",
        "Je te paierais pour partir, mais ça reviendrait trop cher.",
        "T'as l'air de quelqu'un qui a raté chaque décision importante de sa vie.",
        "Dans un autre univers, t'aurais peut-être été utile. Pas dans celui-ci.",
        "T'es venu ici pour quoi ? Déprimer les gens autour de toi ?",
        "Si ta réputation était un vaisseau, elle serait déjà au fond d'un astéroïde.",
        "Dégage. T'as même pas la décence d'être intéressant.",
        "T'es le genre de type que tout le monde regarde partir avec soulagement.",
        "Ça pue l'échec par ici. Oh. C'est toi.",
        "Même les Faucons Noirs te refuseraient.",
        "T'as survécu jusqu'ici ? C'est décevant pour le reste de l'univers.",
        "Va mourir ailleurs, au moins ça serait utile pour les scavengers.",
        "T'es le genre de parasite que même les parasites évitent.",
        "Si t'étais une marchandise, personne t'achèterait même soldé.",
        "J'ai des rebuts de ferraille qui ont plus de valeur que toi.",
        "T'aurais pas une raison d'exister que tu nous aurais cachée ?",
        "Ta mère t'aurait vendu pour du carburant si elle avait su.",
        "Les nébuleuses les plus vides ont plus de contenu que toi.",
        "T'as la classe d'un Vagabond et le charme d'une taxe.",
    ];

    static Situation PublicHumiliation(GameState state)
    {
        var insult = Insults[Rng.Next(Insults.Length)];
        var who = Rng.Next(5) switch
        {
            0 => "Un homme appuyé au mur te dévisage",
            1 => "Un groupe de types qui rigolent te regarde passer",
            2 => "Une femme avec une cigarette crachée à tes pieds",
            3 => "Un commerçant depuis son stand, fort pour que tout le monde entende",
            _ => "Une voix depuis la foule",
        };

        Narrator.Say($"{who} : \"{insult}\"", Color.OrangeRed1);
        state.Reputation -= Rng.Next(5, 18);   // réputation perdue automatiquement
        Display.ShowEvent($"Humiliation publique. -{Rng.Next(5, 18)} réputation.", Color.Red);

        return new Situation("La foule a entendu. Tout le monde regarde. Comment tu réagis ?",
        [
            new("Ignorer et continuer à marcher", s =>
            {
                if (Rng.Next(100) < 40) { s.Reputation -= 8; Narrator.Say("Ils recommencent. La foule ricane. -8 rép supplémentaires.", Color.Red); }
                else Narrator.Say("Tu passes sans répondre. Certains t'admirent pour ça. La plupart non.", Color.Grey);
                Narrator.Pause();
            }),
            new("Répondre avec la même monnaie — les insulter en public", s =>
            {
                if (Rng.Next(100) < 50)
                {
                    s.Reputation += 15;
                    var counter = Rng.Next(5) switch {
                        0 => "Tu réponds quelque chose que personne attendait. La foule retient son souffle. Puis quelqu'un rit — avec toi. +15 rép.",
                        1 => "Ta réplique est si parfaite que même lui est obligé de sourire. +15 rép.",
                        2 => "Tu retournes l'humiliation. Il rougit. La foule prend ton parti. +15 rép.",
                        3 => "Tu dis quelque chose de simple, froid, qui coupe. Il ne répond pas. +15 rép.",
                        _ => "Ta réponse est cinglante. Elle colle. +15 rép.",
                    };
                    Narrator.Say(counter, Color.Green);
                }
                else
                {
                    s.Reputation -= 10;
                    Narrator.Say("Mauvaise réplique. La foule ricane encore plus. -10 rép supplémentaires.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("Approcher et le regarder dans les yeux sans rien dire", s =>
            {
                var chance = 30 + (s.Class.Name == "Seigneur de guerre" ? 35 : 0) + Math.Max(0, -s.Reputation / 8);
                if (Rng.Next(100) < chance)
                {
                    s.Reputation += 20;
                    Narrator.Say("Le silence dure. Il recule. La foule a compris. +20 rép.", Color.Green);
                }
                else
                {
                    s.Reputation -= 5;
                    Narrator.Say("Il soutient ton regard. Il rigole. La foule avec lui. -5 rép.", Color.Red);
                }
                Narrator.Pause();
            }),
            new("Lui casser la gueule — ici, maintenant", s =>
            {
                var e = Combat.TierLow[Rng.Next(Combat.TierLow.Count)];
                s.Reputation -= 15;
                Narrator.Say("Tout le monde regardait déjà. Maintenant ils regardent encore plus. -15 rép au passage.", Color.OrangeRed1);
                Situations.ApplyCombatOutcome(s, Combat.Start(s, e));
            }),
        ], Color.OrangeRed1);
    }

    // ── ÉVÉNEMENT HOSTILE SCALÉ PAR DANGER ──────────────────────────────────
    // Déclenché AVANT la sélection du pool normal. Le type d'événement monte
    // en gravité avec le danger de la station.

    static Situation DangerEvent(GameState state, int danger)
    {
        // Danger 1-2 → petite criminalité / agression opportuniste
        // Danger 3   → organisation criminelle / traquenard
        // Danger 4-5 → embuscade armée / assassin / gang organisé

        var pool = danger switch
        {
            <= 2 => DangerEventsLow,
            3    => DangerEventsMid,
            _    => DangerEventsHigh,
        };

        return pool[Rng.Next(pool.Count)](state, danger);
    }

    static readonly List<Func<GameState, int, Situation>> DangerEventsLow =
    [
        // Vol à l'arraché
        (s, _) => new Situation(
            "Une main arrache quelque chose de ta veste. Un gamin part en courant.",
            [
                new("Courir après — rattraper le voleur", gs => {
                    if (Rng.Next(100) < 55) {
                        var recup = Rng.Next(80, 300); gs.Credits += recup;
                        Narrator.Say($"Tu le rattrapes. Il se débat. T'as récupéré {recup}cr et une fierté intacte.", Color.Green);
                    } else {
                        var perdu = Rng.Next(80, 300); gs.Credits = Math.Max(0, gs.Credits - perdu);
                        Narrator.Say($"Il était plus rapide. -{perdu}cr. T'as l'air ridicule à bout de souffle.", Color.Red);
                    }
                    Narrator.Pause();
                }),
                new("Crier au voleur", gs => {
                    gs.Reputation += 3;
                    if (Rng.Next(100) < 40) { var recup = Rng.Next(50, 200); gs.Credits += recup; Narrator.Say($"Un passant l'intercepte. +{recup}cr récupérés.", Color.Cyan1); }
                    else Narrator.Say("Personne réagit. Les gens baissent les yeux.", Color.Grey);
                    Narrator.Pause();
                }),
                new("Laisser tomber — trop de danger pour courir ici", gs => {
                    var perdu = Rng.Next(80, 300); gs.Credits = Math.Max(0, gs.Credits - perdu);
                    Narrator.Say($"-{perdu}cr. T'as laissé. Sage ou fataliste, difficile à dire.", Color.Red);
                    Narrator.Pause();
                }),
            ], Color.Yellow),

        // Ivrogne agressif version rapide
        (s, _) => new Situation(
            "Un type ivre te bloque physiquement. Il sent la ferraille frelatée. Il veut quelque chose.",
            [
                new("Le repousser violemment", gs => {
                    switch (Rng.Next(3)) {
                        case 0: gs.Credits += Rng.Next(30, 150); Narrator.Say("Il chancelle, fouille ses poches, te tend quelques crédits. Bizarre.", Color.Green); break;
                        case 1: Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.TierLow[Rng.Next(Combat.TierLow.Count)])); return;
                        case 2: Narrator.Say("Il s'effondre. Les gens autour regardent ailleurs.", Color.Grey); break;
                    }
                    Narrator.Pause();
                }),
                new("Lui payer un verre pour qu'il dégage", gs => {
                    var cout = Rng.Next(20, 60); gs.Credits = Math.Max(0, gs.Credits - cout);
                    if (Rng.Next(2) == 0) { gs.Credits += Rng.Next(100, 400); Narrator.Say($"-{cout}cr. Il était moins ivre que prévu. Il te balance une info utile.", Color.Cyan1); }
                    else Narrator.Say($"-{cout}cr. Il prend le verre et disparaît.", Color.Grey);
                    Narrator.Pause();
                }),
                new("L'ignorer et passer en force", gs => {
                    if (Rng.Next(100) < 40) { var hp = Rng.Next(5, 18); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp); Narrator.Say($"Il te lance quelque chose dans le dos. -{hp} PV.", Color.Red); }
                    else Narrator.Say("Il te laisse passer en marmonnant.", Color.Grey);
                    Narrator.Pause();
                }),
            ], Color.OrangeRed1),

        // Pickpocket discret
        (s, _) => new Situation(
            "Tu sens une main dans ta veste. Quelqu'un essaie de te voler.",
            [
                new("Attraper son poignet immédiatement", gs => {
                    switch (Rng.Next(3)) {
                        case 0: gs.Reputation += 10; Narrator.Say("C'est un gamin. Tu le regardes dans les yeux et tu le relâches. Il part sans un mot. +10 rép.", Color.Green); break;
                        case 1: Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.TierLow[Rng.Next(Combat.TierLow.Count)])); return;
                        case 2: var bonus = Rng.Next(80, 300); gs.Credits += bonus; Narrator.Say($"Il avait déjà pris les tiennes — et les siennes. Confusion totale. +{bonus}cr.", Color.Cyan1); break;
                    }
                    Narrator.Pause();
                }),
                new("Laisser faire — voir ce qu'il prend", gs => {
                    var perdu = Rng.Next(100, 500); gs.Credits = Math.Max(0, gs.Credits - perdu);
                    Narrator.Say($"Il disparaît dans la foule. -{perdu}cr.", Color.Red); Narrator.Pause();
                }),
                new("Le suivre discrètement", gs => {
                    switch (Rng.Next(3)) {
                        case 0: var butin = Rng.Next(400, 1200); gs.Credits += butin; gs.Reputation -= 10; Narrator.Say($"Sa planque. +{butin}cr, -10 rép.", Color.Gold1); break;
                        case 1: Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.TierLow[Rng.Next(Combat.TierLow.Count)])); return;
                        case 2: Narrator.Say("Il disparaît dans une ventilation.", Color.Grey); break;
                    }
                    Narrator.Pause();
                }),
            ], Color.Yellow),
    ];

    static readonly List<Func<GameState, int, Situation>> DangerEventsMid =
    [
        // Gang de rue qui rackette
        (s, danger) => new Situation(
            "Trois types bloquent le couloir. Ils te fixent. Le plus grand tend la main. 'Droit de passage.'",
            [
                new("Payer sans discuter", gs => {
                    var cout = Rng.Next(150, 500); gs.Credits = Math.Max(0, gs.Credits - cout);
                    switch (Rng.Next(3)) {
                        case 0: Narrator.Say($"Ils s'écartent. -{cout}cr.", Color.Yellow); break;
                        case 1: var extra = Rng.Next(100, 300); gs.Credits = Math.Max(0, gs.Credits - extra); Narrator.Say($"-{cout}cr. Et en redemandent. -{extra}cr.", Color.Red); break;
                        case 2: gs.Credits += Rng.Next(200, 500); Narrator.Say($"-{cout}cr payés. L'un d'eux te glisse quelque chose. Il a reconnu quelqu'un en toi.", Color.Cyan1); break;
                    }
                    Narrator.Pause();
                }),
                new("Intimider — tu travailles pour pire qu'eux", gs => {
                    var chance = 20 + Math.Max(0, -gs.Reputation / 5) + (gs.Class.Name == "Seigneur de guerre" ? 35 : 0);
                    if (Rng.Next(100) < chance) { Narrator.Say("Ils hésitent. Ils s'écartent. Ton nom ou ta gueule — ça a marché.", Color.Green); }
                    else { Narrator.Say("Ils rigolent. Puis ils avancent.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], danger - 2))); return; }
                    Narrator.Pause();
                }),
                new("Foncer — t'avais prévu ça", gs => {
                    var e = Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], danger - 2);
                    Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                }),
                new("Faire demi-tour — un autre chemin", gs => {
                    if (Rng.Next(100) < 45) { Narrator.Say("Ils te suivent.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.TierMid[Rng.Next(Combat.TierMid.Count)])); return; }
                    else Narrator.Say("Tu perds dix minutes mais tu passes ailleurs.", Color.Grey);
                    Narrator.Pause();
                }),
            ], Color.OrangeRed1),

        // Chasseur de primes
        (s, danger) => new Situation(
            "Un homme s'approche et vérifie quelque chose sur son device. Il a ta photo.",
            [
                new("Nier — ce n'est pas toi", gs => {
                    switch (Rng.Next(3)) {
                        case 0: Narrator.Say("Il hésite. Il repart vérifier ailleurs.", Color.Yellow); break;
                        case 1: Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], danger - 2))); return;
                        case 2: Narrator.Say("Il te croit. Il s'excuse presque.", Color.Green); break;
                    }
                    Narrator.Pause();
                }),
                new($"Payer pour qu'il oublie", gs => {
                    var prime = Rng.Next(400, 1500);
                    if (gs.Credits < prime) { Narrator.Say($"T'as pas assez ({prime}cr demandés).", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.TierMid[Rng.Next(Combat.TierMid.Count)])); return; }
                    gs.Credits -= prime;
                    if (Rng.Next(100) < 55) { Narrator.Say($"-{prime}cr. Il part. Jusqu'à la prochaine fois.", Color.Yellow); gs.Reputation += 5; }
                    else { gs.IsImprisoned = true; Narrator.Say("Il prend l'argent ET t'arrête. La prime était plus haute.", Color.Red); }
                    Narrator.Pause();
                }),
                new("Attaquer en premier", gs => {
                    var e = Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], danger - 2);
                    Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                }),
                new("Se rendre — voir ce qui se passe", gs => {
                    switch (Rng.Next(3)) {
                        case 0: gs.IsImprisoned = true; Narrator.Say("Cellule directement.", Color.Red); return;
                        case 1: var caution = Rng.Next(400, 1200); if (gs.Credits >= caution) { gs.Credits -= caution; Narrator.Say($"Caution sur place. -{caution}cr.", Color.Yellow); } else { gs.IsImprisoned = true; Narrator.Say("T'as pas assez.", Color.Red); } break;
                        case 2: Narrator.Say("Il lit ton dossier. 'T'es pas celui que je cherche.' Il repart.", Color.Green); break;
                    }
                    Narrator.Pause();
                }),
            ], Color.Red),

        // Embuscade préparée
        (s, danger) => new Situation(
            "T'entends du mouvement derrière toi. Deux silhouettes bloquent l'avant. Tu es cerné.",
            [
                new("Se battre — les neutraliser", gs => {
                    var e = Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], danger - 2);
                    Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                }),
                new("Fuir par le côté — il y a forcément une sortie", gs => {
                    if (Rng.Next(100) < 45) {
                        gs.Fuel = Math.Max(0, gs.Fuel - 1);
                        Narrator.Say("T'as trouvé une issue. -1 carburant.", Color.Yellow);
                    } else {
                        var hp = Rng.Next(20, 40); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp);
                        Narrator.Say($"T'as pris des coups en essayant. -{hp} PV.", Color.Red);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.TierMid[Rng.Next(Combat.TierMid.Count)]));
                        return;
                    }
                    Narrator.Pause();
                }),
                new("Négocier — 'Qu'est-ce que vous voulez exactement ?'", gs => {
                    var cout = Rng.Next(300, 900);
                    if (gs.Credits >= cout) {
                        gs.Credits -= cout;
                        Narrator.Say($"Ils voulaient de l'argent. -{cout}cr. Tu repars.", Color.Yellow);
                    } else {
                        Narrator.Say("T'as pas ce qu'ils veulent.", Color.Red);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], 1)));
                        return;
                    }
                    Narrator.Pause();
                }),
            ], Color.Red),
    ];

    static readonly List<Func<GameState, int, Situation>> DangerEventsHigh =
    [
        // Assassin ciblé
        (s, danger) => new Situation(
            "Un homme te regarde depuis l'autre bout du couloir. Il porte une arme visible. Il marche vers toi sans se presser.",
            [
                new("Attaquer avant qu'il arrive à portée", gs => {
                    var e = Combat.Scale(Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)], danger - 3);
                    var outcome = Combat.Start(gs, e);
                    if (outcome == CombatOutcome.Victory) { var loot = Rng.Next(600, 2000); gs.Credits += loot; gs.Reputation += 10; Display.ShowEvent($"+{loot}cr sur lui. +10 rép.", Color.Gold1); }
                    Situations.ApplyCombatOutcome(gs, outcome);
                }),
                new("Fuir en courant maintenant", gs => {
                    if (Rng.Next(100) < 40) { gs.Fuel = Math.Max(0, gs.Fuel - 1); Narrator.Say("Tu l'as semé. -1 carburant.", Color.Yellow); Narrator.Pause(); }
                    else {
                        Narrator.Say("Il est plus rapide que prévu.", Color.Red);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.Scale(Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)], danger - 3)));
                    }
                }),
                new("Rester immobile — voir ce qu'il veut", gs => {
                    switch (Rng.Next(3)) {
                        case 0:
                            Narrator.Say("Il s'arrête à deux mètres. Il te donne une enveloppe. Il repart sans un mot.", Color.Yellow);
                            gs.Credits += Rng.Next(500, 1500); gs.Reputation -= 15;
                            Display.ShowEvent("L'enveloppe contenait une mission. Tu peux refuser. Ou pas.", Color.Yellow);
                            break;
                        case 1:
                            Narrator.Say("Il s'arrête. Il dit quelque chose. Tu as deux secondes pour réagir.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.Scale(Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)], danger - 3)));
                            return;
                        case 2:
                            Narrator.Say("Il t'a confondu avec quelqu'un d'autre. Il réalise. Il s'en va.", Color.Grey);
                            break;
                    }
                    Narrator.Pause();
                }),
            ], Color.Red),

        // Armée de bandits — trop nombreux, choix de gestion
        (s, danger) => new Situation(
            "Un groupe de huit types armés sort d'un couloir latéral. Trop nombreux pour un combat frontal.",
            [
                new("Courir — maintenant, sans réfléchir", gs => {
                    if (Rng.Next(100) < 50) { Narrator.Say("T'as couru. T'as semé. T'es essoufflé mais libre.", Color.Yellow); Narrator.Pause(); }
                    else {
                        var hp = Rng.Next(30, 60); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp);
                        Narrator.Say($"Pas assez rapide. Ils t'ont rattrapé et tabassé. -{hp} PV.", Color.Red); Narrator.Pause();
                    }
                }),
                new("Payer le tribut — tout ce qu'ils demandent", gs => {
                    var cout = Rng.Next(500, 2000); gs.Credits = Math.Max(0, gs.Credits - cout);
                    Narrator.Say($"Ils prennent. -{cout}cr. Ils te laissent partir. T'as peut-être pris la bonne décision.", Color.Yellow);
                    Narrator.Pause();
                }),
                new("Lancer un explosif et profiter du chaos pour fuir", gs => {
                    if (gs.Cargo.Get("Explosifs") > 0) {
                        gs.Cargo.Remove("Explosifs", 1);
                        gs.Reputation -= 15;
                        Narrator.Say("Chaos. Fumée. T'es dehors avant qu'ils comprennent ce qui s'est passé. -1 Explosif, -15 rép.", Color.OrangeRed1);
                    } else {
                        Narrator.Say("T'as pas d'explosifs. La seule option restante c'est de courir.", Color.Red);
                        var hp = Rng.Next(20, 45); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp);
                        Narrator.Say($"-{hp} PV.", Color.Red);
                    }
                    Narrator.Pause();
                }),
                new("Se battre — mourir debout", gs => {
                    // combat brutal, fortement défavorable
                    var e = Combat.Scale(Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)], danger - 2);
                    Situations.ApplyCombatOutcome(gs, Combat.Start(gs, e));
                }),
            ], Color.Red),

        // Hitman qui te surveille depuis un moment
        (s, danger) => new Situation(
            "Un inconnu s'est assis deux tables plus loin, face à toi, depuis cinq minutes. Il ne commande rien. Il attend.",
            [
                new("L'aborder directement — 'T'en veux à ma vie ou à mon portefeuille ?'", gs => {
                    switch (Rng.Next(3)) {
                        case 0:
                            Narrator.Say("Il rigole. 'Ni l'un ni l'autre. Je te testais.' Il glisse un bout de papier. Une offre.", Color.Cyan1);
                            var offre = Rng.Next(1000, 3000); gs.Credits += offre;
                            Display.ShowEvent($"+{offre}cr. T'es recruté pour quelque chose.", Color.Cyan1);
                            break;
                        case 1:
                            Narrator.Say("Il se lève. Combat dans le couloir.", Color.Red);
                            Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.Scale(Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)], danger - 3)));
                            return;
                        case 2:
                            gs.Reputation += 15;
                            Narrator.Say("Fausse alarme. C'était juste quelqu'un de bizarre. +15 rép pour avoir géré ça calmement.", Color.Green);
                            break;
                    }
                    Narrator.Pause();
                }),
                new("Partir discrètement sans le regarder", gs => {
                    if (Rng.Next(100) < 55) { Narrator.Say("Il te laisse partir. Peut-être que c'était pas toi qu'il cherchait.", Color.Grey); Narrator.Pause(); }
                    else {
                        Narrator.Say("Il te suit. Il t'attendait.", Color.Red);
                        Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.Scale(Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)], danger - 3)));
                    }
                }),
                new("Appeler les gardes — risqué mais visible", gs => {
                    if (Rng.Next(100) < 40) { gs.Reputation += 10; Narrator.Say("Les gardes l'emmènent. +10 rép.", Color.Green); Narrator.Pause(); }
                    else { gs.Reputation -= 20; Narrator.Say("Les gardes s'intéressent autant à toi qu'à lui. -20 rép.", Color.Red); gs.IsImprisoned = true; }
                }),
            ], Color.Red),
    ];

    static List<Func<GameState, Situation>> GetPool(string station) => station switch
    {
        "La Carcasse" or "Les Bas-Fonds de Vega" or "Fort Kharos" or "Port des Brumes"
            or "Station Rocaille" or "Avant-Poste Kalem" or "Forge Alpha"
            or "La Citadelle" or "La Ferronnerie" or "Terminus Sud"
            or "Les Décombres de Vael" or "La Bulle" => LowPool,

        "Emporium Requiem" or "Scotty Golden North" or "Star Quest"
            or "La Couronne d'Eos" or "Station Belvédère" or "Nexus Aldara"
            or "L'Académie Stellaire" or "Sanctum Machina" => HighPool,

        _ => MidPool,
    };

    static readonly List<Func<GameState, Situation>> LowPool =
    [
        Ivrogne,
        Pickpocket,
        PropositionLouche,
        GardeCorrompu,
        Blessé,
        Dispute,
        Dealer,
        BarFight,
        Overdose,
        AlcoolBar,
        Fugitif,
        TentationCaisse,
        NuitPerdue,
        GossipNpc,   // habitant qui raconte le lore et hints sur le boss
        QuestGiver,  // PNJ qui donne une quête
    ];

    static readonly List<Func<GameState, Situation>> MidPool =
    [
        ChasseurDePrimes,
        ContrebandierCoins,
        PropositionLouche,
        GardeCorrompu,
        Dispute,
        Chantage,
        MeurtreCommandité,
        JeuSouterrain,
        Dealer,
        AlcoolBar,
        NuitPerdue,
        SousMarine,
        RecrutementFaction,
        GossipNpc,
        QuestGiver,
    ];

    static readonly List<Func<GameState, Situation>> HighPool =
    [
        ChasseurDePrimes,
        Chantage,
        MeurtreCommandité,
        JeuSouterrain,
        RecrutementFaction,
        SousMarine,
        PropositionLouche,
        NuitPerdue,
        GossipNpc,
        QuestGiver,
    ];

    // ── RENCONTRES LOW TIER ─────────────────────────────────────────────────

    static Situation Ivrogne(GameState state) => new(
        "Un homme ivre t'attrape par le col. 'Toi... tu m'dois quelque chose.'",
        [
            new("Repousser violemment", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il chancelle, tombe. Silence. 'T'as raison. Désolé.' Il fouille ses poches et te tend quelques crédits.", Color.Green);
                        var cr = Rng.Next(50, 200);
                        s.Credits += cr; Display.ShowEvent($"+{cr}cr.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Il se redresse. 'Tu vas regretter ça.' Il siffle. Des silhouettes bougent dans l'ombre.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Il s'effondre contre le mur. Les gens autour regardent ailleurs.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Négocier — lui demander ce qu'il veut vraiment", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il se calme. Il cherchait juste quelqu'un à qui parler. Il te glisse un bout de papier. 'Y'a un truc là-bas. Je peux plus y aller.'", Color.Cyan1);
                        s.Credits += Rng.Next(300, 800);
                        Display.ShowEvent($"L'adresse mène à un dépôt oublié. +{Rng.Next(300, 800)}cr.", Color.Cyan1);
                        break;
                    case 1:
                        Narrator.Say("Il t'écoute. Puis ses yeux changent. 'T'es malin, toi.' Sa main glisse dans ta veste pendant qu'il parle.", Color.Red);
                        var vol = Rng.Next(100, 400);
                        s.Credits = Math.Max(0, s.Credits - vol);
                        Display.ShowEvent($"Il t'a volé pendant qu'il parlait. -{vol}cr.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il te raconte une histoire sans queue ni tête. Au bout de dix minutes, il s'endort contre le mur.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Lui offrir un verre", s =>
            {
                var cost = Rng.Next(20, 60);
                s.Credits = Math.Max(0, s.Credits - cost);
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il prend le verre. Deux heures plus tard, il t'a raconté tout ce qu'il sait sur les convois locaux.", Color.Green);
                        var gain = Rng.Next(200, 600);
                        s.Credits += gain; s.Reputation += 5;
                        Display.ShowEvent($"-{cost}cr. +{gain}cr d'infos. +5 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Il prend le verre. Il prend aussi ton portefeuille.", Color.Red);
                        var perdu = Rng.Next(150, 500);
                        s.Credits = Math.Max(0, s.Credits - perdu);
                        Display.ShowEvent($"-{cost}cr pour le verre. -{perdu}cr volés.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il prend le verre, te fait un clin d'œil, et disparaît.", Color.Grey);
                        Display.ShowEvent($"-{cost}cr.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Ignorer et passer ton chemin", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il te suit en marmonnant pendant trois couloirs. Puis abandonne.", Color.Grey);
                        break;
                    case 1:
                        Narrator.Say("Il te lance quelque chose dans le dos. Pas grave, mais ça fait mal.", Color.Yellow);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(5, 15));
                        Display.ShowEvent($"-{Rng.Next(5, 15)} PV joueur.", Color.Yellow);
                        break;
                    case 2:
                        Narrator.Say("Il te laisse passer. Tu l'entends : 'L'prochain, lui, il paiera.'", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.OrangeRed1);

    static Situation Pickpocket(GameState state) => new(
        "Tu sens une main dans ta veste. Quelqu'un essaie de te voler.",
        [
            new("Attraper son bras immédiatement", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Un gamin. Pas plus de quinze ans. Il te regarde avec des yeux immenses.", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Qu'est-ce que tu fais ?",
                        [
                            new("Le lâcher et partir", gs => { Narrator.Say("Tu le lâches. Il disparaît en courant.", Color.Grey); gs.Reputation += 5; Display.ShowEvent("+5 réputation.", Color.Green); Narrator.Pause(); }),
                            new("Le remettre aux gardes", gs => { Narrator.Say("Les gardes l'emmènent. Il ne dit rien. Il te regarde jusqu'au bout.", Color.Grey); gs.Reputation -= 10; Display.ShowEvent("-10 réputation.", Color.Red); Narrator.Pause(); }),
                            new("Lui donner quelques crédits", gs => { var d = Rng.Next(50, 150); gs.Credits = Math.Max(0, gs.Credits - d); gs.Reputation += 15; Narrator.Say("Il prend l'argent sans comprendre. Puis il court.", Color.Green); Display.ShowEvent($"-{d}cr. +15 réputation.", Color.Green); Narrator.Pause(); }),
                        ]), s);
                        return;
                    case 1:
                        Narrator.Say("Il se débat. La situation dégénère vite.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Il se fige. 'D'accord, d'accord.' Il sort ses poches. Dans la confusion il avait pris les tiennes... et les siennes.", Color.Cyan1);
                        var bonus = Rng.Next(80, 300);
                        s.Credits += bonus; Display.ShowEvent($"+{bonus}cr récupérés.", Color.Cyan1);
                        break;
                }
                Narrator.Pause();
            }),

            new("Laisser faire — voir ce qu'il prend", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        var vol = Rng.Next(100, 500);
                        s.Credits = Math.Max(0, s.Credits - vol);
                        Narrator.Say("Il disparaît dans la foule. Bien joué.", Color.Red);
                        Display.ShowEvent($"-{vol}cr.", Color.Red);
                        break;
                    case 1:
                        Narrator.Say("Il fouille. Il ne trouve rien qui l'intéresse et disparaît, déçu.", Color.Grey);
                        break;
                    case 2:
                        Narrator.Say("Il prend quelque chose mais un passant l'intercepte. Il s'enfuit. Le passant te rend l'objet.", Color.Green);
                        s.Reputation += 5; Display.ShowEvent("+5 réputation.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),

            new("Le suivre discrètement", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il te mène dans un couloir mal éclairé. Une planque. Il y a des caisses.", Color.Gold1);
                        var butin = Rng.Next(400, 1200);
                        s.Credits += butin; s.Reputation -= 10;
                        Display.ShowEvent($"+{butin}cr dans les caisses. -10 réputation.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Il t'a vu. Il t'attire dans un traquenard.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Il disparaît dans une ventilation. Impossible à suivre.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Yellow);

    static Situation PropositionLouche(GameState state) => new(
        "Un inconnu s'approche avec un sourire trop large. 'J'ai quelque chose pour toi.'",
        [
            new("Écouter ce qu'il propose", s =>
            {
                switch (Rng.Next(4))
                {
                    case 0:
                        var pay = Rng.Next(400, 1200);
                        Narrator.Say($"Une livraison discrète. Pas de questions. {pay}cr si tu la fais.", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Tu acceptes ?",
                        [
                            new("Oui", gs =>
                            {
                                if (Rng.Next(2) == 0) { gs.Credits += pay; gs.Reputation -= 15; Narrator.Say("Livraison faite. Personne n'a posé de questions. Pour l'instant.", Color.Yellow); Display.ShowEvent($"+{pay}cr. -15 réputation.", Color.Yellow); }
                                else { gs.Reputation -= 30; Narrator.Say("Les gardes t'attendaient à destination. Tu t'en sors de justesse.", Color.Red); Display.ShowEvent("-30 réputation.", Color.Red); }
                                Narrator.Pause();
                            }),
                            new("Non", _ => { Narrator.Say("'Dommage.' Il disparaît aussi vite qu'il est apparu.", Color.Grey); Narrator.Pause(); }),
                        ]), s);
                        return;
                    case 1:
                        Narrator.Say("Il te vend une 'information exclusive'. C'était du vent.", Color.Red);
                        var arnaque = Rng.Next(100, 400);
                        s.Credits = Math.Max(0, s.Credits - arnaque);
                        Display.ShowEvent($"-{arnaque}cr. Arnaque.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il veut te vendre des coordonnées. Ça semble réel.", Color.Cyan1);
                        var coords = Rng.Next(300, 900);
                        s.Credits += coords; Display.ShowEvent($"Les coordonnées mènent à une épave rentable. +{coords}cr.", Color.Cyan1);
                        break;
                    case 3:
                        Narrator.Say("Il te regarde, change d'avis, et repart. Quelque chose dans ton regard lui a déplu.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Refuser sèchement", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0: Narrator.Say("Il insiste. 'T'as pas compris. C'est pas une question.' Il bloque le couloir.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 1: Narrator.Say("'T'as tort.' Il hausse les épaules et part.", Color.Grey); break;
                    case 2: Narrator.Say("Il se vexe. 'Un jour tu regretteras.' Il disparaît.", Color.Grey); break;
                }
                Narrator.Pause();
            }),

            new("Lui proposer un contre-deal", s =>
            {
                var repBonus = s.Class.Name == "Contrebandier" ? 20 : 0;
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il réfléchit. Il accepte. Ton contre-deal était meilleur.", Color.Green);
                        var gain = Rng.Next(500, 1400);
                        s.Credits += gain; Display.ShowEvent($"+{gain}cr. Tu as bien joué.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Il se méfie. Il pense que tu veux le doubler. La conversation tourne mal.", Color.Red);
                        var perte = Rng.Next(150, 500);
                        s.Credits = Math.Max(0, s.Credits - perte); Display.ShowEvent($"-{perte}cr. La négociation a dérapé.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il éclate de rire. 'J'aime ton culot.' Il part en te faisant un geste de la main.", Color.Grey);
                        s.Reputation += 5 + repBonus; Display.ShowEvent($"+{5 + repBonus} réputation.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),

            new("L'attaquer et prendre ce qu'il a", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il ne s'y attendait pas. Tu le dépouilles avant qu'il réalise.", Color.Gold1);
                        var vol = Rng.Next(200, 700);
                        s.Credits += vol; s.Reputation -= 20;
                        Display.ShowEvent($"+{vol}cr. -20 réputation.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Il résiste mieux que prévu.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Ses amis étaient là. Tu ne les avais pas vus.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)]));
                        return;
                }
                Narrator.Pause();
            }),
        ],
        Color.Yellow);

    static Situation GardeCorrompu(GameState state) => new(
        "Un garde bloque ton passage et tend la main. Il n'explique rien. Il attend.",
        [
            new("Payer sans discuter", s =>
            {
                var montant = Rng.Next(80, 300);
                if (s.Credits < montant) { Narrator.Say("T'as pas assez. Il te regarde et appelle du renfort.", Color.Red); s.Reputation -= 10; Display.ShowEvent("-10 réputation.", Color.Red); Narrator.Pause(); return; }
                s.Credits -= montant;
                switch (Rng.Next(3))
                {
                    case 0: Narrator.Say("Il s'écarte. Personne n'a rien vu.", Color.Grey); break;
                    case 1:
                        Narrator.Say("Il prend l'argent et chuchote quelque chose. Une info utile.", Color.Cyan1);
                        s.Credits += Rng.Next(200, 600);
                        Display.ShowEvent($"L'info valait plus que le pot-de-vin.", Color.Cyan1);
                        break;
                    case 2:
                        Narrator.Say("Il prend l'argent. Et en redemande.", Color.Red);
                        s.Credits -= Math.Min(s.Credits, montant / 2);
                        Display.ShowEvent($"-{montant}cr puis -{montant / 2}cr supplémentaires.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("Refuser et le regarder dans les yeux", s =>
            {
                var intimidBonus = s.Class.Name == "Seigneur de guerre" ? 30 : s.Reputation > 100 ? 15 : 0;
                var chance = 30 + intimidBonus + Math.Max(0, s.Reputation / 10);
                if (Rng.Next(100) < chance)
                {
                    Narrator.Say("Il soutient ton regard. Puis il s'écarte. Il a vu quelque chose dans tes yeux.", Color.Green);
                    Display.ShowEvent("Tu passes sans payer.", Color.Green);
                }
                else
                {
                    Narrator.Say("Il appelle du renfort. Deux gardes supplémentaires arrivent.", Color.Red);
                    Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                    return;
                }
                Narrator.Pause();
            }),

            new("Attaquer — neutraliser rapidement", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("T'as l'avantage de la surprise. Il tombe avant d'avoir le temps de réagir.", Color.Gold1);
                        var loot = Rng.Next(150, 400);
                        s.Credits += loot; s.Reputation -= 25;
                        Display.ShowEvent($"+{loot}cr sur lui. -25 réputation.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Il résiste. D'autres gardes approchent.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Tu le mets à terre. Une alarme retentit quelque part.", Color.Red);
                        s.Reputation -= 30; Display.ShowEvent("-30 réputation. La station sait ce que tu as fait.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("Négocier — trouver un arrangement", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il écoute. Il veut des informations plutôt que des crédits.", Color.Yellow);
                        s.Reputation -= 10;
                        Display.ShowEvent("Tu lui livres des informations sur quelqu'un. Tu passes. -10 réputation.", Color.Yellow);
                        break;
                    case 1:
                        Narrator.Say("Il fait semblant de négocier. Puis il t'arrête quand même.", Color.Red);
                        s.IsImprisoned = true;
                        Narrator.Pause(); return;
                    case 2:
                        Narrator.Say("Il accepte quelques crédits et te glisse un mot d'ordre. Ça pourrait être utile plus tard.", Color.Cyan1);
                        var cout = Rng.Next(50, 200);
                        s.Credits = Math.Max(0, s.Credits - cout);
                        Display.ShowEvent($"-{cout}cr. +mot d'ordre (info locale).", Color.Cyan1);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Yellow);

    static Situation Blessé(GameState state) => new(
        "Un homme est affalé dans un couloir, blessé. Il respire encore.",
        [
            new("L'aider", s =>
            {
                var medicBonus = s.Class.Name == "Médecin" ? 2 : 1;
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il reprend conscience. Il fouille sa veste et sort une liasse. 'Prends-le. Tu en as plus besoin que moi maintenant.'", Color.Green);
                        var cr = Rng.Next(300, 900) * medicBonus;
                        s.Credits += cr; s.Reputation += 20;
                        Display.ShowEvent($"+{cr}cr. +20 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Tu l'aides à s'asseoir. Il murmure une adresse. 'Dis-leur que Varro t'envoie.'", Color.Cyan1);
                        s.Reputation += 30;
                        Display.ShowEvent("+30 réputation. Tu as un contact quelque part.", Color.Cyan1);
                        break;
                    case 2:
                        Narrator.Say("Ceux qui l'ont mis là arrivent au même moment.", Color.Red);
                        s.Reputation += 10;
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                }
                Narrator.Pause();
            }),

            new("Le fouiller discrètement", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        var vol = Rng.Next(150, 600);
                        s.Credits += vol; s.Reputation -= 15;
                        Narrator.Say("Tu prends ses crédits. Il ne bouge pas. Tu ne sais pas s'il dormait ou s'il t'a laissé faire.", Color.Red);
                        Display.ShowEvent($"+{vol}cr. -15 réputation.", Color.Red);
                        break;
                    case 1:
                        Narrator.Say("Il ouvre un œil. Il te regarde fouiller ses poches. Il ne dit rien. C'est pire.", Color.Red);
                        s.Reputation -= 25; Display.ShowEvent("-25 réputation. Des témoins.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Ses poches sont vides. Quelqu'un est passé avant toi.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Appeler des secours et partir", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Les secours arrivent vite. Quelqu'un t'a vu faire. Ça compte.", Color.Green);
                        s.Reputation += 15; Display.ShowEvent("+15 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Les 'secours' qui arrivent étaient ceux qui l'avaient tabassé. Tu te retrouves impliqué malgré toi.", Color.Red);
                        var dmg = Rng.Next(100, 400);
                        s.Credits = Math.Max(0, s.Credits - dmg);
                        Display.ShowEvent($"-{dmg}cr. Mauvaise situation.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Personne ne vient. Tu repars. C'était peut-être la bonne décision.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Passer sans t'arrêter", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0: Narrator.Say("Tu passes. Tu l'entends appeler. Tu continues.", Color.Grey); s.Reputation -= 5; Display.ShowEvent("-5 réputation.", Color.Grey); break;
                    case 1: Narrator.Say("Tu passes. Il ne dit rien. Tu te demandes s'il était déjà mort.", Color.Grey); break;
                    case 2:
                        Narrator.Say("Tu passes. Quelques heures plus tard, tu le recroises debout. Il te regarde différemment.", Color.Yellow);
                        s.Reputation -= 10; Display.ShowEvent("-10 réputation.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.SteelBlue1);

    static Situation Dispute(GameState state) => new(
        "Deux hommes se battent dans le couloir. L'un est clairement en difficulté.",
        [
            new("Intervenir pour les séparer", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Ils s'arrêtent net. Puis ils t'attaquent tous les deux. Tu avais mal choisi ton moment.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 1:
                        Narrator.Say("Le plus faible en profite pour fuir. Le plus fort se retourne vers toi. 'T'avais pas à te mêler de ça.'", Color.Red);
                        var dmg = Rng.Next(10, 30); s.PlayerHp = Math.Max(1, s.PlayerHp - dmg);
                        Display.ShowEvent($"-{dmg} PV joueur. Il t'a quand même frappé.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Ça marche. Les deux s'arrêtent, surpris. L'un d'eux te serre la main. 'C'est rare ce que t'as fait là.'", Color.Green);
                        s.Reputation += 15; Display.ShowEvent("+15 réputation.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),

            new("Aider le plus faible", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("À deux, vous retournez la situation. Le plus faible te remercie avec tout ce qu'il a.", Color.Green);
                        var cr = Rng.Next(200, 700);
                        s.Credits += cr; s.Reputation += 20;
                        Display.ShowEvent($"+{cr}cr. +20 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Tu arrives mais c'est déjà trop tard. Il perd quand même. Et maintenant le plus fort s'en prend à toi.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Le plus faible profite de ta distraction pour fuir. Le plus fort pareil. Tu t'es battu pour rien.", Color.Grey);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(5, 20));
                        Display.ShowEvent("Quelques bleus. Rien de grave.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Regarder et attendre la fin", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Le plus fort gagne. Il sort quelque chose de la poche du perdant. Il te regarde. 'T'as vu quelque chose ?'", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Tu dis quoi ?",
                        [
                            new("Non, rien du tout.", gs => { Narrator.Say("Il hoche la tête et part.", Color.Grey); Narrator.Pause(); }),
                            new("Je veux ma part.", gs => { var partage = Rng.Next(100, 400); gs.Credits += partage; gs.Reputation -= 15; Narrator.Say($"Il réfléchit. Il partage. +{partage}cr.", Color.Gold1); Display.ShowEvent($"+{partage}cr. -15 réputation.", Color.Gold1); Narrator.Pause(); }),
                        ]), s);
                        return;
                    case 1:
                        Narrator.Say("Un des deux se relève et fuit dans ta direction. Il t'entraîne dans sa fuite.", Color.Red);
                        s.Reputation -= 10; Display.ShowEvent("Tu te retrouves mêlé à quelque chose. -10 réputation.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Ils s'arrêtent d'eux-mêmes. Ils se serrent la main. C'était une dispute pour rire.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Ignorer et changer de couloir", s =>
            {
                Narrator.Say("Tu fais demi-tour. Ce n'était pas ton problème.", Color.Grey);
                if (Rng.Next(3) == 0) { s.Reputation -= 5; Display.ShowEvent("-5 réputation. Quelqu'un t'a reconnu.", Color.Grey); }
                Narrator.Pause();
            }),
        ],
        Color.OrangeRed1);

    // ── RENCONTRES MID TIER ─────────────────────────────────────────────────

    static Situation ChasseurDePrimes(GameState state) => new(
        "Un homme s'approche et t'examine de la tête aux pieds. Il a ton signalement.",
        [
            new("Nier — tu ne sais pas de quoi il parle", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il hésite. Il n'est pas sûr. Il repart en te surveillant du coin de l'œil.", Color.Yellow);
                        Display.ShowEvent("Tu as gagné du temps. Il reviendra.", Color.Yellow);
                        break;
                    case 1:
                        Narrator.Say("Il n'est pas dupe. 'T'es le seul ici à avoir cette cicatrice.' Combat.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Il croit ta version. Il s'excuse presque et part chercher ailleurs.", Color.Green);
                        Display.ShowEvent("Tu t'en sors.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),

            new("Négocier — lui proposer plus que la prime", s =>
            {
                var cout = Rng.Next(500, 1500);
                if (s.Credits < cout) { Narrator.Say($"T'as pas assez pour le convaincre. Il n'a pas envie d'attendre.", Color.Red); Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)])); return; }
                switch (Rng.Next(3))
                {
                    case 0:
                        s.Credits -= cout;
                        Narrator.Say($"Il réfléchit. Il prend l'argent. 'T'as jamais été là.' -{cout}cr.", Color.Yellow);
                        s.Reputation += 5; Display.ShowEvent($"-{cout}cr. Tu passes.", Color.Yellow);
                        break;
                    case 1:
                        s.Credits -= cout;
                        Narrator.Say("Il prend l'argent. Et il t'arrête quand même. 'La prime est plus haute.'", Color.Red);
                        s.IsImprisoned = true; Narrator.Pause(); return;
                    case 2:
                        Narrator.Say("Il te propose un deal différent. Travailler pour lui une fois.", Color.Cyan1);
                        s.Reputation -= 20;
                        var gain = Rng.Next(800, 2000);
                        s.Credits += gain; Display.ShowEvent($"Tu acceptes. +{gain}cr. -20 réputation.", Color.Cyan1);
                        break;
                }
                Narrator.Pause();
            }),

            new("Attaquer le premier", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il ne s'y attendait pas. Tu neutralises la menace rapidement.", Color.Gold1);
                        var loot = Rng.Next(300, 900);
                        s.Credits += loot; s.Reputation -= 20;
                        Display.ShowEvent($"+{loot}cr. -20 réputation.", Color.Gold1);
                        break;
                    case 1:
                    case 2:
                        Narrator.Say("Il est entraîné. Tu l'avais sous-estimé.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)]));
                        return;
                }
                Narrator.Pause();
            }),

            new("Se rendre", s =>
            {
                Narrator.Say("Tu lèves les mains. Il hoche la tête.", Color.Grey);
                switch (Rng.Next(3))
                {
                    case 0:
                        s.IsImprisoned = true;
                        Narrator.Say("Tu te retrouves derrière des barreaux.", Color.Red);
                        Narrator.Pause(); return;
                    case 1:
                        var caution = Rng.Next(400, 1200);
                        if (s.Credits >= caution) { s.Credits -= caution; Narrator.Say($"Il accepte une caution sur place. -{caution}cr.", Color.Yellow); Display.ShowEvent($"-{caution}cr. Tu restes libre.", Color.Yellow); }
                        else { s.IsImprisoned = true; Narrator.Say("T'as pas assez. Il t'emmène.", Color.Red); }
                        break;
                    case 2:
                        Narrator.Say("Il lit ton dossier. 'T'es pas celui que je cherche.' Il repart.", Color.Green);
                        Display.ShowEvent("Mauvaise identité. Tu passes.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Red);

    static Situation ContrebandierCoins(GameState state) => new(
        "Un homme dans l'ombre t'accoste. Il a besoin de quelqu'un pour transporter quelque chose. Pas de questions.",
        [
            new("Accepter sans poser de questions", s =>
            {
                var paye = Rng.Next(600, 2000);
                switch (Rng.Next(3))
                {
                    case 0:
                        s.Credits += paye; s.Reputation -= 15;
                        Narrator.Say($"Livraison faite. L'argent est là. Ce que t'as transporté, tu préfères pas savoir.", Color.Yellow);
                        Display.ShowEvent($"+{paye}cr. -15 réputation.", Color.Yellow);
                        break;
                    case 1:
                        Narrator.Say("La douane t'attendait à destination. Quelqu'un a vendu l'information.", Color.Red);
                        s.Reputation -= 35; s.IsImprisoned = true;
                        Narrator.Pause(); return;
                    case 2:
                        s.Credits += paye * 2; s.Reputation -= 10;
                        Narrator.Say("Le destinataire était si content qu'il a doublé la mise.", Color.Gold1);
                        Display.ShowEvent($"+{paye * 2}cr. -10 réputation.", Color.Gold1);
                        break;
                }
                Narrator.Pause();
            }),

            new("Demander ce qu'il faut transporter", s =>
            {
                Narrator.Say("Il hésite. Puis : 'Des informations. Rien de physique.'", Color.Yellow);
                switch (Rng.Next(3))
                {
                    case 0:
                        var cr = Rng.Next(500, 1500);
                        s.Credits += cr; s.Reputation -= 5;
                        Narrator.Say("Transaction sans accroc.", Color.Green);
                        Display.ShowEvent($"+{cr}cr. -5 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Les informations concernent quelqu'un que tu as croisé. Ça te rend complice de quelque chose.", Color.Red);
                        s.Reputation -= 25; Display.ShowEvent("-25 réputation.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il te ment. Les 'informations' sont gravées sur un objet physique très illégal.", Color.Red);
                        s.Reputation -= 20; Display.ShowEvent("-20 réputation. T'as failli te faire prendre.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("Refuser et partir", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0: Narrator.Say("Il accepte ton refus. 'Je trouverai quelqu'un d'autre.'", Color.Grey); break;
                    case 1:
                        Narrator.Say("Il insiste. Tu refuses encore. Il devient menaçant.", Color.Red);
                        var perte = Rng.Next(100, 400);
                        s.Credits = Math.Max(0, s.Credits - perte);
                        Display.ShowEvent($"-{perte}cr. Il a pris de quoi 'compenser son temps'.", Color.Red);
                        break;
                    case 2: Narrator.Say("Il disparaît. Comme s'il n'avait jamais été là.", Color.Grey); break;
                }
                Narrator.Pause();
            }),

            new("Le dénoncer aux gardes", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Les gardes l'appréhendent. Ils te remercient avec une petite récompense.", Color.Green);
                        var reward = Rng.Next(200, 600);
                        s.Credits += reward; s.Reputation += 10;
                        Display.ShowEvent($"+{reward}cr. +10 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Les gardes le laissent partir. Ils sont dans son camp. Tu t'es fait un ennemi pour rien.", Color.Red);
                        s.Reputation -= 20; Display.ShowEvent("-20 réputation.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il t'a vu aller voir les gardes. Tu le retrouveras certainement au mauvais moment.", Color.Red);
                        s.Reputation -= 15; Display.ShowEvent("-15 réputation. Il sait.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.OrangeRed1);

    // ── NOUVELLES RENCONTRES ────────────────────────────────────────────────

    static Situation Dealer(GameState state) => new(
        "Un type s'approche dans un couloir sombre. Il ouvre la main. Des comprimés, de la poudre, quelque chose en tube.",
        [
            new("Acheter et consommer sur place", s =>
            {
                var cost = Rng.Next(80, 250);
                if (s.Credits < cost) { Narrator.Say("T'as même pas de quoi te payer l'oubli. Il referme la main.", Color.Grey); Narrator.Pause(); return; }
                s.Credits -= cost;
                // Progression de l'addiction
                s.AddictionLevel++;
                s.AddictionDaysSinceDose = 0;
                if (s.AddictionLevel > 1)
                    Display.ShowEvent($"Addiction : {s.AddictionLabel} (coût quotidien : {s.AddictionDailyCost}cr)", Color.OrangeRed1);
                switch (Rng.Next(5))
                {
                    case 0:
                        Narrator.Say("Ça monte vite. Trop vite. Tu vois des choses que t'aurais préféré ne pas voir. Ou peut-être que c'était réel.", Color.Magenta1);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(10, 30));
                        s.Reputation += 5;
                        Display.ShowEvent($"-{cost}cr. -{Rng.Next(10, 30)} PV. Dans un état second t'as dit des choses qui ont impressionné les gens.", Color.Magenta1);
                        break;
                    case 1:
                        Narrator.Say("T'as perdu quatre heures. Quelqu'un t'a soutenu quelque chose pendant ce temps.", Color.Red);
                        var vol = Rng.Next(200, 700);
                        s.Credits = Math.Max(0, s.Credits - vol);
                        Display.ShowEvent($"-{cost}cr. -{vol}cr volés pendant ta descente.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Clarity absolue pendant deux heures. Tu vois les connexions, les routes, les patterns. Puis ça s'arrête et t'as juste mal partout.", Color.Cyan1);
                        var gain = Rng.Next(300, 900);
                        s.Credits += gain;
                        Display.ShowEvent($"-{cost}cr. +{gain}cr de décisions prises dans cet état.", Color.Cyan1);
                        break;
                    case 3:
                        Narrator.Say("Réaction allergique sévère. T'arrives à te traîner jusqu'à un couloir passant avant de t'effondrer.", Color.Red);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(30, 60));
                        Display.ShowEvent($"-{cost}cr. PV joueur critiques.", Color.Red);
                        break;
                    case 4:
                        Narrator.Say("T'as dormi seize heures. Tu te réveilles avec l'envie de recommencer. C'est probablement mauvais signe.", Color.Yellow);
                        s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 20);
                        Display.ShowEvent($"-{cost}cr. +20 PV. Une envie de recommencer.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),

            new("Acheter pour revendre", s =>
            {
                var cost = Rng.Next(150, 400);
                if (s.Credits < cost) { Narrator.Say("Pas assez pour investir.", Color.Grey); Narrator.Pause(); return; }
                s.Credits -= cost;
                s.Cargo.Add("Marchandises illégales", 1);
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("T'as la marchandise. Reste à trouver un acheteur discret.", Color.Yellow);
                        Display.ShowEvent($"-{cost}cr. +1 Marchandises illégales dans la cargaison.", Color.Yellow);
                        break;
                    case 1:
                        Narrator.Say("Un garde a vu l'échange. Il note quelque chose. Il ne t'arrête pas. Pas encore.", Color.Red);
                        s.Reputation -= 15;
                        Display.ShowEvent($"-{cost}cr. +1 Marchandises illégales. -15 réputation.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Le dealer t'a refilé de la marchandise coupée. Elle vaudra moins à la revente.", Color.Grey);
                        Display.ShowEvent($"-{cost}cr. +1 Marchandises illégales (qualité douteuse).", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Dénoncer le dealer aux gardes", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Les gardes l'arrêtent. Une récompense officieuse t'est remise.", Color.Green);
                        var r = Rng.Next(200, 500);
                        s.Credits += r; s.Reputation += 10;
                        Display.ShowEvent($"+{r}cr. +10 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Les gardes sont sur la liste de ses clients. Ils le laissent partir et te regardent d'un mauvais œil.", Color.Red);
                        s.Reputation -= 20;
                        Display.ShowEvent("-20 réputation. T'as dénoncé quelqu'un à ses complices.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il a disparu avant que les gardes arrivent. Il sait que c'est toi. Il se souviendra.", Color.Yellow);
                        s.Reputation -= 10;
                        Display.ShowEvent("-10 réputation. Il reviendra.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),

            new("Ignorer et passer", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0: Narrator.Say("Il hausse les épaules. 'La prochaine fois peut-être.'", Color.Grey); break;
                    case 1:
                        Narrator.Say("Il te suit un moment. Il insiste. Tu finis par lui payer quelque chose juste pour qu'il parte.", Color.Red);
                        var perte = Rng.Next(50, 150);
                        s.Credits = Math.Max(0, s.Credits - perte);
                        Display.ShowEvent($"-{perte}cr. Le prix de la tranquillité.", Color.Red);
                        break;
                    case 2: Narrator.Say("Il ne te dit rien. Mais il te regarde partir comme s'il savait que tu reviendrais.", Color.Grey); break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Magenta1);

    static Situation BarFight(GameState state) => new(
        "Quelqu'un reverse son verre sur toi dans un bar bondé. Il ne s'excuse pas. Il attend de voir comment tu réagis.",
        [
            new("Répondre — le regarder dans les yeux", s =>
            {
                var intimidBonus = s.Class.Name == "Seigneur de guerre" ? 40 : s.Class.Name == "Vétéran" ? 20 : 0;
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il recule. Deux secondes plus tard il s'excuse et offre la tournée. Tout le monde a vu.", Color.Green);
                        s.Reputation += 15;
                        Display.ShowEvent("+15 réputation. Tu domines la salle.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Il appelle ses amis. La bagarre part vite.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                    case 2:
                        Narrator.Say("La tension monte. Le barman intervient. L'autre type est mis dehors. On t'offre un verre.", Color.Cyan1);
                        s.Credits -= Rng.Next(20, 60);
                        s.Reputation += 10;
                        Display.ShowEvent("+10 réputation. Verre offert.", Color.Cyan1);
                        break;
                }
                Narrator.Pause();
            }),

            new("Avaler l'humiliation et repartir", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Tu pars. Quelqu'un t'a vu faire. Il te rattrape dehors : 'Smart move. Ces gars-là tuent pour moins que ça.' Il te glisse quelque chose.", Color.Yellow);
                        var cr = Rng.Next(200, 600);
                        s.Credits += cr;
                        Display.ShowEvent($"+{cr}cr. Parfois la sagesse paie.", Color.Yellow);
                        break;
                    case 1:
                        Narrator.Say("Tu pars. Ils rient dans ton dos. Tu l'entends depuis le couloir.", Color.Grey);
                        s.Reputation -= 10;
                        Display.ShowEvent("-10 réputation. Le bar se souviendra.", Color.Grey);
                        break;
                    case 2:
                        Narrator.Say("Tu pars. L'autre type te suit pour finir l'histoire.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                }
                Narrator.Pause();
            }),

            new("Offrir un verre en retour — désamorcer", s =>
            {
                var cost = Rng.Next(30, 100);
                s.Credits = Math.Max(0, s.Credits - cost);
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say($"Il est surpris. Deux heures après vous êtes les meilleurs amis du monde. Il te parle d'un deal.", Color.Green);
                        var deal = Rng.Next(300, 900);
                        s.Credits += deal;
                        Display.ShowEvent($"-{cost}cr pour le verre. +{deal}cr du deal.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say($"Il prend le verre. Il en commande un autre à ton compte. Et encore un. La soirée coûte cher.", Color.Yellow);
                        var extra = Rng.Next(100, 400);
                        s.Credits = Math.Max(0, s.Credits - extra);
                        Display.ShowEvent($"-{cost + extra}cr total. Tu t'en tires sans violence.", Color.Yellow);
                        break;
                    case 2:
                        Narrator.Say($"Il refuse. 'J'ai pas besoin de ta pitié.' Ça repart.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierLow[Rng.Next(Combat.TierLow.Count)]));
                        return;
                }
                Narrator.Pause();
            }),

            new("Lui fracasser le verre sur la table et attendre", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Silence dans le bar. Il ne bouge plus. Il part sans un mot. La salle reprend ses conversations.", Color.Gold1);
                        s.Reputation += 20;
                        Display.ShowEvent("+20 réputation.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Ça dégénère instantanément. Tout le monde tape sur tout le monde.", Color.Red);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(15, 40));
                        var loot = Rng.Next(50, 300);
                        s.Credits += loot;
                        Display.ShowEvent($"Bagarre générale. -{Rng.Next(15, 40)} PV. +{loot}cr tombés des poches pendant le chaos.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Le barman t'expulse. Les deux.", Color.Grey);
                        s.Reputation -= 5;
                        Display.ShowEvent("-5 réputation. Expulsé.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.OrangeRed1);

    static Situation Overdose(GameState state) => new(
        "Quelqu'un est effondré dans un coin. Il respire à peine. Il y a une seringue cassée à côté de lui.",
        [
            new("Intervenir — essayer de le ranimer", s =>
            {
                var medicBonus = s.Class.Name == "Médecin";
                switch (Rng.Next(medicBonus ? 2 : 3))
                {
                    case 0:
                        Narrator.Say("Tu le retournes. Il reprend conscience lentement. Il te regarde sans comprendre qui tu es ni ce qui s'est passé.", Color.Green);
                        if (s.Cargo.Get("Médicaments") > 0) { s.Cargo.Remove("Médicaments", 1); Narrator.Say("Tu utilises un médicament. Ça fait la différence.", Color.Green); }
                        s.Reputation += 20;
                        Display.ShowEvent("+20 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("C'est trop tard. Tu ne pouvais rien faire. Mais des gens ont vu que t'as essayé.", Color.Grey);
                        s.Reputation += 5;
                        Display.ShowEvent("+5 réputation. T'as essayé.", Color.Grey);
                        break;
                    case 2:
                        Narrator.Say("Il se réveille en panique. Il croit que t'es là pour le voler. Il appelle au secours.", Color.Red);
                        s.Reputation -= 15;
                        Display.ShowEvent("-15 réputation. Mauvais timing.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("Le fouiller pendant qu'il est inconscient", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        var cr = Rng.Next(100, 500);
                        s.Credits += cr; s.Reputation -= 20;
                        Narrator.Say("T'as pris ses crédits. Il vivra peut-être ou peut-être pas. T'étais déjà loin.", Color.Red);
                        Display.ShowEvent($"+{cr}cr. -20 réputation.", Color.Red);
                        break;
                    case 1:
                        s.Cargo.Add("Marchandises illégales", 1); s.Reputation -= 10;
                        Narrator.Say("T'as trouvé sa réserve. Maintenant c'est la tienne.", Color.Yellow);
                        Display.ShowEvent("+1 Marchandises illégales. -10 réputation.", Color.Yellow);
                        break;
                    case 2:
                        Narrator.Say("Il a rien. Ses poches sont vides. Quelqu'un est passé avant toi.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Appeler du secours et partir vite", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Les secours arrivent. Quelqu'un t'a vu signaler. +réputation.", Color.Green);
                        s.Reputation += 15;
                        Display.ShowEvent("+15 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Les 'secours' sont ses fournisseurs. Ils pensent que t'as quelque chose à voir avec son état. Ils t'interrogent.", Color.Red);
                        var perdu = Rng.Next(200, 600);
                        s.Credits = Math.Max(0, s.Credits - perdu);
                        Display.ShowEvent($"-{perdu}cr pour fermer les questions.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Personne ne vient. Tu passes.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Continuer à marcher — t'en mêler c'est des ennuis", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Tu passes. Deux heures après, tu l'entends parler dans le couloir derrière toi. Il a survécu. Il ne sait pas que t'as rien fait.", Color.Grey);
                        break;
                    case 1:
                        Narrator.Say("Tu passes. Le lendemain, quelqu'un te dit qu'il est mort. L'info ne devrait rien te faire. Elle fait quand même quelque chose.", Color.Grey);
                        s.Reputation -= 5;
                        Display.ShowEvent("-5 réputation. Des gens t'ont vu passer.", Color.Grey);
                        break;
                    case 2:
                        Narrator.Say("Tu passes. Un garde était dans le couloir et t'a vu passer sans t'arrêter. Il note quelque chose.", Color.Yellow);
                        s.Reputation -= 10;
                        Display.ShowEvent("-10 réputation.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Grey);

    static Situation AlcoolBar(GameState state) => new(
        "Tu passes devant un bar. L'odeur de l'alcool bon marché et du bruit humain t'attire ou te repousse selon les jours. Aujourd'hui ?",
        [
            new("Entrer et boire un verre — juste un", s =>
            {
                var cost = Rng.Next(30, 80);
                s.Credits = Math.Max(0, s.Credits - cost);
                switch (Rng.Next(4))
                {
                    case 0:
                        Narrator.Say("Tu bois. Un voisin de comptoir commence à parler. T'aurais pas dû l'écouter mais l'info valait quelque chose.", Color.Cyan1);
                        var info = Rng.Next(300, 900);
                        s.Credits += info;
                        Display.ShowEvent($"-{cost}cr. +{info}cr d'une info glanée au bar.", Color.Cyan1);
                        break;
                    case 1:
                        Narrator.Say("Un verre devient trois. Tu repars avec la tête qui tourne et la conviction que t'as pris de bonnes décisions.", Color.Yellow);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(5, 15));
                        Display.ShowEvent($"-{cost * 3}cr. -PV. On verra demain.", Color.Yellow);
                        s.Credits = Math.Max(0, s.Credits - cost * 2);
                        break;
                    case 2:
                        Narrator.Say("Tu bois. Le barman est bavard. Il te parle d'un client bizarre qui cherche quelqu'un pour un job discret.", Color.Yellow);
                        Display.ShowEvent($"-{cost}cr. Un contact potentiel.", Color.Yellow);
                        s.Reputation += 5;
                        break;
                    case 3:
                        Narrator.Say("Tu bois seul. Rien ne se passe. C'est exactement ce dont t'avais besoin.", Color.Grey);
                        s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 10);
                        Display.ShowEvent($"-{cost}cr. +10 PV. Un peu de calme.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Entrer et boire jusqu'à ne plus se souvenir", s =>
            {
                var cost = Rng.Next(200, 600);
                s.Credits = Math.Max(0, s.Credits - cost);
                // L'alcool en excès développe aussi une addiction
                if (Rng.Next(3) == 0)
                {
                    s.AddictionLevel++;
                    s.AddictionDaysSinceDose = 0;
                    if (s.AddictionLevel > 1)
                        Display.ShowEvent($"L'abus d'alcool commence à te coûter. Addiction : {s.AddictionLabel} ({s.AddictionDailyCost}cr/jour)", Color.OrangeRed1);
                }
                switch (Rng.Next(4))
                {
                    case 0:
                        Narrator.Say("Tu te réveilles dans un couloir que tu reconnais pas. T'as plus ton portefeuille. T'as un tatouage que t'avais pas avant.", Color.Red);
                        var vol = Rng.Next(300, 800);
                        s.Credits = Math.Max(0, s.Credits - vol);
                        Display.ShowEvent($"-{cost}cr de boisson. -{vol}cr volés. Un tatouage.", Color.Red);
                        break;
                    case 1:
                        Narrator.Say("Tu te réveilles avec un contrat signé dans ta poche. Tu ne te souviens pas de l'avoir signé. Il semble... profitable.", Color.Gold1);
                        var gain = Rng.Next(500, 1500);
                        s.Credits += gain;
                        Display.ShowEvent($"-{cost}cr. +{gain}cr du contrat mystère.", Color.Gold1);
                        break;
                    case 2:
                        Narrator.Say("Tu te réveilles en prison. Manifestement t'as fait quelque chose. Les gardes ne veulent pas t'expliquer quoi.", Color.Red);
                        s.IsImprisoned = true;
                        Narrator.Pause(); return;
                    case 3:
                        Narrator.Say("Tu te réveilles dans ton vaisseau. Quelqu'un t'y a ramené. Il y a un mot. 'La prochaine fois bois moins vite.' Pas de signature.", Color.Grey);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(10, 25));
                        Display.ShowEvent($"-{cost}cr. -PV. Quelqu'un t'a aidé.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Commander une tournée générale", s =>
            {
                var cost = Rng.Next(300, 800);
                if (s.Credits < cost) { Narrator.Say("T'as pas assez pour jouer le grand seigneur.", Color.Grey); Narrator.Pause(); return; }
                s.Credits -= cost;
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Le bar t'acclame. Tu deviens brièvement quelqu'un. Deux inconnus te parlent d'une opportunité chacun.", Color.Gold1);
                        s.Reputation += 25;
                        var retour = Rng.Next(400, 1200);
                        s.Credits += retour;
                        Display.ShowEvent($"-{cost}cr. +{retour}cr d'opportunités. +25 réputation.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Le bar t'acclame. Jusqu'à ce qu'une bagarre parte pour une raison que tu comprends pas.", Color.Red);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - Rng.Next(15, 35));
                        Display.ShowEvent($"-{cost}cr. -{Rng.Next(15, 35)} PV. La fête a mal tourné.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Le bar t'acclame. T'es entouré de nouveaux amis qui boivent très vite et disparaissent quand la note arrive.", Color.Yellow);
                        var sup = Rng.Next(100, 400);
                        s.Credits = Math.Max(0, s.Credits - sup);
                        Display.ShowEvent($"-{cost + sup}cr total. Popularité éphémère.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),

            new("Passer son chemin", s =>
            {
                Narrator.Say("Tu passes. Le bruit décroît derrière toi.", Color.Grey);
                Narrator.Pause();
            }),
        ],
        Color.Gold1);

    static Situation Fugitif(GameState state) => new(
        "Quelqu'un te bouscule en courant et murmure 'cache-moi' avant que tu aies le temps de répondre. Des gardes approchent.",
        [
            new("Le cacher — jouer le jeu", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Les gardes passent sans s'arrêter. Le fugitif attend que leurs pas s'éloignent, puis sort quelque chose de sa veste.", Color.Green);
                        var cr = Rng.Next(400, 1200);
                        s.Credits += cr; s.Reputation += 10;
                        Display.ShowEvent($"+{cr}cr. +10 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Un garde s'arrête. Il te regarde. Il regarde la direction d'où venait le fugitif. 'Tu as vu quelqu'un ?'", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Tu dis quoi ?",
                        [
                            new("Non.", gs =>
                            {
                                if (Rng.Next(2) == 0) { Narrator.Say("Il te croit. Il repart.", Color.Grey); gs.Reputation += 5; }
                                else { Narrator.Say("Il ne te croit pas. Il fouille. Il trouve le fugitif.", Color.Red); gs.Reputation -= 20; gs.IsImprisoned = true; }
                                Narrator.Pause();
                            }),
                            new("Oui — par là.", gs =>
                            {
                                Narrator.Say("Le garde part dans la mauvaise direction. Le fugitif disparaît. Une heure plus tard, il te laisse quelque chose.", Color.Cyan1);
                                var r = Rng.Next(300, 800);
                                gs.Credits += r; gs.Reputation -= 5;
                                Display.ShowEvent($"+{r}cr. -5 réputation.", Color.Cyan1);
                                Narrator.Pause();
                            }),
                        ]), s);
                        return;
                    case 2:
                        Narrator.Say("Les gardes t'emmènent tous les deux pour vérification. Une heure de perdus, quelques crédits pour fermer l'affaire.", Color.Red);
                        var cout = Rng.Next(150, 500);
                        s.Credits = Math.Max(0, s.Credits - cout);
                        Display.ShowEvent($"-{cout}cr. T'étais au mauvais endroit.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("Le livrer aux gardes", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Les gardes le prennent. Ils te remercient avec une prime.", Color.Green);
                        var prime = Rng.Next(200, 600);
                        s.Credits += prime; s.Reputation -= 15;
                        Display.ShowEvent($"+{prime}cr. -15 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Les gardes le prennent. Ils te regardent avec un peu trop d'intérêt. 'Et toi, t'étais où ce soir ?'", Color.Yellow);
                        s.Reputation -= 10;
                        Display.ShowEvent("-10 réputation. Tu t'es attiré de l'attention.", Color.Yellow);
                        break;
                    case 2:
                        Narrator.Say("Le fugitif te regarde en étant emmené. 'Je t'oublierai pas.' Ce n'est pas une promesse rassurante.", Color.Red);
                        s.Reputation -= 25;
                        Display.ShowEvent("-25 réputation. T'as des ennemis.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("L'ignorer — faire semblant de rien", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Les gardes passent. Toi aussi. Le fugitif disparaît dans la foule. Personne ne sait que tu étais là.", Color.Grey);
                        break;
                    case 1:
                        Narrator.Say("Un garde te remarque. 'Toi, t'as vu quelqu'un passer ?' Tu joues l'ignorant. Il insiste.", Color.Yellow);
                        s.Reputation -= 5;
                        Display.ShowEvent("-5 réputation. On t'a noté.", Color.Yellow);
                        break;
                    case 2:
                        Narrator.Say("Le fugitif te laisse quelque chose dans la poche en passant. Tu t'en aperçois dix minutes après.", Color.Cyan1);
                        s.Cargo.Add("Marchandises illégales", 1);
                        Display.ShowEvent("+1 Marchandises illégales. Un cadeau non demandé.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Yellow);

    static Situation TentationCaisse(GameState state) => new(
        "Une caisse ouverte et non surveillée dans un couloir. Personne autour. Enfin... apparemment.",
        [
            new("Prendre ce qu'il y a dedans", s =>
            {
                switch (Rng.Next(4))
                {
                    case 0:
                        var cr = Rng.Next(200, 800);
                        s.Credits += cr; s.Reputation -= 10;
                        Narrator.Say("T'as pris l'argent. Il n'y avait personne. Ou alors il y avait quelqu'un et cette personne a choisi de ne rien faire.", Color.Gold1);
                        Display.ShowEvent($"+{cr}cr. -10 réputation.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("C'était un piège. Les gardes sortent de derrière des panneaux.", Color.Red);
                        s.Reputation -= 30; s.IsImprisoned = true;
                        Narrator.Pause(); return;
                    case 2:
                        s.Cargo.Add("Pièces techniques", 2);
                        Narrator.Say("Des pièces techniques. Pas de crédits mais c'est utile.", Color.Cyan1);
                        Display.ShowEvent("+2 Pièces techniques.", Color.Cyan1);
                        break;
                    case 3:
                        Narrator.Say("La caisse est vide. Quelqu'un est passé avant toi. Ou alors c'est la caisse de quelqu'un qui n'a rien.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Regarder autour — vérifier si quelqu'un surveille", s =>
            {
                Narrator.Say("Tu regardes. Dans un couloir perpendiculaire, quelqu'un te regarde regarder.", Color.Yellow);
                ChoiceMenu.Resolve(new Situation("Qu'est-ce que tu fais ?",
                [
                    new("Prendre quand même", gs =>
                    {
                        var cr = Rng.Next(150, 600);
                        gs.Credits += cr; gs.Reputation -= 20;
                        Narrator.Say("Tu prends. Il a vu. Il note quelque chose.", Color.Red);
                        Display.ShowEvent($"+{cr}cr. -20 réputation.", Color.Red); Narrator.Pause();
                    }),
                    new("Repartir sans toucher à rien", gs =>
                    {
                        Narrator.Say("Tu repars. L'autre hoche la tête. Il ne sait pas si tu es honnête ou prudent. Lui non plus n'y touche pas.", Color.Grey);
                        gs.Reputation += 5; Display.ShowEvent("+5 réputation.", Color.Grey); Narrator.Pause();
                    }),
                    new("Signaler la caisse au surveillant", gs =>
                    {
                        switch (Rng.Next(2))
                        {
                            case 0: gs.Reputation += 15; Narrator.Say("C'était la caisse du responsable de quai. Il te remercie.", Color.Green); Display.ShowEvent("+15 réputation.", Color.Green); break;
                            case 1: var r = Rng.Next(100, 400); gs.Credits += r; gs.Reputation += 10; Narrator.Say("Petite récompense pour l'honnêteté.", Color.Green); Display.ShowEvent($"+{r}cr. +10 réputation.", Color.Green); break;
                        }
                        Narrator.Pause();
                    }),
                ]), s);
                return;
            }),

            new("Passer sans s'arrêter", s =>
            {
                Narrator.Say("Tu passes. Ce n'était probablement rien.", Color.Grey);
                if (Rng.Next(4) == 0) { s.Reputation += 5; Display.ShowEvent("+5 réputation. Quelqu'un t'a vu ignorer la caisse.", Color.Green); }
                Narrator.Pause();
            }),
        ],
        Color.Yellow);

    static Situation NuitPerdue(GameState state) => new(
        "Tu te réveilles quelque part sans te souvenir exactement comment t'es arrivé là.",
        [
            new("Essayer de reconstruire ce qui s'est passé", s =>
            {
                switch (Rng.Next(5))
                {
                    case 0:
                        Narrator.Say("T'as apparemment signé un accord commercial dans ton sommeil. Il tient la route juridiquement.", Color.Gold1);
                        var gain = Rng.Next(500, 2000);
                        s.Credits += gain;
                        Display.ShowEvent($"+{gain}cr. La nuit a été productive.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("T'as une cicatrice que t'avais pas avant. Elle est propre. Ça a été fait par quelqu'un qui savait ce qu'il faisait. Tu te souviens vaguement d'un deal.", Color.Red);
                        s.PlayerHp = Math.Max(1, s.PlayerHp - 15);
                        var mystère = Rng.Next(300, 1000);
                        s.Credits += mystère;
                        Display.ShowEvent($"-15 PV. +{mystère}cr. Tu préfères peut-être ne pas savoir.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("T'as rencontré quelqu'un d'important. Manifestement. Il y a un numéro de contact dans ta poche et un mot : 'Rappelle-moi.'", Color.Cyan1);
                        s.Reputation += 30;
                        Display.ShowEvent("+30 réputation. Un contact puissant.", Color.Cyan1);
                        break;
                    case 3:
                        Narrator.Say("Ta cargaison a diminué. Quelqu'un a prélevé sa dîme pendant que tu dormais ou pendant ce que tu faisais à la place de dormir.", Color.Red);
                        if (s.Cargo.All.Any())
                        {
                            var item = s.Cargo.All.Keys.First();
                            s.Cargo.Remove(item, 1);
                            Display.ShowEvent($"-1 {item}. Disparu dans la nuit.", Color.Red);
                        }
                        else Display.ShowEvent("T'avais rien. Au moins ça.", Color.Grey);
                        break;
                    case 4:
                        Narrator.Say("Apparemment t'as passé la nuit à aider des gens. Tu te souviens de rien mais tout le monde ici semble te connaître.", Color.Green);
                        s.Reputation += 20;
                        Display.ShowEvent("+20 réputation. La nuit blanche du héros anonyme.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),

            new("Repartir sans chercher à comprendre", s =>
            {
                Narrator.Say("Certaines nuits ne méritent pas d'explication. Tu repars.", Color.Grey);
                var roll = Rng.Next(3);
                if (roll == 0) { s.Credits = Math.Max(0, s.Credits - Rng.Next(100, 400)); Display.ShowEvent("T'as moins de crédits qu'hier. C'est tout ce que tu sais.", Color.Grey); }
                else if (roll == 1) { s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 15); Display.ShowEvent("Tu te sens étonnamment bien. La nuit a dû être bonne.", Color.Green); }
                else Display.ShowEvent("Tout est en ordre. Mystérieusement en ordre.", Color.Grey);
                Narrator.Pause();
            }),

            new("Fouiller l'endroit où tu t'es réveillé", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        var loot = Rng.Next(200, 700);
                        s.Credits += loot;
                        Narrator.Say("Sous le matelas, dans les fissures du mur, dans un vase. L'endroit était un dépôt.", Color.Gold1);
                        Display.ShowEvent($"+{loot}cr.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Tu trouves une note avec ton nom dessus. Le message n'est pas une bonne nouvelle.", Color.Red);
                        s.Reputation -= 20;
                        Display.ShowEvent("-20 réputation. Quelqu'un te cherche.", Color.Red);
                        break;
                    case 2:
                        s.Cargo.Add("Artefacts", 1);
                        Narrator.Say("Un objet que tu ne te souviens pas d'avoir pris. Il a l'air précieux.", Color.Cyan1);
                        Display.ShowEvent("+1 Artefact.", Color.Cyan1);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Magenta1);

    // ── RENCONTRES MID/HIGH TIER ────────────────────────────────────────────

    static Situation Chantage(GameState state) => new(
        "Un inconnu s'assoit en face de toi. Il pose une enveloppe sur la table. 'J'ai des informations sur toi. Des vraies.'",
        [
            new("Payer pour faire taire ça", s =>
            {
                var demande = Rng.Next(500, 2000);
                if (s.Credits < demande) { Narrator.Say($"T'as pas assez. Il ricane. 'Je reviendrai quand tu auras eu le temps de travailler.'", Color.Red); s.Reputation -= 15; Display.ShowEvent("-15 réputation.", Color.Red); Narrator.Pause(); return; }
                s.Credits -= demande;
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il prend l'argent et remet l'enveloppe. T'as acheté du temps, pas une solution.", Color.Yellow);
                        Display.ShowEvent($"-{demande}cr. Le problème n'est peut-être pas réglé.", Color.Yellow);
                        break;
                    case 1:
                        Narrator.Say("Il prend l'argent. Il revient deux jours plus tard avec une deuxième enveloppe.", Color.Red);
                        s.Reputation -= 10;
                        Display.ShowEvent($"-{demande}cr. Il reviendra.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il prend l'argent. Il disparaît. Peut-être pour de bon.", Color.Grey);
                        Display.ShowEvent($"-{demande}cr. Peut-être réglé.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Appeler son bluff — l'enveloppe ne contient probablement rien", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il ouvre l'enveloppe. Elle est vide. Il rougit. Il part. T'avais raison.", Color.Green);
                        s.Reputation += 10;
                        Display.ShowEvent("+10 réputation. T'as lu les gens correctement.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Il ouvre l'enveloppe. Ce n'est pas rien. T'aurais préféré que ce soit rien.", Color.Red);
                        s.Reputation -= 35;
                        Display.ShowEvent("-35 réputation. L'information est maintenant publique.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il répond : 'Ça, c'est ce que tu crois.' Il repart sans ouvrir l'enveloppe. Tu te demanderas.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Le menacer à ton tour", s =>
            {
                var intimidChance = s.Class.Name == "Seigneur de guerre" ? 60 : 30 + Math.Max(0, s.Reputation / 10);
                if (Rng.Next(100) < intimidChance)
                {
                    Narrator.Say("Il réfléchit. Il repart en laissant l'enveloppe. Ta réputation a parlé avant toi.", Color.Green);
                    s.Reputation += 10;
                    Display.ShowEvent("+10 réputation.", Color.Green);
                }
                else
                {
                    Narrator.Say("Il n'est pas impressionné. Il diffuse l'information.", Color.Red);
                    s.Reputation -= 40;
                    Display.ShowEvent("-40 réputation.", Color.Red);
                }
                Narrator.Pause();
            }),

            new("L'attaquer — récupérer l'enveloppe de force", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Tu récupères l'enveloppe. Il s'en va vite. Copie ou pas copie, tu ne sauras jamais.", Color.Gold1);
                        s.Reputation -= 15;
                        Display.ShowEvent("-15 réputation. Le problème est peut-être réglé.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Il avait de la compagnie. La situation empire rapidement.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)]));
                        return;
                    case 2:
                        Narrator.Say("Il fuit. L'enveloppe tombe. Elle contient des photos compromettantes. Pas les tiennes.", Color.Cyan1);
                        s.Reputation += 5;
                        Display.ShowEvent("+5 réputation. Tu as les photos de quelqu'un d'autre.", Color.Cyan1);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Red);

    static Situation MeurtreCommandité(GameState state) => new(
        "On te fait asseoir dans une pièce sans fenêtre. Un homme que tu n'as jamais vu te tend une photo. Une autre personne. Un prix.",
        [
            new("Accepter le contrat", s =>
            {
                var paye = Rng.Next(2000, 6000);
                switch (Rng.Next(4))
                {
                    case 0:
                        Narrator.Say($"Tu trouves la cible. Elle ne s'y attendait pas. La transaction est propre. {paye}cr.", Color.Red);
                        s.Credits += paye; s.Reputation -= 40;
                        Display.ShowEvent($"+{paye}cr. -40 réputation.", Color.Red);
                        break;
                    case 1:
                        Narrator.Say("La cible était mieux protégée que prévu. L'opération tourne mal.", Color.Red);
                        s.ShipHp = Math.Max(1, s.ShipHp - Rng.Next(20, 50));
                        s.Reputation -= 20;
                        Display.ShowEvent($"-PV vaisseau. -20 réputation. Pas de paiement.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("La cible t'attendait. C'était un piège. Le commanditaire voulait t'éliminer proprement.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)]));
                        return;
                    case 3:
                        Narrator.Say("La cible te propose plus que le commanditaire pour que tu la laisses vivre.", Color.Gold1);
                        var contre = Rng.Next(3000, 8000);
                        s.Credits += contre; s.Reputation -= 10;
                        Display.ShowEvent($"+{contre}cr. Tu travailles maintenant pour la cible. -10 réputation.", Color.Gold1);
                        break;
                }
                Narrator.Pause();
            }),

            new("Refuser et partir", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il hoche la tête. 'Quelqu'un d'autre le fera.' Tu repars.", Color.Grey);
                        break;
                    case 1:
                        Narrator.Say("Il note quelque chose. Tu sais trop maintenant pour qu'on te laisse partir tranquillement.", Color.Red);
                        s.Reputation -= 15;
                        Display.ShowEvent("-15 réputation. T'as refusé mais tu sais.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Il te met quand même de l'argent dans la poche. 'Pour le dérangement. Et pour ton silence.'", Color.Yellow);
                        var silence = Rng.Next(300, 800);
                        s.Credits += silence;
                        Display.ShowEvent($"+{silence}cr. Prix du silence.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),

            new("Demander plus — doubler le prix", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("Il réfléchit. Il accepte.", Color.Gold1);
                        var doublepaye = Rng.Next(4000, 10000);
                        s.Credits += doublepaye; s.Reputation -= 40;
                        Display.ShowEvent($"+{doublepaye}cr. -40 réputation.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Il se lève. 'T'es pas à la hauteur du travail si t'as peur du prix.' Tu es sorti.", Color.Grey);
                        break;
                    case 2:
                        Narrator.Say("Il contre-propose. 'Je te donne le double si tu me ramènes aussi quelque chose de la cible.'", Color.Yellow);
                        Display.ShowEvent("La proposition est sur la table.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),

            new("Prévenir la cible", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("La cible te récompense généreusement. Et te met sur sa liste des gens qu'elle ne veut pas voir disparaître.", Color.Green);
                        var récompense = Rng.Next(2000, 5000);
                        s.Credits += récompense; s.Reputation += 30;
                        Display.ShowEvent($"+{récompense}cr. +30 réputation.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Le commanditaire l'apprend. Il envoie quelqu'un te régler le problème.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)]));
                        return;
                    case 2:
                        Narrator.Say("La cible croit que c'est un autre piège. Elle te fait chasser.", Color.Red);
                        s.Reputation -= 10;
                        Display.ShowEvent("-10 réputation. Bonne intention, mauvais résultat.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),
        ],
        Color.Red);

    static Situation JeuSouterrain(GameState state) => new(
        "Un passage entre deux caisses mène à une salle souterraine. Des gens parient autour d'un combat clandestin.",
        [
            new("Parier sur un combattant", s =>
            {
                var mise = Rng.Next(200, 800);
                if (s.Credits < mise) { Narrator.Say("T'as pas de quoi jouer.", Color.Grey); Narrator.Pause(); return; }
                s.Credits -= mise;
                switch (Rng.Next(3))
                {
                    case 0:
                        var gain = (int)(mise * (Rng.Next(150, 350) / 100.0));
                        s.Credits += gain;
                        Narrator.Say($"Ton combattant gagne. Proprement.", Color.Gold1);
                        Display.ShowEvent($"-{mise}cr misés. +{gain}cr récupérés.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Ton combattant tombe au troisième round. Quelqu'un a arrangé le combat.", Color.Red);
                        Display.ShowEvent($"-{mise}cr. Perdu.", Color.Red);
                        break;
                    case 2:
                        Narrator.Say("Le combat est arrêté par une descente de gardes. Tout le monde fuit.", Color.Yellow);
                        s.Reputation -= 10;
                        Display.ShowEvent($"-{mise}cr. -10 réputation. Descente.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }),

            new("Entrer dans l'arène soi-même", s =>
            {
                Narrator.Say("La foule se retourne. Quelqu'un accepte le défi.", Color.Red);
                var adversaire = Combat.TierMid[Rng.Next(Combat.TierMid.Count)];
                var outcome = Combat.Start(s, adversaire);
                if (outcome == CombatOutcome.Victory)
                {
                    var prize = Rng.Next(600, 2000);
                    s.Credits += prize; s.Reputation += 20;
                    Display.ShowEvent($"+{prize}cr. +20 réputation. La salle t'acclame.", Color.Gold1);
                    Narrator.Pause();
                }
                else Situations.ApplyCombatOutcome(s, outcome);
            }),

            new("Observer et chercher à tricher", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        Narrator.Say("T'as vu comment le combat était arrangé. T'as misé en conséquence.", Color.Gold1);
                        var gain = Rng.Next(500, 1500);
                        s.Credits += gain; s.Reputation -= 10;
                        Display.ShowEvent($"+{gain}cr. -10 réputation. Tu t'es servi du système.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say("Quelqu'un t'a vu regarder de trop près. Ils pensent que t'es flic.", Color.Red);
                        Situations.ApplyCombatOutcome(s, Combat.Start(s, Combat.TierMid[Rng.Next(Combat.TierMid.Count)]));
                        return;
                    case 2:
                        Narrator.Say("T'as rien trouvé à exploiter. Le combat était peut-être honnête.", Color.Grey);
                        break;
                }
                Narrator.Pause();
            }),

            new("Repartir — ce genre d'endroit laisse des traces", s =>
            {
                Narrator.Say("Tu repars. Ce genre d'endroit, soit tu t'en souviens bien soit tu t'en souviens très mal.", Color.Grey);
                Narrator.Pause();
            }),
        ],
        Color.Gold1);

    static Situation SousMarine(GameState state) => new(
        "Un message anonyme sur un terminal public. Coordonnées, heure, un chiffre. Rendez-vous souterrain.",
        [
            new("Y aller", s =>
            {
                switch (Rng.Next(4))
                {
                    // ── CAS 0 : Groupe politique — le joueur choisit s'il accepte ──
                    case 0:
                        Narrator.Say("Un groupe politique clandestin. Ils t'exposent leur situation : une livraison urgente, des gens surveillés, besoin d'un inconnu qui ne figure sur aucune liste. Idéalistes ou dangereux ? Les deux, probablement.", Color.Cyan1);
                        var paye = Rng.Next(800, 2500);
                        ChoiceMenu.Resolve(new Situation($"Ils proposent {paye}cr. Tu acceptes ?",
                        [
                            new("Accepter le job", gs =>
                            {
                                if (Rng.Next(100) < 65)
                                {
                                    gs.Credits += paye; gs.Reputation -= 15;
                                    Narrator.Say($"Livraison faite. Discret. +{paye}cr, -15 réputation. T'es impliqué maintenant.", Color.Yellow);
                                }
                                else
                                {
                                    gs.Reputation -= 35; gs.IsImprisoned = true;
                                    Narrator.Say("La douane t'attendait à mi-chemin. Quelqu'un dans leur groupe a parlé.", Color.Red);
                                }
                                Narrator.Pause();
                            }),
                            new("Négocier le prix à la hausse", gs =>
                            {
                                if (Rng.Next(100) < 50)
                                {
                                    var bonus = (int)(paye * 1.5);
                                    gs.Credits += bonus; gs.Reputation -= 15;
                                    Narrator.Say($"Ils acceptent. T'es soit courageux soit inconscient. +{bonus}cr.", Color.Gold1);
                                }
                                else Narrator.Say("Ils ont trouvé quelqu'un d'autre moins gourmand. Tu repars bredouille.", Color.Grey);
                                Narrator.Pause();
                            }),
                            new("Refuser — trop risqué", gs =>
                            {
                                if (Rng.Next(100) < 30) { gs.Reputation -= 10; Narrator.Say("Ils notent ton visage. 'T'as vu trop pour repartir simplement.' Mais ils te laissent partir. -10 rép.", Color.Yellow); }
                                else Narrator.Say("Ils te laissent partir sans un mot. La porte se referme.", Color.Grey);
                                Narrator.Pause();
                            }),
                            new("Les dénoncer à la sortie", gs =>
                            {
                                gs.Credits += Rng.Next(300, 700); gs.Reputation -= 30;
                                Narrator.Say("La récompense officielle est maigre. Le nom de traître, lui, colle.", Color.Red);
                                Narrator.Pause();
                            }),
                        ], Color.Cyan1), s);
                        return;

                    // ── CAS 1 : Marché noir — inchangé, déjà interactif ──
                    case 1:
                        Narrator.Say("C'est un marché noir de haut niveau. Des marchandises que tu ne verras jamais en surface.", Color.Gold1);
                        ChoiceMenu.Resolve(new Situation("Tu achètes quelque chose ?",
                        [
                            new("Acheter des Armes (haut de gamme, 1500cr)", gs =>
                            {
                                if (gs.Credits < 1500) { Display.ShowEvent("Pas assez.", Color.Red); Narrator.Pause(); return; }
                                gs.Credits -= 1500; gs.Cargo.Add("Armes", 2);
                                Display.ShowEvent("-1500cr. +2 Armes.", Color.Gold1); Narrator.Pause();
                            }, gs => gs.Credits >= 1500),
                            new("Acheter des Médicaments rares (800cr)", gs =>
                            {
                                if (gs.Credits < 800) { Display.ShowEvent("Pas assez.", Color.Red); Narrator.Pause(); return; }
                                gs.Credits -= 800; gs.Cargo.Add("Médicaments", 3);
                                Display.ShowEvent("-800cr. +3 Médicaments.", Color.Green); Narrator.Pause();
                            }, gs => gs.Credits >= 800),
                            new("Proposer de vendre quelque chose", gs =>
                            {
                                if (gs.Cargo.All.Any())
                                {
                                    var item = gs.Cargo.All.Keys.First();
                                    var prix = Rng.Next(600, 2000);
                                    gs.Cargo.Remove(item, 1); gs.Credits += prix;
                                    Narrator.Say($"Ils achètent ton {item} sans négocier. +{prix}cr.", Color.Gold1);
                                }
                                else Narrator.Say("T'as rien qu'ils veulent.", Color.Grey);
                                Narrator.Pause();
                            }),
                            new("Juste regarder et partir", _ => { Narrator.Say("Certains savoir ne valent rien si on les utilise pas.", Color.Grey); Narrator.Pause(); }),
                        ]), s);
                        return;

                    // ── CAS 2 : Traquenard — combat immédiat ──
                    case 2:
                        Narrator.Say("C'était un traquenard. Quelqu'un voulait voir qui répondrait à ce message.", Color.Red);
                        ChoiceMenu.Resolve(new Situation("Embuscade. Tu fais quoi ?",
                        [
                            new("Se battre — pas le choix", gs =>
                            {
                                Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth + 1)));
                            }),
                            new("Fuir en renversant tout sur ton passage", gs =>
                            {
                                if (Rng.Next(100) < 55) { gs.Fuel = Math.Max(0, gs.Fuel - 1); Narrator.Say("Tu t'en sors de justesse. -1 carburant.", Color.Yellow); }
                                else { var hp = Rng.Next(20, 45); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp); Narrator.Say($"Presque. -{hp} PV.", Color.Red); }
                                Narrator.Pause();
                            }, gs => gs.Fuel > 0),
                            new("Négocier — 'je travaille pour quelqu'un de pire que vous'", gs =>
                            {
                                var chance = 20 + (gs.Reputation < -200 ? 30 : 0);
                                if (Rng.Next(100) < chance) Narrator.Say("Ils hésitent. Ils te laissent partir. Ta réputation est arrivée avant toi.", Color.Green);
                                else { Narrator.Say("Ils rigolent.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth + 1))); return; }
                                Narrator.Pause();
                            }),
                        ], Color.Red), s);
                        return;

                    // ── CAS 3 : Coffre mystère ──
                    default:
                        Narrator.Say("Une salle vide. Un coffre. Un mot : 'Pour celui qui avait le courage de venir.'", Color.Gold1);
                        ChoiceMenu.Resolve(new Situation("Le coffre ?",
                        [
                            new("L'ouvrir sans hésiter", gs =>
                            {
                                var trésor = Rng.Next(1000, 4000);
                                gs.Credits += trésor;
                                Display.ShowEvent($"+{trésor}cr.", Color.Gold1); Narrator.Pause();
                            }),
                            new("Vérifier s'il est piégé d'abord", gs =>
                            {
                                if (Rng.Next(100) < 25) { var hp = Rng.Next(15, 35); gs.PlayerHp = Math.Max(1, gs.PlayerHp - hp); Narrator.Say($"Il l'était. -{hp} PV. Mais t'as quand même pris ce qu'il y avait dedans.", Color.Red); gs.Credits += Rng.Next(500, 2000); }
                                else { var trésor = Rng.Next(1000, 4000); gs.Credits += trésor; Narrator.Say($"Pas piégé. +{trésor}cr.", Color.Gold1); }
                                Narrator.Pause();
                            }),
                            new("Laisser — trop beau pour être vrai", gs => { Narrator.Say("T'as pas pris. C'était peut-être vraiment gratuit. On saura jamais.", Color.Grey); Narrator.Pause(); }),
                        ], Color.Gold1), s);
                        return;
                }
            }),

            new("Ignorer le message", s =>
            {
                Narrator.Say("Tu effaces le message. Il y en a un deuxième deux heures plus tard.", Color.Grey);
                ChoiceMenu.Resolve(new Situation("Tu l'ignores encore ?",
                [
                    new("Oui — bloquer l'expéditeur", gs => { Narrator.Say("Les messages s'arrêtent. Ce n'est pas rassurant.", Color.Grey); Narrator.Pause(); }),
                    new("Finalement y aller — leur insistance t'intrigue", gs =>
                    {
                        var cr = Rng.Next(400, 1200); gs.Credits += cr;
                        Narrator.Say($"L'insistance avait une raison. +{cr}cr.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("Répondre anonymement — sonder sans s'exposer", gs =>
                    {
                        if (Rng.Next(2) == 0) { gs.Reputation += 10; var cr = Rng.Next(200, 700); gs.Credits += cr; Narrator.Say($"Ils acceptent l'anonymat. Info utile en échange. +{cr}cr, +10 rép.", Color.Cyan1); }
                        else { gs.Reputation -= 10; Narrator.Say("Ta réponse anonyme les a alertés. Ils savent qui tu es maintenant. -10 rép.", Color.Red); }
                        Narrator.Pause();
                    }),
                ], Color.Grey), s);
            }),

            new("Tendre un piège — y aller armé en avance, embuscade prête", s =>
            {
                // L'embuscade se déclenche TOUJOURS — la variable c'est ce qui arrive.
                Narrator.Say("T'arrives une heure avant l'heure indiquée. Tu prends position dans l'ombre. Tu attends.", Color.OrangeRed1);
                switch (Rng.Next(3))
                {
                    case 0:
                        // Un groupe neutre arrive → tu as l'avantage tactique, tu choisis
                        Narrator.Say("Un groupe de trois arrive. Pas d'armes visibles. Ils ont l'air de chercher quelqu'un.", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Tu fais quoi depuis ta cachette ?",
                        [
                            new("Sortir et négocier — avantage de surprise", gs =>
                            {
                                var cr = Rng.Next(600, 1800); gs.Credits += cr; gs.Reputation += 10;
                                Narrator.Say($"Ils sont surpris. T'as négocié en position de force. +{cr}cr, +10 rép.", Color.Gold1);
                                Narrator.Pause();
                            }),
                            new("Attaquer — tu avais préparé ça", gs =>
                            {
                                var e = Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], 1);
                                var outcome = Combat.Start(gs, e);
                                if (outcome == CombatOutcome.Victory) { var loot = Rng.Next(500, 1500); gs.Credits += loot; Display.ShowEvent($"+{loot}cr récupérés.", Color.Gold1); }
                                Situations.ApplyCombatOutcome(gs, outcome);
                            }),
                            new("Rester caché et écouter", gs =>
                            {
                                var cr = Rng.Next(400, 1200); gs.Credits += cr;
                                Narrator.Say($"Ils parlent. Tu entends quelque chose d'utile. +{cr}cr d'info.", Color.Cyan1);
                                Narrator.Pause();
                            }),
                        ], Color.OrangeRed1), s);
                        return;

                    case 1:
                        // C'était bien un piège — tu les as vus arriver en premier
                        Narrator.Say("Cinq types lourdement armés. C'était clairement un piège. Mais toi t'étais là avant eux.", Color.Red);
                        ChoiceMenu.Resolve(new Situation("T'as l'avantage de la surprise. Qu'est-ce que tu fais ?",
                        [
                            new("Attaque immédiate — neutraliser le groupe", gs =>
                            {
                                var e = Combat.Scale(Combat.TierMid[Rng.Next(Combat.TierMid.Count)], 2);
                                var outcome = Combat.Start(gs, e);
                                if (outcome == CombatOutcome.Victory) { var butin = Rng.Next(800, 2200); gs.Credits += butin; gs.Reputation += 15; Display.ShowEvent($"+{butin}cr sur eux. +15 rép.", Color.Gold1); }
                                Situations.ApplyCombatOutcome(gs, outcome);
                            }),
                            new("Sortir et lancer un avertissement — les faire fuir", gs =>
                            {
                                var chance = 35 + (gs.Reputation < 0 ? 20 : 0) + (gs.Class.Name == "Seigneur de guerre" ? 25 : 0);
                                if (Rng.Next(100) < chance) { gs.Reputation += 20; Narrator.Say("Ils fuient. Ta réputation les a précédés. +20 rép.", Color.Green); }
                                else { Narrator.Say("Ils rigolent et avancent quand même.", Color.Red); Situations.ApplyCombatOutcome(gs, Combat.Start(gs, Combat.GetScaled(gs, gs.ZoneDepth + 1))); return; }
                                Narrator.Pause();
                            }),
                            new("Rester caché et les laisser partir", gs =>
                            {
                                Narrator.Say("Ils attendent une heure. Ils repartent. Tu sors dans un couloir vide. Sage.", Color.Grey);
                                Narrator.Pause();
                            }),
                        ], Color.Red), s);
                        return;

                    default:
                        // Personne ne vient — mais t'as quand même attendu
                        Narrator.Say("Personne. Une heure d'attente dans le noir. Le rendez-vous a été annulé ou c'était du vent.", Color.Grey);
                        ChoiceMenu.Resolve(new Situation("Tu es dans la salle. Elle n'est pas vide.",
                        [
                            new("Fouiller maintenant que tu es là", gs =>
                            {
                                var cr = Rng.Next(300, 900); gs.Credits += cr;
                                Narrator.Say($"T'as fouillé la salle pendant que t'attendais. +{cr}cr trouvés.", Color.Gold1);
                                Narrator.Pause();
                            }),
                            new("Repartir — perte de temps", gs => { Narrator.Say("T'es reparti. L'heure passée dans le noir était quand même utile : personne ne t'a vu.", Color.Grey); Narrator.Pause(); }),
                        ], Color.Grey), s);
                        return;
                }
            }),
        ],
        Color.Magenta1);

    // ── RECRUTEMENT FACTION ─────────────────────────────────────────────────

    static Situation RecrutementFaction(GameState state)
    {
        if (state.Faction != FactionId.None)
        {
            return new Situation(
                "Une silhouette te reconnaît. Elle sait pour ta faction. Elle propose autre chose.",
                [
                    new("Écouter — double jeu", s =>
                    {
                        switch (Rng.Next(3))
                        {
                            case 0:
                                s.IsDoubleAgent = true;
                                s.SecondFaction = s.Faction == FactionId.Faucons ? "Gardiens" : "Faucons";
                                Narrator.Say("Double jeu. Tu travailles pour les deux camps. Les bénéfices sont doubles. Le risque aussi.", Color.Magenta1);
                                Display.ShowEvent("Statut : Agent double activé.", Color.Magenta1);
                                break;
                            case 1:
                                var cr = Rng.Next(1000, 3000);
                                s.Credits += cr;
                                Narrator.Say($"Une seule mission. Discret. +{cr}cr.", Color.Yellow);
                                break;
                            case 2:
                                s.Reputation -= 30;
                                Narrator.Say("Test de loyauté de ta propre faction. Tu les as déçus. -30 réputation.", Color.Red);
                                break;
                        }
                        Narrator.Pause();
                    }),
                    new("Refuser fermement", s => { s.AddReputation(10); Narrator.Say("'Je suis loyal.' +10 réputation.", Color.Green); Narrator.Pause(); }),
                ],
                Color.Magenta1);
        }

        var factionId = (FactionId)Rng.Next(1, 5);
        var (nom, desc, bonus) = Factions.Info[factionId];

        return new Situation(
            $"Quelqu'un s'approche avec le signe discret de {nom}. Il attend.",
            [
                new($"Rejoindre {nom}", s =>
                {
                    s.Faction = factionId;
                    Narrator.Say($"Tu rejoins {nom}. Bonus : {bonus}.", Color.Gold1);
                    Display.ShowEvent($"Faction : {nom}", Color.Gold1);
                    Narrator.Pause();
                }),
                new("Demander plus d'infos", s =>
                {
                    Narrator.Say($"{desc} Bonus : {bonus}. Tu peux rejoindre via les PNJs de la faction.", Color.Cyan1);
                    Narrator.Pause();
                }),
                new("Refuser", s => { Narrator.Say("'La porte reste ouverte.'", Color.Grey); Narrator.Pause(); }),
            ],
            Color.Cyan1);
    }

    // ── PNJ GOSSIP — habitant qui parle du lieu ─────────────────────────────

    static Situation GossipNpc(GameState state)
    {
        var gossip = QuestSystem.GetGossip(state.CurrentStation);
        var openers = new[]
        {
            "Un habitant s'arrête et te regarde. Il a quelque chose à dire.",
            "Quelqu'un s'assoit à côté de toi sans être invité. Il parle sans te regarder.",
            "Une voix basse dans ton oreille. Tu te retournes. Un inconnu, l'air de savoir des choses.",
            "Un vieux type dans un coin te fait signe d'approcher.",
            "Une femme qui fait semblant de réparer quelque chose te parle sans lever les yeux.",
        };
        Narrator.Say(openers[Rng.Next(openers.Length)], Color.Grey);
        Narrator.Say($"\"{gossip}\"", Color.Cyan1);

        return new Situation("Il a l'air d'en savoir plus. Comment tu joues ça ?",
        [
            new("Écouter attentivement — mémoriser chaque détail", s =>
            {
                s.Reputation += 5;
                switch (Rng.Next(3))
                {
                    case 0:
                        var cr = Rng.Next(200, 700); s.Credits += cr;
                        Narrator.Say($"Il continue. Les détails qu'il donne valent quelque chose. +{cr}cr d'info utilisable.", Color.Green);
                        break;
                    case 1:
                        Narrator.Say("Il t'indique un endroit précis dans la station. Un raccourci. Un angle mort. Utile si tu explores.", Color.Cyan1);
                        s.ZoneDepth = Math.Max(0, s.ZoneDepth - 1);  // moins profond pour trouver le boss
                        Display.ShowEvent("Profondeur effective réduite — tu sais où chercher.", Color.Cyan1);
                        break;
                    case 2:
                        s.Reputation += 10;
                        Narrator.Say("Il t'a reconnu d'une rumeur. Ce que tu as fait ailleurs lui est parvenu. +10 rép.", Color.Green);
                        break;
                }
                Narrator.Pause();
            }),

            new("Lui offrir un verre — le faire parler davantage", s =>
            {
                var cout = Rng.Next(30, 80); s.Credits = Math.Max(0, s.Credits - cout);
                switch (Rng.Next(3))
                {
                    case 0:
                        var bonus = Rng.Next(400, 1200); s.Credits += bonus;
                        Narrator.Say($"-{cout}cr. Il se détend. Il te donne l'adresse d'une planque. +{bonus}cr.", Color.Gold1);
                        break;
                    case 1:
                        Narrator.Say($"-{cout}cr. Il parle beaucoup mais dit peu. L'alcool délie les langues sur des choses sans importance.", Color.Grey);
                        break;
                    case 2:
                        s.Reputation += 8;
                        Narrator.Say($"-{cout}cr. Il te présente quelqu'un d'utile avant de partir. +8 rép.", Color.Cyan1);
                        break;
                }
                Narrator.Pause();
            }),

            new("Lui poser une question directe sur le boss du coin", s =>
            {
                var extraGossip = QuestSystem.GetGossip(s.CurrentStation);
                Narrator.Say($"Il baisse la voix. \"{extraGossip}\"", Color.Red);
                if (Rng.Next(100) < 40)
                    Narrator.Say("Il ajoute : 'Si tu cherches vraiment... explore. Descends. C'est au fond que ça se passe.'", Color.OrangeRed1);
                Narrator.Pause();
            }),

            new("Ignorer — t'as pas besoin de ça", s =>
            {
                Narrator.Say("Il hausse les épaules. 'T'as tort mais c'est ton problème.'", Color.Grey);
                Narrator.Pause();
            }),
        ], Color.Cyan1);
    }

    // ── PNJ DONNEUR DE QUÊTES ───────────────────────────────────────────────

    static Situation QuestGiver(GameState state)
    {
        var quest = QuestSystem.Generate(state);

        if (quest is null)
        {
            // Pas de quête disponible, fallback sur un habitant ordinaire
            return GossipNpc(state);
        }

        var openers = new[]
        {
            "Quelqu'un t'attrape le bras discrètement. 'J'ai besoin de quelqu'un.'",
            "Un inconnu te bloque le passage, pas agressivement. Juste déterminé. 'T'as une minute ?'",
            "Une voix depuis une alcôve. 'Psst. Toi. T'as l'air de quelqu'un qui fait des choses.'",
            "Un homme avec une enveloppe te regarde approcher. Il attendait quelqu'un. Peut-être toi.",
            "Une femme dans le couloir te fait signe. Son expression est urgente.",
        };

        Narrator.Say(openers[Rng.Next(openers.Length)], Color.Yellow);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [cyan1 bold]{quest.Title}[/]");
        AnsiConsole.MarkupLine($"  {quest.Description}");
        AnsiConsole.MarkupLine($"  [yellow dim]Récompense : {quest.CreditReward}cr, +{quest.RepReward} réputation[/]");
        AnsiConsole.WriteLine();

        return new Situation("Tu acceptes ?",
        [
            new("Accepter la quête", s =>
            {
                if (s.ActiveQuests.Count >= 5)
                {
                    Narrator.Say("T'as déjà trop de trucs en cours. Règle ça d'abord.", Color.Yellow);
                    Narrator.Pause();
                    return;
                }
                s.ActiveQuests.Add(quest);
                Narrator.Say($"'Bien. Je compte sur toi.' — {quest.Giver}", Color.Cyan1);
                Display.ShowEvent($"Quête ajoutée : {quest.Title}", Color.Cyan1);
                Narrator.Pause();
            }),

            new("Négocier la récompense", s =>
            {
                if (Rng.Next(100) < 45)
                {
                    var betterQuest = quest with { CreditReward = (int)(quest.CreditReward * 1.4) };
                    s.ActiveQuests.Add(betterQuest);
                    Narrator.Say($"Il hésite. Il accepte. {betterQuest.CreditReward}cr maintenant.", Color.Gold1);
                    Display.ShowEvent($"Quête acceptée (meilleure récompense) : {betterQuest.Title}", Color.Gold1);
                }
                else
                {
                    Narrator.Say("'T'es pas en position de négocier.' Il repart.", Color.Grey);
                }
                Narrator.Pause();
            }),

            new("Refuser — pas le temps", s =>
            {
                if (Rng.Next(100) < 25)
                    Narrator.Say($"'{quest.Giver} se retourne. 'Dommage. J'aurais pu te faciliter la vie ici.' Il disparaît.", Color.Grey);
                else
                    Narrator.Say("Il acquiesce. 'La prochaine fois peut-être.'", Color.Grey);
                Narrator.Pause();
            }),

            new("Écouter les détails avant de décider", s =>
            {
                switch (quest.Type)
                {
                    case QuestType.Delivery:
                        Narrator.Say($"'C'est une livraison discrète. {quest.TargetItem} à une personne de confiance sur {quest.TargetStation}. Simple si tu poses pas de questions.'", Color.Cyan1);
                        break;
                    case QuestType.Kill:
                        Narrator.Say($"'Je veux que tu ailles sur {quest.TargetStation} et que tu t'en prennes au maître des lieux. Méthode libre. Résultat attendu.'", Color.Red);
                        break;
                    case QuestType.Revenge:
                        Narrator.Say($"'Ces gens de {quest.TargetStation} ont détruit ce que j'avais construit. Je veux qu'ils sachent que ça a un prix.'", Color.Orange1);
                        break;
                    case QuestType.Info:
                        Narrator.Say($"'Va sur {quest.TargetStation}. Regarde ce qui s'y passe. Reviens me dire. C'est tout.'", Color.Grey);
                        break;
                }
                Narrator.Say("Tu acceptes maintenant ?", Color.Yellow);

                ChoiceMenu.Resolve(new Situation("",
                [
                    new("Oui", gs =>
                    {
                        if (gs.ActiveQuests.Count >= 5) { Narrator.Say("T'as déjà trop de quêtes.", Color.Yellow); }
                        else { gs.ActiveQuests.Add(quest); Display.ShowEvent($"Quête ajoutée : {quest.Title}", Color.Cyan1); }
                        Narrator.Pause();
                    }),
                    new("Non", _ => { Narrator.Say("Il repart.", Color.Grey); Narrator.Pause(); }),
                ], Color.Yellow), s);
            }),
        ], Color.Yellow);
    }
}
