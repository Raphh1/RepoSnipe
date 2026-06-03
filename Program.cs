using Spectre.Console;
using VoidTrader;

var state = new GameState();
var Rng   = new Random();

Display.ShowTitle();
Display.ShowClass(state.Class);
AnsiConsole.WriteLine();

// Situation initiale à la station de départ
var startStation = Universe.Get(state.CurrentStation);
AnsiConsole.MarkupLine($"[grey]{startStation.Description}[/]");
ChoiceMenu.Resolve(Situations.Arrival(state), state);

while (true)
{
    AnsiConsole.WriteLine();
    Display.ShowStatus(state);
    AnsiConsole.WriteLine();

    ChoiceMenu.Resolve(MainMenu(state), state);

    // Prison
    if (state.IsImprisoned)
        Prison.Enter(state);

    Objectives.CheckAll(state);

    if (state.IsDead)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Spectre.Console.Rule("[red bold]TU ES MORT[/]").RuleStyle("red"));
        AnsiConsole.MarkupLine($"[grey]{state.DeathCause}[/]");
        AnsiConsole.MarkupLine($"[grey]Survécu [white]{state.Day} jours[/] avec [yellow]{state.Credits}cr[/]. Le vide ne retient pas les noms.[/]");
        break;
    }

    // Filet de sécurité : si les PV tombent à 0 hors d'une issue de combat gérée.
    if (state.PlayerHp <= 0)
    {
        state.PlayerHp = Math.Max(1, state.PlayerMaxHp / 4);
        state.Stamina  = 0;
        state.Credits  = Math.Max(0, state.Credits - Rng.Next(200, 600));
        Narrator.Say("Tu reprends conscience. Tu as été assommé et dévalisé.", Color.Red);
        Narrator.Pause();
    }

    if (state.ShipHp <= 0)
    {
        AnsiConsole.MarkupLine("\n[red]Ton vaisseau est détruit. La run s'arrête ici.[/]");
        AnsiConsole.MarkupLine($"[grey]Survécu [white]{state.Day} jours[/] avec [yellow]{state.Credits}cr[/].[/]");
        break;
    }

    if (state.Fuel <= 0 && state.Credits < 80)
    {
        AnsiConsole.MarkupLine("\n[red]Plus de carburant. Plus de crédits. Tu dériveras dans le vide pour l'éternité.[/]");
        AnsiConsole.MarkupLine($"[grey]Survécu [white]{state.Day} jours[/].[/]");
        break;
    }
}


// ── MENU PRINCIPAL ──────────────────────────────────────────────────────────

static Situation MainMenu(GameState state)
{
    return new Situation("Que fais-tu ?", new List<Choice>
    {
        new("Voyager vers une autre station",   s => MenuTravel(s),                                                                          Category: "Voyage"),
        new("Aller au marché",                  s => { Narrator.Say("Tu te diriges vers le marché..."); Menus.OpenMarket(s); },              Category: "Commerce"),
        new("Ravitailler en carburant",         s => { Narrator.Say("Direction la station-service..."); MenuRefuel(s); Narrator.Pause(); },  s => s.Fuel < s.MaxFuel, Category: "Commerce"),
        new("Voir ma cargaison",                s => { Display.ShowCargo(s); Narrator.Pause(); },                                            Category: "Inventaire"),
        new("Gérer les armes",                  s => Menus.ManageWeapons(s),                                                                 s => s.Weapons.Any(), "Inventaire"),
        new("Gérer les armures",                s => Menus.ManageArmors(s),                                                                  s => s.Armors.Any(),  "Inventaire"),
        new("Se soigner",                       s => Menus.HealOutsideCombat(s),                                                             s => s.Cargo.Get("Médicaments") > 0 && s.PlayerHp < s.PlayerMaxHp, "Inventaire"),
        new("Explorer la zone",                 s => { Exploration.Explore(s); Clock.SpendAction(s); },                                      Category: "Exploration"),
        new("Traîner dans le coin",             s => { Encounters.Roll(s); Clock.SpendAction(s); },                                          Category: "Exploration"),
        new("Chercher du carburant",            s => { Situations.ResolveScroungeFuel(s); Clock.SpendAction(s); },                           s => s.Fuel == 0, "Survie"),
        new("Objectifs",                        s => { Objectives.Show(s); Narrator.Pause(); }),
        new("Arcs narratifs",                   s => { NarrativeArcs.ShowActiveArcs(s); Narrator.Pause(); },
            s => s.ActiveArcs.Any() || s.CompletedArcs.Any()),
        new("Quêtes actives",                   s => { QuestSystem.Show(s); },
            s => s.ActiveQuests.Any()),
        new("Consulter le wiki",                s => MenuWiki(s)),
        new("Quitter le jeu",                   s =>
        {
            AnsiConsole.MarkupLine($"\n[grey]Tu as survécu [white]{s.Day} jours[/] et terminé avec [yellow]{s.Credits}cr[/]. À bientôt dans le vide.[/]");
            System.Environment.Exit(0);
        }),
    });
}

// ── VOYAGE ──────────────────────────────────────────────────────────────────

