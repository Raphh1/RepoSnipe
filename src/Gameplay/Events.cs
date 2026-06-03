using Spectre.Console;

namespace VoidTrader;

static class Events
{
    private static readonly Random Rng = new();

    private record Event(int Weight, bool IsPositive, bool IsPirate, Action<GameState> Apply, string Message, Color Color);

    private static readonly List<Event> Pool =
    [
        // ── POSITIFS ────────────────────────────────────────────────────────
        new(12, true,  false, s => s.Credits += 200,   "Une capsule de cargo dérive vers toi. +200cr",                              Color.Green),
        new(8,  true,  false, s => s.Credits += 500,   "Un marchand reconnaissant te glisse 500cr sans explication.",               Color.Green),
        new(8,  true,  false, s => s.Fuel    += 2,     "Épave repérée — 2 cellules de carburant récupérées.",                       Color.Cyan1),
        new(6,  true,  false, s => s.Credits += 300,   "Tu gagnes un pari contre un contrebandier ivre. +300cr",                    Color.Green),
        new(4,  true,  false, s => s.Credits += 1000,  "Artefact rare à bord — vendu 1000cr !",                                     Color.Gold1),
        new(4,  true,  false, s => s.Fuel    += 3,     "Cache de carburant au marché noir. +3 carburant.",                          Color.Gold1),
        new(5,  true,  false, s => s.Credits += 400,   "Un vaisseau militaire confond ton vaisseau avec une escorte. Tu joues le jeu. +400cr", Color.Green),
        new(4,  true,  false, s => s.Credits += 700,   "Un drone de livraison perdu se coince dans ton vaisseau. Sa cargaison est à toi.", Color.Green),
        new(3,  true,  false, s => { s.Credits += 1500; s.Reputation += 5; }, "Un coffre-fort intact dans une épave dérivante. +1500cr", Color.Gold1),
        new(5,  true,  false, s => s.Credits += 250,   "Un touriste te photographie et te donne de l'argent. Il croit que t'es célèbre.", Color.Green),
        new(3,  true,  false, s => { s.Cargo.Add("Artefacts", 1); }, "Une boîte mystérieuse flotte dans les débris. Contenu inconnu.", Color.Cyan1),
        new(4,  true,  false, s => s.Fuel    += 1,     "Ton moteur produit accidentellement du carburant de qualité. +1 fuel bonus.", Color.Cyan1),
        new(3,  true,  false, s => { s.Credits += 2000; s.Reputation += 10; }, "Deux pirates s'entretuent sur ta route. Tu récupères tout.", Color.Gold1),
        new(5,  true,  false, s => s.ShipHp = Math.Min(s.ShipHp + 15, s.ShipMaxHp), "Un robot de nettoyage s'accroche à ta coque et répare l'extérieur. +15 PV vaisseau.", Color.Green),
        new(4,  true,  false, s => s.Credits += 600,   "Un pilote suicidaire te vend son vaisseau pour 1cr. Tu le désosses. +600cr.", Color.Green),

        // ── NÉGATIFS ────────────────────────────────────────────────────────
        new(10, false, false, s => s.Credits -= 150,   "Péage spatial. 150cr, que tu le veuilles ou non.",                          Color.Red),
        new(10, false, false, s => s.Fuel    -= 1,     "Micrométéorite. -1 cellule de carburant.",                                  Color.Red),
        new(5,  false, false, s => s.Credits -= 500,   "Panne moteur. Les réparations coûtent 500cr.",                              Color.OrangeRed1),
        new(5,  false, false, s => { s.ShipHp -= 20; s.ShipHp = Math.Max(1, s.ShipHp); }, "Impact de débris. -20 PV vaisseau.",    Color.Red),
        new(4,  false, false, s => { s.Credits -= 200; s.Credits = Math.Max(0, s.Credits); }, "Passager clandestin te vole 200cr en dormant.", Color.Red),
        new(4,  false, false, s => { if (s.Cargo.All.Any()) { var k = s.Cargo.All.Keys.First(); s.Cargo.Remove(k, 1); Display.ShowEvent($"Tempête ionique. Ta cargaison de {k} est endommagée.", Color.Red); } }, "", Color.Red),
        new(3,  false, false, s => { s.ShipHp -= 30; s.ShipHp = Math.Max(1, s.ShipHp); }, "Champ de mines oublié depuis la guerre. -30 PV vaisseau.", Color.Red),
        new(3,  false, false, s => s.Credits -= 300,   "Une dette oubliée te rattrape. -300cr ou tu te bagarres.",                  Color.Red),
        new(4,  false, false, s => { s.ShipHp -= 10; s.Fuel -= 1; s.ShipHp = Math.Max(1, s.ShipHp); s.Fuel = Math.Max(0, s.Fuel); }, "Tempête de particules. -10 PV vaisseau, -1 carburant.", Color.OrangeRed1),

        // ── PIRATES ─────────────────────────────────────────────────────────
        new(8,  false, true,  s => s.Credits -= 300,   "Pirates ! Ils te volent 300cr.",                                           Color.Red),
        new(5,  false, true,  s => s.Credits -= 500,   "Embuscade pirate organisée. -500cr.",                                      Color.Red),

        // ── WTF ─────────────────────────────────────────────────────────────
        new(4,  true,  false, s => { s.Credits += Rng.Next(0, 2000); }, "Un vaisseau explose en te dépassant. Sa cargaison te tombe dessus. Montant aléatoire.", Color.Gold1),
        new(3,  true,  false, s => s.Reputation += 15, "Un signal en morse vieux de 200 ans. Les coordonnées indiquent ta position. Quelqu'un t'attendait.", Color.Cyan1),
        new(3,  false, false, s => { s.Credits -= Rng.Next(50, 400); s.Credits = Math.Max(0, s.Credits); }, "Ton IA de navigation refuse d'avancer avant que tu l'écoutes. Tu perds du temps et de l'argent.", Color.OrangeRed1),
        new(2,  true,  false, s => { s.Credits += 1; s.Fuel += 1; }, "Un vaisseau de mariage passe. Du riz spatial percute ton réacteur. +1 fuel inexplicablement.", Color.Gold1),
        new(3,  true,  false, s => s.Cargo.Add("Artefacts", 1), "Une bouteille à la mer spatiale. Dedans : un artefact et des coordonnées illisibles.", Color.Cyan1),
        new(2,  false, false, s => s.Reputation -= 20, "Tu reçois un message de toi-même : 'ne va pas là'. Tu ignores. Erreur.", Color.Red),
        new(3,  true,  false, s => { var r = Rng.Next(3); if(r==0) { s.Credits += Rng.Next(500,3000); } else if(r==1) { s.ShipHp -= 25; s.ShipHp = Math.Max(1, s.ShipHp); } else { s.Cargo.Add("Artefacts", 1); } }, "Un enfant lance quelque chose depuis un sas. C'est soit une bombe soit le meilleur cadeau de ta vie.", Color.Gold1),

        // ── NEUTRE ──────────────────────────────────────────────────────────
        new(14, true,  false, s => { },                "Voyage tranquille. Rien à signaler.",                                       Color.Grey),
        new(6,  true,  false, s => { },                "Le vide est calme aujourd'hui. Tu en profites.",                            Color.Grey),
        new(4,  true,  false, s => { },                "Quelques débris sur la route, rien de dangereux.",                         Color.Grey),
    ];

