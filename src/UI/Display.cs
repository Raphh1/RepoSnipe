using Spectre.Console;

namespace VoidTrader;

static class Display
{
    public static void ShowTitle()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("VOID TRADER").Color(Color.SteelBlue1));
        AnsiConsole.MarkupLine("[grey]Un jeu de survie et de commerce spatial[/]\n");
    }

    public static void ShowClass(PlayerClass cls)
    {
        var (color, tier) = cls.Tier switch
        {
            ClassTier.Bad      => ("red",    "★☆☆  Difficulté : Élevée"),
            ClassTier.Balanced => ("yellow", "★★☆  Difficulté : Normale"),
            ClassTier.Good     => ("green",  "★★★  Difficulté : Facile"),
            _                  => ("grey",   "?")
        };

        var avantages = BuildClassPerks(cls);

        var panel = new Panel(
            $"[{color} bold]{cls.Name}[/]  [grey]{tier}[/]\n" +
            $"[grey]{cls.Description}[/]\n\n" +
            $"[grey]Crédits de départ :[/] [yellow]{cls.StartingCredits}cr[/]   " +
            $"[grey]Carburant max :[/] [blue]{cls.MaxFuel}[/]   " +
            $"[grey]Station départ :[/] [steelblue1]{cls.StartingStation}[/]\n\n" +
            avantages
        )
        {
            Header = new PanelHeader(" Votre classe tirée au sort "),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    static string BuildClassPerks(PlayerClass cls)
    {
        var perks   = new List<string>();
        var maluses = new List<string>();

        if (cls.BuyDiscountPercent > 0)   perks.Add($"[green]+[/] Achats -[yellow]{cls.BuyDiscountPercent}%[/]");
        if (cls.MedicBonus)               perks.Add("[green]+[/] Médicaments vendus [yellow]+50%[/]");
        if (cls.SeesPrices)               perks.Add("[green]+[/] Voit les prix des stations avant de voyager");
        if (cls.AutoKillsPirates)         perks.Add("[green]+[/] Pirates en voyage éliminés automatiquement");
        if (cls.PeriodicIncome > 0)       perks.Add($"[green]+[/] Virement familial [yellow]+{cls.PeriodicIncome}cr[/] tous les 5 jours");
        if (cls.NeutralEventsBoost)       perks.Add("[green]+[/] Événements positifs plus fréquents");

        if (cls.DailyDebt > 0)            maluses.Add($"[red]-[/] Perd [yellow]{cls.DailyDebt}cr[/] par jour (dette)");
        if (cls.TravelCreditCost > 0)     maluses.Add($"[red]-[/] Perd [yellow]{cls.TravelCreditCost}cr[/] à chaque voyage (addiction)");
        if (cls.CargoDegrades)            maluses.Add("[red]-[/] Cargaison peut disparaître pendant les voyages");
        if (cls.CursedEvents)             maluses.Add("[red]-[/] Événements positifs annulés 50% du temps");
        if (cls.PiratesDoubled)           maluses.Add("[red]-[/] Pirates deux fois plus fréquents");
        if (cls.PeacefulBan)              maluses.Add("[red]-[/] Les stations paisibles refusent de vendre");
        if (cls.CannotBuyWeapons)         maluses.Add("[red]-[/] Impossible d'acheter des armes");

        var result = "";
        if (perks.Any())   result += "[grey]Avantages :[/]\n" + string.Join("\n", perks.Select(p => $"  {p}")) + "\n";
        if (maluses.Any()) result += "\n[grey]Malus :[/]\n"   + string.Join("\n", maluses.Select(m => $"  {m}"));

        return result.TrimEnd();
    }

    public static void ShowStatus(GameState state)
    {
        var classColor = state.Class.Tier switch
        {
            ClassTier.Bad      => "red",
            ClassTier.Balanced => "yellow",
            ClassTier.Good     => "green",
            _                  => "grey"
        };

        var repColor = state.Reputation switch
        {
            >= 500  => "green",
            >= 0    => "grey",
            >= -500 => "orange1",
            _       => "red"
        };

        var shipHpColor  = state.ShipHp   > state.ShipMaxHp   * 0.5 ? "green" : state.ShipHp   > state.ShipMaxHp   * 0.25 ? "yellow" : "red";
        var playerHpColor = state.PlayerHp > state.PlayerMaxHp * 0.5 ? "green" : state.PlayerHp > state.PlayerMaxHp * 0.25 ? "yellow" : "red";

        var weaponStr  = state.EquippedWeapon != null ? $"   [orange1]⚔ {state.EquippedWeapon.Name}[/]" : "   [grey]⚔ mains nues[/]";
        string armorStr;
        if (state.EquippedArmor != null)
        {
            var a = state.EquippedArmor;
            var effectPart = a.Effect != ArmorEffect.None ? $" [cyan1]{a.Effect}+{a.EffectValue}[/]" : "";
            armorStr = $"   [steelblue1]🛡 {a.Name}[/] [grey](-{a.Defense}% dmg  +{a.HpBonus}PV[/]{effectPart}[grey])[/]";
        }
        else armorStr = "";
        var factionStr = state.Faction != FactionId.None ? $"   [magenta1]{Factions.Info[state.Faction].Name}[/]{(state.IsDoubleAgent ? " [grey](agent double)[/]" : "")}" : "";
        var objStr     = $"   [grey dim]Objectifs : {state.CompletedObjectives.Count}/{Objectives.All.Count}[/]";
        var addictStr  = state.AddictionLevel > 0 ? $"   {state.AddictionLabel} [grey dim](-{state.AddictionDailyCost}cr/jour)[/]" : "";
        var stalkerStr = state.StalkerLevel > 0 ? $"   [red]👁 {state.StalkerName} (niveau {state.StalkerLevel})[/]" : "";
        var questStr   = state.ActiveQuests.Any() ? $"   [cyan1]📋 {state.ActiveQuests.Count} quête{(state.ActiveQuests.Count > 1 ? "s" : "")} active{(state.ActiveQuests.Count > 1 ? "s" : "")}[/]" : "";

        var panel = new Panel(
            $"[yellow]Crédits :[/] {state.Credits}cr   " +
            $"[blue]Carburant :[/] {state.Fuel}/{state.MaxFuel}   " +
            $"[grey]Jour :[/] {state.Day} [grey dim]({state.ActionsToday}/{Clock.ActionsPerDay})[/]   " +
            $"[{classColor}]{state.Class.Name}[/]\n" +
            $"[{shipHpColor}]Vaisseau :[/] {state.ShipHp}/{state.ShipMaxHp}PV   " +
            $"[{playerHpColor}]Joueur :[/] {state.PlayerHp}/{state.PlayerMaxHp}PV   " +
            $"[{repColor}]Réputation :[/] {state.Reputation} ({state.ReputationLabel})" +
            weaponStr + armorStr + factionStr + objStr + addictStr + stalkerStr + questStr
        )
        {
            Header = new PanelHeader($" [steelblue1]{state.CurrentStation}[/]  {Universe.DangerBadge(Universe.Get(state.CurrentStation))} "),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }

    // Items utilisables directement par le joueur
    private static readonly HashSet<string> Usable = ["Médicaments", "Plantes médicinales", "Explosifs", "Objets expérimentaux"];
    // Items avec usage spécial (échangeable contre un service)
    private static readonly HashSet<string> Special = ["Or", "Artefacts", "Cartes stellaires", "Marchandises illégales"];

    public static void ShowCargo(GameState state)
    {
        if (!state.Cargo.All.Any() && !state.Weapons.Any())
        {
            AnsiConsole.MarkupLine("[grey]Cargaison : vide[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("Marchandise").AddColumn("Qté").AddColumn("Usage");

        foreach (var (item, qty) in state.Cargo.All)
        {
            var usage = Usable.Contains(item)  ? "[cyan1]Utilisable[/]" :
                        Special.Contains(item) ? "[gold1]Échangeable[/]" :
                        "[grey]Revente[/]";
            table.AddRow(item, qty.ToString(), usage);
        }

        if (state.Weapons.Any())
        {
            table.AddEmptyRow();
            foreach (var w in state.Weapons)
            {
                var equipped = state.EquippedWeapon == w ? " [green](équipée)[/]" : "";
                var selfWarn = w.SelfDmgChance > 0 ? $" [red]⚠[/]" : "";
                table.AddRow(
                    $"[orange1]{w.Name}[/]{equipped}{selfWarn}",
                    "1",
                    $"[orange1]Arme T{w.Tier}[/]  [grey]{w.DamageMin}–{w.DamageMax} dmg  crit {w.CritChance}%[/]"
                );
            }
        }

        AnsiConsole.Write(table);
    }

    public static void ShowEvent(string message, Color color)
    {
        AnsiConsole.MarkupLine($"\n[{color}]>> {message}[/]");
    }

    public static void ShowHelp()
    {
        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("[grey]Commande[/]")
            .AddColumn("[grey]Description[/]");

        table.AddRow("travel [[station]]", "Voyager vers une station (sans arg = liste des destinations)");
        table.AddRow("refuel [[n|full]]", "Acheter du carburant ici (sans arg = voir le prix)");
        table.AddRow("buy [[article]]",   "Acheter des marchandises (sans arg = voir le marché)");
        table.AddRow("sell [[article]]",  "Vendre de la cargaison (sans arg = voir ta cargaison)");
        table.AddRow("status",            "Afficher ta cargaison");
        table.AddRow("wiki [[sujet]]",    "Bible du jeu — sujets : classes, stations, marchandises, événements");
        table.AddRow("help",              "Afficher cette liste");
        table.AddRow("quit",              "Quitter le jeu");

        AnsiConsole.Write(table);
    }
}