static void MenuTravel(GameState state)
{
    var current    = Universe.Get(state.CurrentStation);
    var accessible = Universe.AccessibleFrom(state.CurrentStation, state);

    if (!accessible.Any())
    {
        AnsiConsole.MarkupLine("[red]Aucune destination accessible.[/]");
        return;
    }

    var choices = accessible.Select(s =>
    {
        var dist      = Universe.DistanceMkm(current, s);
        var cost      = Universe.FuelCost(current, s);
        var banned    = Universe.IsBanned(s, state);
        var canAfford = cost <= state.Fuel && !banned;

        var label = $"[steelblue1]{s.Name}[/]  {Universe.DangerBadge(s)}  [grey]{dist:N0}M km — {cost} carburant[/]";
        if (banned)    label += " [red](refusé)[/]";
        if (!canAfford && !banned) label += " [red](carburant insuffisant)[/]";
        if (state.Class.SeesPrices)
        {
            var prices = string.Join(", ", s.Goods.Select(g => $"{g} {Market.GetPrice(g)}cr"));
            label += $" [grey dim]({prices})[/]";
        }

        return new Choice(label,
            gs =>
            {
                if (banned) { AnsiConsole.MarkupLine("[red]Ta classe n'est pas la bienvenue ici.[/]"); return; }
                if (cost > gs.Fuel) { AnsiConsole.MarkupLine("[red]Carburant insuffisant.[/]"); return; }

                Narrator.Say($"Tu mets le cap sur {s.Name}. Le moteur vrombît. Le vide s'ouvre devant toi...", Spectre.Console.Color.SteelBlue1);
                Thread.Sleep(800);

                gs.Fuel -= cost;
                gs.CurrentStation = s.Name;
                gs.ZoneDepth = 0;            // nouvelle zone : on repart en surface
                Clock.NewDayFromTravel(gs);  // un voyage = un jour plein
                gs.VisitedStations.Add(s.Name);

                // Usure vaisseau à chaque voyage
                gs.ShipHp = Math.Max(1, gs.ShipHp - new Random().Next(2, 8));

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[steelblue1]── {s.Name} ──[/]  [grey]{dist:N0}M km parcourus — Jour {gs.Day}[/]");
                AnsiConsole.MarkupLine($"[grey]{s.Description}[/]");
                Events.ApplyTravelEffects(gs);

                // ── GUERRE SPATIALE : invasion possible en route ──────────────
                SpaceWar.MaybeTriggerBoarding(gs, s);

                // ── QUÊTES : vérifier livraisons/infos/vengeances à l'arrivée ──
                QuestSystem.CheckOnArrival(gs);

                Events.MaybeTriggerWithChoice(gs);
                Narrator.Pause();
                ChoiceMenu.Resolve(Situations.Arrival(gs), gs);
            });
    }).ToList();

    choices.Add(new Choice("[grey]← Annuler[/]", _ => { }));

    ChoiceMenu.Resolve(new Situation("Destination ?", choices, Spectre.Console.Color.SteelBlue1), state);
}

// ── RAVITAILLEMENT ──────────────────────────────────────────────────────────

static void MenuRefuel(GameState state)
{
    var price   = Market.GetPrice("Cellules de carburant");
    var missing = state.MaxFuel - state.Fuel;

    AnsiConsole.MarkupLine($"[grey]Prix :[/] {price}cr/unité   [grey]Manquant :[/] {missing}   [grey]Pour remplir :[/] {price * missing}cr");

    var choices = new List<Choice>();

    for (int i = 1; i <= missing; i++)
    {
        var amount = i;
        var total  = price * amount;
        if (state.Credits < total) break;
        choices.Add(new Choice($"+{amount} unité{(amount > 1 ? "s" : "")} — [yellow]{total}cr[/]",
            s =>
            {
                s.Credits -= total;
                s.Fuel    += amount;
                AnsiConsole.MarkupLine($"Ravitaillé [blue]+{amount}[/] unités pour [yellow]{total}cr[/]. Réservoir : {s.Fuel}/{s.MaxFuel}.");
            }));
    }

    if (!choices.Any())
        AnsiConsole.MarkupLine("[red]Pas assez de crédits pour acheter du carburant.[/]");
    else
    {
        choices.Add(new Choice("[grey]← Annuler[/]", _ => { }));
        ChoiceMenu.Resolve(new Situation("Combien de carburant ?", choices, Spectre.Console.Color.Blue), state);
    }
}

// ── WIKI ────────────────────────────────────────────────────────────────────

static void MenuWiki(GameState state)
{
    ChoiceMenu.Resolve(new Situation("Quel sujet ?", new List<Choice>
    {
        new("Objectifs",     _ => { Objectives.Show(state); Narrator.Pause(); }),
        new("Classes",       _ => Lore.Show("classes")),
        new("Stations",      _ => Lore.Show("stations")),
        new("Marchandises",  _ => Lore.Show("marchandises")),
        new("Événements",    _ => Lore.Show("événements")),
        new("[grey]← Retour[/]", _ => { }),
    }), state);
}