    public static void MaybeTriggerWithChoice(GameState state)
    {
        // Chasseurs de primes si réputation très négative
        if (state.Reputation <= -300)
        {
            var bountyChance = Math.Min(60, Math.Abs(state.Reputation) / 10);
            if (Rng.Next(100) < bountyChance)
            {
                TriggerBountyHunter(state);
                return;
            }
        }

        // Événements liés aux quêtes actives (25% si une quête est en cours)
        if (state.ActiveQuests.Count > 0 && Rng.Next(100) < 25)
        {
            var q = state.ActiveQuests[Rng.Next(state.ActiveQuests.Count)];
            switch (q.Type)
            {
                case QuestType.Delivery when q.TargetItem != null:
                    TriggerQuestTravelEvent(state, q); return;
                case QuestType.Kill:
                    TriggerKillQuestTravelEvent(state, q); return;
                case QuestType.Revenge:
                    TriggerRevengeQuestTravelEvent(state, q); return;
                case QuestType.Info:
                    TriggerInfoQuestTravelEvent(state, q); return;
            }
        }

        if (Rng.Next(100) >= 70) return;

        var cls = state.Class;

        var pool = Pool
            .Where(e => !(e.Message == "" && e.Apply != null)) // filtre les events avec message vide qui ont leur propre affichage
            .Select(e =>
            {
                var w = e.Weight;
                if (e.IsPirate && cls.PiratesDoubled) w *= 2;
                if (e.IsPositive && cls.NeutralEventsBoost) w = (int)(w * 1.5);
                // Arc Ouest Apocalypse : pas de pirates en route vers cette station
                if (e.IsPirate && state.CurrentStation == "Arc Ouest Apocalypse") w = 0;
                return (weight: w, e.IsPositive, e.IsPirate, e.Apply, e.Message, e.Color);
            })
            .Where(e => e.weight > 0)
            .ToList();

        var totalWeight = pool.Sum(e => e.weight);
        var roll        = Rng.Next(totalWeight);
        var cursor      = 0;

        foreach (var (weight, isPositive, isPirate, apply, message, color) in pool)
        {
            cursor += weight;
            if (roll >= cursor) continue;

            if (isPirate && cls.AutoKillsPirates)
            {
                Display.ShowEvent("Des pirates t'attaquent — tu les anéantis sans même transpirer.", Color.Green);
                return;
            }

            if (isPirate && state.Faction == FactionId.Faucons)
            {
                Display.ShowEvent("Des Faucons Noirs t'interceptent — ils reconnaissent ton signe. Ils s'écartent.", Color.Cyan1);
                return;
            }

            if (isPirate)
            {
                // 40% : combat spatial direct, 60% : rencontre avec choix
                if (Rng.Next(100) < 40 && !cls.AutoKillsPirates)
                {
                    var spaceEnemy = SpaceCombat.GetForStation(state.CurrentStation);
                    AnnounceSpaceCombat(state, spaceEnemy);
                    var outcome = SpaceCombat.Start(state, spaceEnemy);
                    Situations.ApplyCombatOutcome(state, outcome);
                }
                else
                {
                    var stolen = Rng.Next(150, 600);
                    ChoiceMenu.Resolve(Situations.PirateEncounter(stolen), state);
                }
                state.Fuel = Math.Clamp(state.Fuel, 0, state.MaxFuel);
                return;
            }

            if (isPositive && cls.CursedEvents && Rng.Next(2) == 0)
            {
                Display.ShowEvent("Quelque chose de bien a failli arriver. Mais non.", Color.Grey);
                return;
            }

            // Événements avec choix
            if (message.Contains("cargaison") && isPositive && Rng.Next(100) < 30)
            {
                ChoiceMenu.Resolve(Situations.MysteriousCapsule(), state);
                return;
            }

            if (!isPositive && Rng.Next(100) < 15 && !isPirate)
            {
                ChoiceMenu.Resolve(Situations.MerchantDistress(), state);
                return;
            }

            apply(state);
            state.Fuel = Math.Clamp(state.Fuel, 0, state.MaxFuel);
            if (!string.IsNullOrEmpty(message))
                Display.ShowEvent(message, color);
            return;
        }
    }

