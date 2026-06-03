using Spectre.Console;

namespace VoidTrader;

// Recette d'amélioration vaisseau : ressources requises + effet
record ShipRecipe(
    string Name,
    string Description,
    (string Item, int Qty)[] Ingredients,
    string Effect
);

static class ShipWorkshop
{
    private static readonly Random Rng = new();

    // ── RECETTES ────────────────────────────────────────────────────────────

    static readonly ShipRecipe RepairLight = new(
        "Réparation légère",
        "+25 PV vaisseau",
        [("Ferraille", 2)],
        "+25 PV vaisseau");

    static readonly ShipRecipe RepairHeavy = new(
        "Réparation lourde",
        "+60 PV vaisseau — nécessite des pièces de qualité",
        [("Pièces techniques", 1), ("Ferraille", 3)],
        "+60 PV vaisseau");

    static readonly ShipRecipe FullRestore = new(
        "Remise en état complète",
        "PV vaisseau au maximum",
        [("Pièces techniques", 2), ("Pièces détachées", 1)],
        "PV vaisseau au max");

    static readonly ShipRecipe HullReinforcement = new(
        "Renforcement de coque",
        "+25 PV max vaisseau permanent",
        [("Ferraille", 4), ("Pièces techniques", 1), ("Minerai brut", 1)],
        "+25 PV max vaisseau permanent");

    static readonly ShipRecipe EngineUpgrade = new(
        "Upgrade moteur",
        "+3 carburant max permanent",
        [("Pièces détachées", 2), ("Minerai exotique", 1)],
        "+3 MaxFuel permanent");

    static readonly ShipRecipe ExoticUpgrade = new(
        "Modification exotique",
        "+15 PV max vaisseau + +2 MaxFuel — matériaux rares",
        [("Matière noire", 1), ("Pièces techniques", 2)],
        "+15 PV max vaisseau, +2 MaxFuel permanent");

    static readonly ShipRecipe EmergencyPatch = new(
        "Patch d'urgence",
        "+10 PV vaisseau — vite fait avec ce qu'on a",
        [("Ferraille", 1)],
        "+10 PV vaisseau");

    static readonly ShipRecipe NavigationUpgrade = new(
        "Mise à jour navigation",
        "Débloque des routes supplémentaires temporairement (+5 portée)",
        [("Cartes stellaires", 1), ("Pièces techniques", 1)],
        "+5 portée navigation (permanent)");

    // ── MENU PRINCIPAL ──────────────────────────────────────────────────────

    public static void Open(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[steelblue1]ATELIER VAISSEAU[/]").RuleStyle("steelblue1"));
        AnsiConsole.MarkupLine($"  [grey]PV vaisseau :[/] [yellow]{state.ShipHp}/{state.ShipMaxHp}[/]   [grey]Carburant max :[/] [yellow]{state.MaxFuel}[/]");
        AnsiConsole.WriteLine();
        ShowInventorySummary(state);
        AnsiConsole.WriteLine();

        var recipes = new[]
        {
            EmergencyPatch, RepairLight, RepairHeavy, FullRestore,
            HullReinforcement, EngineUpgrade, ExoticUpgrade, NavigationUpgrade
        };

        var choices = recipes.Select(r =>
        {
            var canCraft = CanCraft(state, r);
            var ingList  = string.Join(", ", r.Ingredients.Select(i => $"{i.Item} x{i.Qty}"));
            var label    = canCraft
                ? $"[white]{r.Name}[/]  [grey dim]— {r.Effect}[/]  [grey]({ingList})[/]"
                : $"[grey]{r.Name}  — {r.Effect}[/]  [grey dim]({ingList})[/]  [red dim]✗ manque ressources[/]";
            return new Choice(label, s =>
            {
                if (canCraft) ApplyRecipe(s, r);
                else ShowMissing(s, r);
            });
        }).ToList();

        choices.Add(new Choice("[grey]← Fermer l'atelier[/]", _ => { }));
        ChoiceMenu.Resolve(new Situation("Que veux-tu faire ?", choices, Spectre.Console.Color.SteelBlue1), state);
    }

    // ── VÉRIFICATION ET APPLICATION ─────────────────────────────────────────

