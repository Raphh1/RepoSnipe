using Spectre.Console;

namespace VoidTrader;

static class Lore
{
    public static void Show(string topic)
    {
        switch (topic.ToLower().Trim())
        {
            case "":
            case "help":
                ShowIndex();
                break;
            case "classes":
                ShowClasses();
                break;
            case "stations":
                ShowStations();
                break;
            case "marchandises":
                ShowGoods();
                break;
            case "événements":
            case "evenements":
                ShowEvents();
                break;
            default:
                AnsiConsole.MarkupLine("[red]Sujet inconnu. Essaie : wiki classes, wiki stations, wiki marchandises, wiki événements[/]");
                break;
        }
    }

    static void ShowIndex()
    {
        var table = new Table().Border(TableBorder.Rounded).Title("[steelblue1]VOID TRADER — WIKI[/]")
            .AddColumn("[grey]Commande[/]").AddColumn("[grey]Description[/]");

        table.AddRow("wiki classes",       "Les 14 classes, leurs bonus et malus");
        table.AddRow("wiki stations",      "Les stations de l'univers et ce qu'elles vendent");
        table.AddRow("wiki marchandises",  "Les marchandises échangeables et leurs prix de base");
        table.AddRow("wiki événements",    "Les événements aléatoires pendant les voyages");

        AnsiConsole.Write(table);
    }

    static void ShowClasses()
    {
        AnsiConsole.MarkupLine("\n[steelblue1]CLASSES[/] — Attribuée aléatoirement au début de chaque partie.\n");

        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("Classe")
            .AddColumn("Tier")
            .AddColumn("Départ")
            .AddColumn("Carburant max")
            .AddColumn("Effet");

        foreach (var cls in PlayerClass.All)
        {
            var (tierColor, tierLabel) = cls.Tier switch
            {
                ClassTier.Bad      => ("red",    "Mauvaise"),
                ClassTier.Balanced => ("yellow", "Équilibrée"),
                ClassTier.Good     => ("green",  "Bonne"),
                _                  => ("grey",   "?")
            };

            table.AddRow(
                $"[bold]{cls.Name}[/]",
                $"[{tierColor}]{tierLabel}[/]",
                $"{cls.StartingCredits}cr",
                $"{cls.MaxFuel}",
                $"[grey]{BuildEffectSummary(cls)}[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    static string BuildEffectSummary(PlayerClass cls)
    {
        var parts = new List<string>();
        if (cls.CargoDegrades)        parts.Add("30% de chance de perdre un article de cargaison par voyage");
        if (cls.DailyDebt > 0)        parts.Add($"-{cls.DailyDebt}cr/jour");
        if (cls.TravelCreditCost > 0) parts.Add($"-{cls.TravelCreditCost}cr/voyage");
        if (cls.CursedEvents)         parts.Add("les événements positifs ont 50% de chance d'être annulés");
        if (cls.BuyDiscountPercent>0) parts.Add($"-{cls.BuyDiscountPercent}% sur les achats");
        if (cls.PiratesDoubled)       parts.Add("pirates 2x plus fréquents");
        if (cls.CannotBuyWeapons)     parts.Add("ne peut pas acheter d'Armes");
        if (cls.PeriodicIncome > 0)   parts.Add($"+{cls.PeriodicIncome}cr tous les 5 jours");
        if (cls.SeesPrices)           parts.Add("voit les prix à destination avant de voyager");
        if (cls.AutoKillsPirates)     parts.Add("élimine automatiquement les pirates");
        if (cls.PeacefulBan)          parts.Add("interdit dans les stations pacifiques");
        if (cls.MedicBonus)           parts.Add("les Médicaments se vendent +50%");
        if (cls.NeutralEventsBoost)   parts.Add("événements neutres plus fréquents");
        return parts.Count > 0 ? string.Join(" | ", parts) : "Aucun effet spécial";
    }

    static void ShowStations()
    {
        AnsiConsole.MarkupLine("\n[steelblue1]STATIONS[/]\n");

        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("Station")
            .AddColumn("Type")
            .AddColumn("Vend");

        foreach (var s in Universe.Stations)
        {
            var type = s.IsPeaceful ? "[green]Pacifique[/]" : "[red]Sans loi[/]";
            table.AddRow($"[bold]{s.Name}[/]", type, string.Join(", ", s.Goods));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]Les stations pacifiques refusent de traiter avec le Seigneur de guerre.[/]");
    }

    static void ShowGoods()
    {
        AnsiConsole.MarkupLine("\n[steelblue1]MARCHANDISES[/] — Les prix fluctuent de ±30% à chaque station.\n");

        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("Article").AddColumn("Prix de base").AddColumn("Notes");

        table.AddRow("Cellules de carburant", "80cr",   "Indispensable pour voyager");
        table.AddRow("Eau",                   "30cr",   "Bon marché, marges faibles");
        table.AddRow("Rations",               "50cr",   "Demande stable");
        table.AddRow("Pièces techniques",     "200cr",  "Bonnes marges");
        table.AddRow("Armes",                 "350cr",  "Grande valeur, interdit aux Héritiers");
        table.AddRow("Minerai exotique",      "500cr",  "Rare, gros gains");
        table.AddRow("Matière noire",         "900cr",  "Marchandise la plus précieuse");
        table.AddRow("Ferraille",             "40cr",   "Remplissage bon marché");
        table.AddRow("Explosifs",             "300cr",  "Risqué mais rentable");
        table.AddRow("Médicaments",           "150cr",  "Vendu +50% par le Médecin");

        AnsiConsole.Write(table);
    }

    static void ShowEvents()
    {
        AnsiConsole.MarkupLine("\n[steelblue1]ÉVÉNEMENTS[/] — 70% de chance de se déclencher à chaque voyage.\n");

        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("Événement").AddColumn("Effet").AddColumn("Notes");

        table.AddRow("[green]Capsule de cargo[/]",        "+200cr",          "");
        table.AddRow("[green]Pourboire marchand[/]",      "+500cr",          "");
        table.AddRow("[green]Épave récupérée[/]",         "+2 carburant",    "");
        table.AddRow("[green]Pari gagné[/]",              "+300cr",          "");
        table.AddRow("[green]Artefact rare[/]",           "+1000cr",         "Rare");
        table.AddRow("[green]Cache de carburant[/]",      "+3 carburant",    "Rare");
        table.AddRow("[red]Péage spatial[/]",             "-150cr",          "");
        table.AddRow("[red]Micrométéorite[/]",            "-1 carburant",    "");
        table.AddRow("[red]Pirates[/]",                   "-300cr",          "x2 pour le Contrebandier, éliminés par le Seigneur de guerre");
        table.AddRow("[red]Panne moteur[/]",              "-500cr",          "Rare");
        table.AddRow("[grey]Voyage tranquille[/]",        "Rien",            "Plus fréquent pour l'Explorateur");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]Maudit : les événements positifs ont 50% de chance d'être annulés.[/]");
    }
}