    // Événement de voyage spécifique à une quête de livraison active.
    static void TriggerQuestTravelEvent(GameState state, Quest q)
    {
        var item = q.TargetItem!;
        var roll = Rng.Next(5);

        AnsiConsole.WriteLine();

        switch (roll)
        {
            case 0:
                // Quelqu'un sait ce que tu transportes et fait une offre
                var offer = (int)(q.CreditReward * Rng.NextDouble() * 0.8 + q.CreditReward * 0.6);
                Narrator.Say($"Un vaisseau inconnu t'approche sur fréquence privée. 'On sait que tu transportes {item}. On offre {offer}cr. Maintenant. Pas de questions.' {q.Giver} t'en donnait {q.CreditReward}cr.", Color.OrangeRed1);

                ChoiceMenu.Resolve(new Situation("Que fais-tu ?",
                [
                    new($"Accepter l'offre — {offer}cr maintenant", s =>
                    {
                        s.Credits += offer;
                        s.Cargo.Remove(item, 1);
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation -= 15;
                        Narrator.Say($"+{offer}cr. -{15} rép. {q.Giver} ne le saura que quand il attendait en vain.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new($"Refuser — rester loyal à {q.Giver}", s =>
                    {
                        if (Rng.Next(100) < 40)
                        {
                            s.Reputation += 20;
                            Narrator.Say($"Ils insistent, puis disparaissent. {q.Giver} saura que tu as résisté. +20 rép.", Color.Green);
                        }
                        else
                        {
                            Narrator.Say("Ils s'éloignent en silence. Peut-être qu'ils attendent la prochaine occasion.", Color.Grey);
                        }
                        Narrator.Pause();
                    }),
                    new("Négocier — demander plus", s =>
                    {
                        var counter = (int)(q.CreditReward * 1.2);
                        if (Rng.Next(100) < 45)
                        {
                            s.Credits += counter;
                            s.Cargo.Remove(item, 1);
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                            s.Reputation -= 20;
                            Narrator.Say($"Ils acceptent. +{counter}cr. Mieux que {q.Giver} mais ta réputation en prend un coup. -20 rép.", Color.Gold1);
                        }
                        else
                        {
                            Narrator.Say("Ils coupent la communication. L'offre est caduque.", Color.Grey);
                        }
                        Narrator.Pause();
                    }),
                ], Color.OrangeRed1), state);
                break;

            case 1:
                // Pirates qui savent exactement ce que tu transportes
                Narrator.Say($"Interception. Ils savent ce que tu transportes. 'Le {item} — donne-le ou on prend tout.'", Color.Red);

                ChoiceMenu.Resolve(new Situation("Pirates ciblés. Ils veulent ton colis.",
                [
                    new($"Donner le {item} — perdre la quête", s =>
                    {
                        s.Cargo.Remove(item, 1);
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation -= 10;
                        Narrator.Say($"Tu leur donnes le colis. Ils repartent. La quête est perdue. -10 rép.", Color.Red);
                        Narrator.Pause();
                    }),
                    new("Combattre pour garder la livraison", s =>
                    {
                        var enemy = Combat.GetScaled(s, 1);
                        var outcome = Combat.Start(s, enemy);
                        if (outcome == CombatOutcome.Victory)
                        {
                            s.Reputation += 10;
                            Narrator.Say($"Repoussé. Le {item} est intact. +10 rép. La livraison peut continuer.", Color.Green);
                        }
                        else
                        {
                            s.Cargo.Remove(item, 1);
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                            Situations.ApplyCombatOutcome(s, outcome);
                        }
                    }),
                    new("Fuir à pleine puissance", s =>
                    {
                        if (s.Fuel > 1 && Rng.Next(100) < 60)
                        {
                            s.Fuel--; s.Fuel = Math.Max(0, s.Fuel);
                            Narrator.Say($"Tu files. -{1} carburant. Le {item} est intact.", Color.Yellow);
                        }
                        else
                        {
                            s.Cargo.Remove(item, 1);
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                            Narrator.Say("Rattrapé. Ils prennent le colis et te laissent partir.", Color.Red);
                        }
                        Narrator.Pause();
                    }, s => s.Fuel > 0),
                ], Color.Red), state);
                break;

            case 2:
                // Checkpoint — la marchandise est suspecte
                Narrator.Say($"Un checkpoint de patrouille. Ils scannent ta cargaison. '{item} à bord. Permis de transport ?'", Color.Yellow);

                ChoiceMenu.Resolve(new Situation("Contrôle en transit.",
                [
                    new("Montrer le contrat de livraison", s =>
                    {
                        if (s.Reputation >= 0)
                        {
                            Narrator.Say("Le contrat suffit. Ils te laissent passer. Bonne route.", Color.Green);
                        }
                        else
                        {
                            s.Reputation -= 5;
                            Narrator.Say("Ton casier les rend méfiants. Ils notent le trajet. -5 rép.", Color.Yellow);
                        }
                        Narrator.Pause();
                    }),
                    new("Corrompre le garde", s =>
                    {
                        var bribe = Rng.Next(80, 250);
                        if (s.Credits >= bribe && Rng.Next(100) < 65)
                        {
                            s.Credits -= bribe;
                            Narrator.Say($"Il glisse la main. -{bribe}cr. Tu passes sans qu'ils regardent vraiment.", Color.Gold1);
                        }
                        else if (s.Credits < bribe)
                        {
                            s.Reputation -= 10;
                            Narrator.Say("T'as pas assez. Et maintenant ils te regardent encore plus. -10 rép.", Color.Red);
                        }
                        else
                        {
                            s.IsImprisoned = true;
                            s.Reputation -= 20;
                            Narrator.Say("Il prend l'argent. Et appelle ses collègues. -20 rép. Prison.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Mentir — c'est pour usage personnel", s =>
                    {
                        var bonus = s.Class.Name == "Contrebandier" ? 25 : s.Class.Name == "Hackeur" ? 15 : 0;
                        if (Rng.Next(100) < 35 + bonus)
                        {
                            Narrator.Say("Il hausse les épaules. 'Bonne route.' Tu passes.", Color.Green);
                        }
                        else
                        {
                            s.Reputation -= 15;
                            s.IsImprisoned = true;
                            Narrator.Say("Ton histoire tient pas. -15 rép. Ils t'embarquent.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                ], Color.Yellow), state);
                break;

            case 3:
                // La marchandise est endommagée en route
                Narrator.Say($"Une turbulence ionique traverse ta cargaison. Le {item} est partiellement endommagé.", Color.Yellow);

                ChoiceMenu.Resolve(new Situation("La marchandise est abîmée. Comment tu gères la livraison ?",
                [
                    new($"Livrer quand même — expliquer la situation à {q.Giver}", s =>
                    {
                        var reduced = q.CreditReward / 2;
                        s.Credits += reduced;
                        s.Reputation += 5;
                        s.Cargo.Remove(item, 1);
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        Narrator.Say($"Il est pas content mais il accepte. +{reduced}cr (moitié). +5 rép pour l'honnêteté.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("Chercher un réparateur en route — perdre du temps", s =>
                    {
                        var cost = Rng.Next(100, 400);
                        if (s.Credits >= cost)
                        {
                            s.Credits -= cost;
                            Narrator.Say($"Tu trouves quelqu'un qui arrange ça. -{cost}cr. La livraison se fera au tarif normal.", Color.Green);
                        }
                        else
                        {
                            Narrator.Say("T'as pas les crédits pour réparer. Tu vas devoir livrer comme ça.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                    new("Livrer sans rien dire — espérer qu'il voit pas", s =>
                    {
                        if (Rng.Next(100) < 45)
                        {
                            s.Credits += q.CreditReward;
                            s.Reputation += q.RepReward;
                            s.Cargo.Remove(item, 1);
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                            Narrator.Say($"Il remarque rien. Ou il fait semblant. +{q.CreditReward}cr, +{q.RepReward} rép.", Color.Gold1);
                        }
                        else
                        {
                            s.Reputation -= 25;
                            Narrator.Say($"Il voit l'état du {item}. 'Vous vous foutez de moi ?' -25 rép. Pas de paiement.", Color.Red);
                            s.Cargo.Remove(item, 1);
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                        }
                        Narrator.Pause();
                    }),
                ], Color.Yellow), state);
                break;

            default:
                // Un informateur te prévient que la destination est dangereuse
                Narrator.Say($"Un message anonyme : 'La livraison pour {q.Giver} à {q.TargetStation} — ils t'attendent. Pas pour te payer. Sois prudent.'", Color.Grey);

                ChoiceMenu.Resolve(new Situation("Quelqu'un te prévient. Est-ce un piège ?",
                [
                    new("Continuer quand même — le risque fait partie du job", s =>
                    {
                        Narrator.Say("Tu continues. Ce que tu trouveras là-bas, tu le trouveras.", Color.Grey);
                        Narrator.Pause();
                    }),
                    new("Faire demi-tour — abandonner cette quête", s =>
                    {
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation -= 10;
                        Narrator.Say($"Tu abandonnes la livraison. -{10} rép. Mieux vaut ça que ce qui t'attendait peut-être.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("Enquêter — contacter directement {q.Giver}", s =>
                    {
                        if (Rng.Next(100) < 55)
                        {
                            s.Reputation += 10;
                            Narrator.Say("Il confirme que tout va bien. L'informateur voulait peut-être juste semer le doute. +10 rép.", Color.Green);
                        }
                        else
                        {
                            s.Reputation += 15;
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                            Narrator.Say("Pas de réponse. L'informateur avait raison. Tu abandonnes prudemment. +15 rép pour l'instinct.", Color.Yellow);
                        }
                        Narrator.Pause();
                    }),
                ], Color.Grey), state);
                break;
        }
    }

    // ── CONTRAT D'ASSASSINAT ─────────────────────────────────────────────────
    static void TriggerKillQuestTravelEvent(GameState state, Quest q)
    {
        AnsiConsole.WriteLine();
        switch (Rng.Next(5))
        {
            case 0:
                // La cible a entendu parler du contrat
                Narrator.Say($"Un message crypté sur ta fréquence privée. C'est {q.TargetStation}. 'J'ai entendu parler du contrat sur moi. On peut s'arranger autrement.'", Color.OrangeRed1);
                ChoiceMenu.Resolve(new Situation("La cible te contacte avant que tu arrives.",
                [
                    new("Écouter l'offre", s =>
                    {
                        var bribe = Rng.Next(1000, 3000);
                        Narrator.Say($"'Ne viens pas. {bribe}cr. Et j'efface la dette avec {q.Giver}.'", Color.Yellow);
                        ChoiceMenu.Resolve(new Situation("Tu acceptes ?",
                        [
                            new($"Accepter — {bribe}cr et oublier le contrat", gs =>
                            {
                                gs.Credits += bribe;
                                gs.ActiveQuests.Remove(q);
                                gs.CompletedQuestIds.Add(q.Id);
                                gs.Reputation -= 20;
                                Narrator.Say($"+{bribe}cr. -{20} rép. {q.Giver} saura que tu as abandonné. La prochaine fois il ira voir quelqu'un d'autre.", Color.OrangeRed1);
                                Narrator.Pause();
                            }),
                            new("Refuser — le contrat tient", gs =>
                            {
                                gs.Reputation += 10;
                                Narrator.Say("'Dommage.' Il coupe. +10 rép. Il sera prêt quand tu arriveras.", Color.Green);
                                Narrator.Pause();
                            }),
                        ], Color.Yellow), s);
                    }),
                    new("Ignorer le message", s => { Narrator.Say("Tu coupes la communication. Le contrat tient.", Color.Grey); Narrator.Pause(); }),
                ], Color.OrangeRed1), state);
                break;

            case 1:
                // Un autre chasseur sur le même contrat
                Narrator.Say($"Un autre chasseur te contacte. 'J'ai le même contrat que toi sur {q.TargetStation}. Premier arrivé, premier servi. À moins qu'on s'entende.'", Color.Red);
                ChoiceMenu.Resolve(new Situation("Un rival sur le même contrat.",
                [
                    new("S'associer — partager la récompense", s =>
                    {
                        if (Rng.Next(100) < 55)
                        {
                            var share = q.CreditReward / 2;
                            s.Credits += share;
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                            s.Reputation += q.RepReward / 2;
                            Narrator.Say($"L'association tient. Il fait le sale boulot, tu prends la moitié. +{share}cr, +{q.RepReward / 2} rép.", Color.Gold1);
                        }
                        else
                        {
                            s.Reputation -= 15;
                            Narrator.Say("Il prend tout et disparaît sans te donner ta part. -15 rép. Tu t'es fait avoir.", Color.Red);
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                        }
                        Narrator.Pause();
                    }),
                    new("L'éliminer — le contrat est pour toi seul", s =>
                    {
                        var enemy = Combat.GetScaled(s, 1);
                        var outcome = Combat.Start(s, enemy);
                        Situations.ApplyCombatOutcome(s, outcome);
                        if (outcome == CombatOutcome.Victory)
                            Narrator.Say("Concurrent éliminé. Le contrat est à toi seul.", Color.Gold1);
                    }),
                    new("Le laisser faire — abandonner cette mission", s =>
                    {
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation -= 10;
                        Narrator.Say("Tu laisses tomber. -10 rép.", Color.Grey);
                        Narrator.Pause();
                    }),
                ], Color.Red), state);
                break;

            case 2:
                // Un ami de la cible essaie de t'intercepter
                Narrator.Say($"Des hommes armés bloquent ta route. 'Tu vas sur {q.TargetStation} ? Demi-tour. Maintenant.'", Color.Red);
                ChoiceMenu.Resolve(new Situation("Des protecteurs de la cible t'interceptent.",
                [
                    new("Forcer le passage — se battre", s =>
                    {
                        var enemy = Combat.GetScaled(s, 2);
                        var outcome = Combat.Start(s, enemy);
                        Situations.ApplyCombatOutcome(s, outcome);
                        if (outcome == CombatOutcome.Victory)
                        { s.Reputation += 15; Narrator.Say("Tu passes. +15 rép.", Color.Gold1); }
                    }),
                    new("Faire demi-tour provisoirement", s =>
                    {
                        Narrator.Say("Tu attends qu'ils se dispersent. Tu perdras du temps mais le contrat tient.", Color.Grey);
                        Narrator.Pause();
                    }),
                    new("Négocier — proposer de laisser partir la cible", s =>
                    {
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        var compens = Rng.Next(300, 800); s.Credits += compens;
                        Narrator.Say($"Ils apprécient. +{compens}cr. La quête est abandonnée mais tu repars intact.", Color.Yellow);
                        Narrator.Pause();
                    }),
                ], Color.Red), state);
                break;

            case 3:
                // Le donneur de quête change les conditions
                Narrator.Say($"{q.Giver} te recontacte. 'Changement de plan. La cible doit survivre. Ramène-la vivante. Même récompense.'", Color.Yellow);
                ChoiceMenu.Resolve(new Situation("Contrat modifié en route.",
                [
                    new("Accepter la modification", s =>
                    {
                        s.Reputation += 10;
                        Narrator.Say("Tu notes. La cible doit survivre. Ça complique les choses. +10 rép pour ta flexibilité.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Refuser — le contrat original ou rien", s =>
                    {
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation -= 10;
                        Narrator.Say($"{q.Giver} coupe le contrat. -10 rép.", Color.Red);
                        Narrator.Pause();
                    }),
                ], Color.Yellow), state);
                break;

            default:
                // Intel sur la cible
                var intel = Rng.Next(3);
                var msgs = new[]
                {
                    $"Une source te vend une info : la cible sur {q.TargetStation} est entourée de gardes depuis une semaine. Elle sait.",
                    $"Un ancien associé te laisse un message : '{q.TargetStation} est un piège. La cible t'attend. Bonne chance.'",
                    $"Tu croises quelqu'un qui connaît ta cible. 'Elle est imprévisible. Et bien armée. T'as intérêt à être prêt.'"
                };
                Narrator.Say(msgs[intel], Color.Grey);
                Narrator.Pause();
                break;
        }
    }

    // ── MISSION DE VENGEANCE ─────────────────────────────────────────────────
    static void TriggerRevengeQuestTravelEvent(GameState state, Quest q)
    {
        AnsiConsole.WriteLine();
        switch (Rng.Next(4))
        {
            case 0:
                // La personne visée contacte le joueur
                Narrator.Say($"Message entrant. '{q.Giver} t'a engagé pour venir sur {q.TargetStation}. Combien il t'a payé ? Je double. Oublie ça.'", Color.OrangeRed1);
                ChoiceMenu.Resolve(new Situation("La cible de la vengeance te contacte.",
                [
                    new("Accepter le contre-paiement", s =>
                    {
                        var counter = q.CreditReward + Rng.Next(200, 800);
                        s.Credits += counter;
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation -= 25;
                        Narrator.Say($"+{counter}cr. -{25} rép. {q.Giver} a payé pour rien et va le savoir.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                    new($"Rester loyal à {q.Giver}", s =>
                    {
                        s.Reputation += 15;
                        Narrator.Say("'Non.' Tu coupes. +15 rép. {q.Giver} apprendra que tu tiens tes engagements.", Color.Green);
                        Narrator.Pause();
                    }),
                ], Color.OrangeRed1), state);
                break;

            case 1:
                // {q.Giver} te donne plus d'infos sur la vraie raison de la vengeance
                Narrator.Say($"{q.Giver} te recontacte. Il voulait pas tout dire. 'Ce qu'ils m'ont fait... c'est pire que ce que je t'ai dit. Tu dois savoir.'", Color.Grey);
                switch (Rng.Next(3))
                {
                    case 0:
                        state.Reputation += 5;
                        Narrator.Say("L'histoire est sordide. Mais ça solidifie ta résolution. +5 rép.", Color.Yellow);
                        break;
                    case 1:
                        Narrator.Say("En fait c'est {q.Giver} qui avait tort. Moralement tu te retrouves dans une zone grise.", Color.Grey);
                        break;
                    case 2:
                        var bonus = Rng.Next(300, 700); state.Credits += bonus;
                        Narrator.Say($"Il te paie un supplément de culpabilité. +{bonus}cr.", Color.Gold1);
                        break;
                }
                Narrator.Pause();
                break;

            case 2:
                // Témoins qui te disent de laisser tomber
                Narrator.Say("Des gens de la station que tu dois visiter t'approchent. 'Laisse tomber cette histoire. {q.Giver} est pas innocent non plus. Vous allez vous entretuer pour rien.'", Color.Grey);
                ChoiceMenu.Resolve(new Situation("On te demande de laisser tomber.",
                [
                    new("Continuer quand même", s => { Narrator.Say("Tu hoches la tête. La quête tient.", Color.Grey); Narrator.Pause(); }),
                    new("Abandonner — peut-être qu'ils ont raison", s =>
                    {
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation += 10;
                        Narrator.Say("Tu laisses tomber. Peut-être la bonne décision. +10 rép.", Color.Green);
                        Narrator.Pause();
                    }),
                ], Color.Grey), state);
                break;

            default:
                // Embuscade d'associés de la cible
                Narrator.Say($"Des hommes de {q.TargetStation} t'attendent en chemin. La nouvelle de ta mission les a précédés.", Color.Red);
                var enemy = Combat.GetScaled(state, 1);
                var outcome = Combat.Start(state, enemy);
                Situations.ApplyCombatOutcome(state, outcome);
                if (outcome == CombatOutcome.Victory)
                { state.Reputation += 10; Narrator.Say("Embuscade repoussée. +10 rép.", Color.Gold1); }
                break;
        }
    }

    // ── MISSION D'INFO ───────────────────────────────────────────────────────
    static void TriggerInfoQuestTravelEvent(GameState state, Quest q)
    {
        AnsiConsole.WriteLine();
        switch (Rng.Next(4))
        {
            case 0:
                // Quelqu'un sait que tu collectes des infos et veut les acheter
                Narrator.Say($"Un inconnu te contacte. 'Tu vas sur {q.TargetStation} pour {q.Giver}. Ce que tu vas trouver là-bas m'intéresse aussi. Travail en double. Je paie bien.'", Color.Yellow);
                ChoiceMenu.Resolve(new Situation("Un acheteur concurrent sur les mêmes infos.",
                [
                    new("Accepter de vendre à deux — doubler les revenus", s =>
                    {
                        s.Reputation -= 10;
                        Narrator.Say($"Double client, double risque. -{10} rép mais si tu livres à tous les deux, tu encaisses double.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new($"Rester exclusif à {q.Giver}", s =>
                    {
                        s.Reputation += 5;
                        Narrator.Say("'Non.' Tu coupe. +5 rép.", Color.Green);
                        Narrator.Pause();
                    }),
                    new("Vendre à l'inconnu et abandonner la mission originale", s =>
                    {
                        var alt = Rng.Next(q.CreditReward, (int)(q.CreditReward * 1.5));
                        s.Credits += alt;
                        s.ActiveQuests.Remove(q);
                        s.CompletedQuestIds.Add(q.Id);
                        s.Reputation -= 15;
                        Narrator.Say($"+{alt}cr. -{15} rép. {q.Giver} attendra.", Color.OrangeRed1);
                        Narrator.Pause();
                    }),
                ], Color.Yellow), state);
                break;

            case 1:
                // L'info que tu cherches est déjà partiellement connue
                Narrator.Say($"Tu croises quelqu'un qui connaît {q.TargetStation}. Il lâche une info sans même que tu demandes.", Color.Cyan1);
                var cr = Rng.Next(100, 400); state.Credits += cr;
                state.Reputation += 5;
                Narrator.Say($"Tu revends ça à {q.Giver} comme avant-goût. +{cr}cr, +5 rép. La quête continue.", Color.Cyan1);
                Narrator.Pause();
                break;

            case 2:
                // {q.Giver} t'avertit que c'est plus dangereux que prévu
                Narrator.Say($"{q.Giver} te recontacte. 'Ce que tu vas chercher... les gens là-bas veulent pas que ça sorte. Sois discret.'", Color.Grey);
                ChoiceMenu.Resolve(new Situation("La mission est plus risquée que prévu.",
                [
                    new("Continuer — le risque est dans le prix", s => { Narrator.Say("Noté. Tu continues.", Color.Grey); Narrator.Pause(); }),
                    new("Renégocier le tarif avant d'y aller", s =>
                    {
                        if (Rng.Next(100) < 55)
                        {
                            var extra = Rng.Next(200, 600);
                            s.Credits += extra;
                            Narrator.Say($"Il accepte le supplément de risque. +{extra}cr.", Color.Gold1);
                        }
                        else
                        {
                            s.ActiveQuests.Remove(q);
                            s.CompletedQuestIds.Add(q.Id);
                            s.Reputation -= 5;
                            Narrator.Say("Il coupe le contrat. 'Trouve quelqu'un de moins cher.' -5 rép.", Color.Red);
                        }
                        Narrator.Pause();
                    }),
                ], Color.Grey), state);
                break;

            default:
                // Fausse piste — quelqu'un essaie de détourner ta mission
                Narrator.Say($"Un message anonyme : 'Les infos sur {q.TargetStation} — elles sont à {q.TargetStation} mais pas là où tu crois. Cherche dans les niveaux inférieurs.'", Color.Grey);
                Narrator.Say("C'est peut-être vrai. Ou quelqu'un veut te faire perdre du temps.", Color.Grey);
                Narrator.Pause();
                break;
        }
    }

    // Coûts qui tombent à CHAQUE jour qui passe (voyage OU actions de terrain).
    // Appelé par Clock.AdvanceDay.
    public static void ApplyDailyCosts(GameState state)
    {
        // Addiction
        if (state.AddictionLevel > 0)
        {
            state.AddictionDaysSinceDose++;
            var cost = state.AddictionDailyCost;

            if (state.Credits >= cost)
            {
                state.Credits -= cost;
                Display.ShowEvent($"Ton addiction te coûte {cost}cr aujourd'hui.", Color.OrangeRed1);
            }
            else
            {
                // Manque — effets négatifs
                var withdrawalDmg = state.AddictionLevel * 5;
                state.PlayerHp = Math.Max(1, state.PlayerHp - withdrawalDmg);
                state.Reputation -= 5;
                Display.ShowEvent($"Manque. -{withdrawalDmg} PV joueur, -5 réputation. T'as pas les crédits pour ta dose.", Color.Red);
            }

            // Le manque prolongé empire l'état
            if (state.AddictionDaysSinceDose >= 3)
            {
                state.PlayerHp = Math.Max(1, state.PlayerHp - 10);
                Display.ShowEvent("Sevrage prolongé. -10 PV supplémentaires.", Color.Red);
            }
        }

        var cls = state.Class;

        if (cls.DailyDebt > 0)
        {
            state.Credits -= cls.DailyDebt;
            state.Credits = Math.Max(0, state.Credits);
            Display.ShowEvent($"Ton créancier prélève {cls.DailyDebt}cr. Comme d'hab.", Color.Red);
        }

        if (cls.PeriodicIncome > 0 && state.Day % 5 == 0)
        {
            state.Credits += cls.PeriodicIncome;
            Display.ShowEvent($"Le virement familial est arrivé. +{cls.PeriodicIncome}cr.", Color.Green);
        }

        // ── Maintenance quotidienne du vaisseau ───────────────────────────
        // Base 50cr/jour + 0.5cr par PV max au-dessus de 100 (vaisseau amélioré coûte plus)
        // Premier jour gratuit — on vient de partir.
        if (state.Day > 1)
        {
            var maintenanceCost = 50 + Math.Max(0, state.ShipMaxHp - 100) / 2;

            if (state.Credits >= maintenanceCost)
            {
                state.Credits -= maintenanceCost;
                Display.ShowEvent($"Maintenance vaisseau : -{maintenanceCost}cr.", Color.Grey);
            }
            else
            {
                // Pas les moyens — le vaisseau se dégrade
                var degradation = Rng.Next(5, 20);
                state.ShipHp = Math.Max(1, state.ShipHp - degradation);
                Display.ShowEvent(
                    $"Maintenance impayée — le vaisseau se dégrade. -{degradation} PV vaisseau. " +
                    $"({state.ShipHp}/{state.ShipMaxHp} PV)", Color.Red);
            }
        }

        // ── Inflation — avertissement ─────────────────────────────────────
        if (state.Day == 10)
            Display.ShowEvent("Les prix dans les marchés commencent à augmenter avec le temps.", Color.Yellow);
        if (state.Day == 25)
            Display.ShowEvent("L'inflation s'est accentuée. Les marchés sont nettement plus chers.", Color.OrangeRed1);
    }

    // Effets liés UNIQUEMENT au fait de voyager (en plus des coûts quotidiens).
    public static void ApplyTravelEffects(GameState state)
    {
        // Rivaux persistants — apparaissent en voyage
        if (NpcTracker.MaybeRivalEncounter(state)) return;

        var cls = state.Class;

        if (cls.TravelCreditCost > 0)
        {
            state.Credits -= cls.TravelCreditCost;
            state.Credits = Math.Max(0, state.Credits);
            Display.ShowEvent($"Tu dépenses {cls.TravelCreditCost}cr pour tes habitudes.", Color.OrangeRed1);
        }

        if (cls.CargoDegrades && state.Cargo.All.Any() && Rng.Next(100) < 30)
        {
            var item = state.Cargo.All.Keys.ElementAt(Rng.Next(state.Cargo.All.Count));
            state.Cargo.Remove(item, 1);
            Display.ShowEvent($"Ton vaisseau s'est encore un peu désintégré. Perdu 1x {item}.", Color.OrangeRed1);
        }

        // Parasite spatial (Ferrailleur plus exposé)
        if (cls.Name == "Ferrailleur" && Rng.Next(100) < 20)
        {
            state.ShipHp -= 5;
            state.ShipHp = Math.Max(1, state.ShipHp);
            Display.ShowEvent("Ton vaisseau perd encore un morceau en route. -5 PV vaisseau.", Color.OrangeRed1);
        }
    }

    // ── ANNONCE COMBAT SPATIAL ────────────────────────────────────────────────

    static void AnnounceSpaceCombat(GameState state, SpaceEnemy enemy, string? extraMsg = null)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red bold]⚠  ALERTE VAISSEAU  ⚠[/]").RuleStyle("red"));
        AnsiConsole.WriteLine();
        Thread.Sleep(500);

        AnsiConsole.MarkupLine("[red]Signal radar non identifié sur trajectoire d'interception.[/]");
        Thread.Sleep(400);
        AnsiConsole.MarkupLine($"[red bold]Contact confirmé — {enemy.Name}.[/]");
        Thread.Sleep(400);
        AnsiConsole.MarkupLine($"[grey]{enemy.Description}[/]");
        Thread.Sleep(300);

        if (extraMsg != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[orange1]{extraMsg}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey dim]Boucliers en ligne. Systèmes d'armement actifs.[/]");
        Thread.Sleep(600);
        Narrator.Pause();
    }

    static void TriggerBountyHunter(GameState state)
    {
        var tier    = state.Reputation <= -800 ? Combat.TierHigh : Combat.TierMid;
        var hunter  = tier[Rng.Next(tier.Count)];
        var prime   = Math.Abs(state.Reputation) * 2;

        var spaceHunter = SpaceCombat.GetForStation(state.CurrentStation);
        AnnounceSpaceCombat(state, spaceHunter, $"Un chasseur de primes. Ta tête vaut {prime}cr.");

        ChoiceMenu.Resolve(new Situation("Un chasseur de primes bloque ta route.", new List<Choice>
        {
            new("Combattre",
                s =>
                {
                    var outcome = SpaceCombat.Start(s, SpaceCombat.GetForStation(s.CurrentStation));
                    if (outcome == CombatOutcome.Victory)
                    {
                        var loot = Rng.Next(300, 900);
                        s.Credits += loot;
                        s.Reputation += 15;
                        Display.ShowEvent($"Chasseur neutralisé. +{loot}cr. +15 réputation.", Color.Gold1);
                    }
                    else Situations.ApplyCombatOutcome(s, outcome);
                }),

            new("Fuir à pleine puissance",
                s =>
                {
                    var fuitChance = 45 + (s.Fuel > 3 ? 20 : 0);
                    if (Rng.Next(100) < fuitChance)
                    {
                        s.Fuel = Math.Max(0, s.Fuel - 2);
                        Display.ShowEvent("Tu sèmes le chasseur. -2 carburant.", Color.Yellow);
                    }
                    else
                    {
                        s.ShipHp = Math.Max(1, s.ShipHp - Rng.Next(20, 45));
                        Display.ShowEvent("Il te rattrape et ouvre le feu avant que tu décroches. -PV vaisseau.", Color.Red);
                    }
                    Narrator.Pause();
                },
                s => s.Fuel > 0),

            new($"Payer la prime ({prime}cr)",
                s =>
                {
                    s.Credits = Math.Max(0, s.Credits - prime);
                    s.Reputation += 30;
                    Display.ShowEvent($"Tu règles la prime. -{prime}cr. +30 réputation. Il te laisse passer.", Color.Yellow);
                    Narrator.Pause();
                },
                s => s.Credits >= prime),

            new("Négocier — lui proposer une information",
                s =>
                {
                    var chance = 25 + Math.Max(0, s.Reputation / 5);
                    if (Rng.Next(100) < chance)
                    {
                        s.Reputation -= 20;
                        Display.ShowEvent("Il accepte l'échange. Tu livres quelqu'un d'autre. -20 réputation.", Color.OrangeRed1);
                    }
                    else
                    {
                        s.ShipHp = Math.Max(1, s.ShipHp - Rng.Next(15, 35));
                        Display.ShowEvent("Il n'a pas besoin d'information. Il a besoin de ta prime. -PV vaisseau.", Color.Red);
                    }
                    Narrator.Pause();
                }),
        }, Color.Red), state);
    }

    private static readonly Random _rng = new();
}