    static bool CanCraft(GameState state, ShipRecipe r)
        => r.Ingredients.All(i => state.Cargo.Get(i.Item) >= i.Qty);

    static void ApplyRecipe(GameState state, ShipRecipe r)
    {
        // Consommer les ressources
        foreach (var (item, qty) in r.Ingredients)
            state.Cargo.Remove(item, qty);

        // Appliquer l'effet
        switch (r.Name)
        {
            case "Patch d'urgence":
                state.ShipHp = Math.Min(state.ShipMaxHp, state.ShipHp + 10);
                Narrator.Say("Patch posé en quinze minutes. +10 PV vaisseau.", Spectre.Console.Color.Green);
                break;

            case "Réparation légère":
                state.ShipHp = Math.Min(state.ShipMaxHp, state.ShipHp + 25);
                Narrator.Say("Réparation propre. +25 PV vaisseau.", Spectre.Console.Color.Green);
                break;

            case "Réparation lourde":
                state.ShipHp = Math.Min(state.ShipMaxHp, state.ShipHp + 60);
                Narrator.Say("Gros travail de soudure. +60 PV vaisseau.", Spectre.Console.Color.Green);
                break;

            case "Remise en état complète":
                state.ShipHp = state.ShipMaxHp;
                Narrator.Say("Tout est remis à neuf. PV vaisseau au maximum.", Spectre.Console.Color.Green);
                break;

            case "Renforcement de coque":
                state.ShipMaxHp += 25;
                state.ShipHp     = Math.Min(state.ShipMaxHp, state.ShipHp + 25);
                Narrator.Say("La coque est plus épaisse maintenant. +25 PV max vaisseau permanent.", Spectre.Console.Color.Gold1);
                break;

            case "Upgrade moteur":
                state.Fuel = Math.Min(state.MaxFuel + 3, state.Fuel + 3);
                // MaxFuel vient de Class donc on l'augmente via un champ dans GameState
                state.BonusMaxFuel += 3;
                Narrator.Say("Le moteur peut maintenant embarquer plus de carburant. +3 MaxFuel permanent.", Spectre.Console.Color.Gold1);
                break;

            case "Modification exotique":
                state.ShipMaxHp  += 15;
                state.BonusMaxFuel += 2;
                state.Fuel        = Math.Min(state.MaxFuel + 2, state.Fuel + 2);
                state.ShipHp      = Math.Min(state.ShipMaxHp, state.ShipHp + 15);
                Narrator.Say("Modification mystérieuse. +15 PV max vaisseau, +2 MaxFuel. Permanent.", Spectre.Console.Color.Gold1);
                break;

            case "Mise à jour navigation":
                state.BonusNavRange += 5;
                Narrator.Say("Navigation recalibrée. +5 portée permanente — tu peux atteindre des stations plus lointaines.", Spectre.Console.Color.Cyan1);
                break;
        }

        AnsiConsole.MarkupLine($"  [grey]Ressources consommées : {string.Join(", ", r.Ingredients.Select(i => $"{i.Item} x{i.Qty}"))}[/]");
        Narrator.Pause();
    }

    static void ShowMissing(GameState state, ShipRecipe r)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red]Ressources manquantes pour [white]{r.Name}[/] :[/]");
        foreach (var (item, qty) in r.Ingredients)
        {
            var has  = state.Cargo.Get(item);
            var icon = has >= qty ? "[green]✔[/]" : "[red]✗[/]";
            AnsiConsole.MarkupLine($"  {icon} {item} : {has}/{qty}");
        }
        Narrator.Pause();
    }

    static void ShowInventorySummary(GameState state)
    {
        var craftItems = new[] { "Ferraille", "Pièces techniques", "Pièces détachées",
                                 "Minerai brut", "Minerai exotique", "Matière noire", "Cartes stellaires" };
        AnsiConsole.MarkupLine("[grey dim]Ressources disponibles :[/]");
        foreach (var item in craftItems)
        {
            var qty = state.Cargo.Get(item);
            if (qty > 0)
                AnsiConsole.MarkupLine($"  [white]{item}[/] x{qty}");
        }
        var hasNone = craftItems.All(i => state.Cargo.Get(i) == 0);
        if (hasNone) AnsiConsole.MarkupLine("  [grey dim]Aucune ressource de maintenance en stock.[/]");
    }
}
