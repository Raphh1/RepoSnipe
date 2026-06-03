using Spectre.Console;

namespace VoidTrader;

record ConsumableEffect(
    int    HpRestore,
    int    StaminaRestore,
    int    HpMaxBonus,       // bonus permanent PV max
    string Description,
    bool   HasRisk = false   // effet aléatoire ou risque d'effet négatif
);

static class Consumables
{
    // ── TABLE DES EFFETS ────────────────────────────────────────────────────

    public static readonly Dictionary<string, ConsumableEffect> Effects = new()
    {
        ["Rations"]              = new(15,  20,  0, "+15 PV, +20 Stamina"),
        ["Vivres"]               = new(30,  35,  0, "+30 PV, +35 Stamina"),
        ["Eau"]                  = new(10,  25,  0, "+10 PV, +25 Stamina"),
        ["Médicaments"]          = new(45,   0,  0, "+45 PV"),
        ["Plantes médicinales"]  = new(20,  15,  0, "+20 PV, +15 Stamina"),
        ["Objets expérimentaux"] = new( 0,   0,  0, "Effet aléatoire — risque", HasRisk: true),
        ["Marchandises illégales"] = new(0,  50, 0, "+50 Stamina — risque", HasRisk: true),
    };

    public static bool IsConsumable(string item) => Effects.ContainsKey(item);

    // ── UTILISER UN CONSOMMABLE ─────────────────────────────────────────────

    public static void Use(GameState state, string item)
    {
        if (!Effects.TryGetValue(item, out var fx)) return;
        if (!state.Cargo.Remove(item, 1))
        {
            Narrator.Say($"Tu n'as plus de {item}.", Color.Red);
            Narrator.Pause();
            return;
        }

        if (fx.HasRisk)
        {
            UseRisky(state, item, fx);
            return;
        }

        var hpGained  = Math.Min(state.PlayerMaxHp - state.PlayerHp, fx.HpRestore);
        var stGained  = Math.Min(state.MaxStamina  - state.Stamina,  fx.StaminaRestore);

        state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + fx.HpRestore);
        state.Stamina  = Math.Min(state.MaxStamina,  state.Stamina  + fx.StaminaRestore);
        if (fx.HpMaxBonus > 0) state.PlayerMaxHp += fx.HpMaxBonus;

        var parts = new List<string>();
        if (hpGained  > 0) parts.Add($"[green]+{hpGained} PV[/]");
        if (stGained  > 0) parts.Add($"[cyan1]+{stGained} Stamina[/]");
        if (fx.HpMaxBonus > 0) parts.Add($"[gold1]+{fx.HpMaxBonus} PV max permanent[/]");
        if (!parts.Any()) parts.Add("[grey]Déjà au max — aucun effet[/]");

        Narrator.Say($"Tu consommes {item}. {string.Join(", ", parts)}.", Color.Green);
        Narrator.Pause();
    }

    static readonly Random Rng = new();

    static void UseRisky(GameState state, string item, ConsumableEffect fx)
    {
        if (item == "Objets expérimentaux")
        {
            switch (Rng.Next(6))
            {
                case 0:
                    var hpBonus = Rng.Next(30, 80);
                    state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + hpBonus);
                    Narrator.Say($"Effet positif massif. +{hpBonus} PV. Tu te sens comme neuf.", Color.Gold1);
                    break;
                case 1:
                    state.PlayerMaxHp += 10;
                    state.PlayerHp     = Math.Min(state.PlayerMaxHp, state.PlayerHp + 10);
                    Narrator.Say("Quelque chose change en profondeur. +10 PV max permanent.", Color.Gold1);
                    break;
                case 2:
                    state.Stamina = state.MaxStamina;
                    Narrator.Say("Pic d'adrénaline. Stamina au max.", Color.Cyan1);
                    break;
                case 3:
                    var dmg = Rng.Next(20, 50);
                    state.PlayerHp = Math.Max(1, state.PlayerHp - dmg);
                    Narrator.Say($"Réaction violente. -{dmg} PV. T'aurais dû lire l'étiquette.", Color.Red);
                    break;
                case 4:
                    state.AddictionLevel++;
                    state.Stamina = state.MaxStamina;
                    state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + 25);
                    Narrator.Say("Euphorie totale. +25 PV, Stamina max. Mais quelque chose accroche. +1 niveau dépendance.", Color.OrangeRed1);
                    break;
                default:
                    Narrator.Say("Rien. Absolument rien. Tu as consommé quelque chose d'inerte.", Color.Grey);
                    break;
            }
        }
        else if (item == "Marchandises illégales")
        {
            state.AddictionLevel++;
            switch (Rng.Next(4))
            {
                case 0:
                    state.Stamina = state.MaxStamina;
                    Narrator.Say("Stamina au max instantanément. +1 dépendance.", Color.OrangeRed1);
                    break;
                case 1:
                    state.Stamina = state.MaxStamina;
                    state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + 20);
                    Narrator.Say("+20 PV, Stamina max. +1 dépendance. Ça dure pas longtemps.", Color.OrangeRed1);
                    break;
                case 2:
                    var lost = Rng.Next(100, 400); state.Credits = Math.Max(0, state.Credits - lost);
                    state.Stamina = state.MaxStamina;
                    Narrator.Say($"T'as perdu {lost}cr pendant que t'étais pas là. Stamina max. +1 dépendance.", Color.Red);
                    break;
                default:
                    var dmg2 = Rng.Next(15, 35); state.PlayerHp = Math.Max(1, state.PlayerHp - dmg2);
                    Narrator.Say($"Mauvaise dose. -{dmg2} PV. +1 dépendance.", Color.Red);
                    break;
            }
        }
        Narrator.Pause();
    }

    // ── MENU INVENTAIRE CONSOMMABLES ────────────────────────────────────────

    public static void OpenInventoryMenu(GameState state)
    {
        var consumables = state.Cargo.All
            .Where(kv => IsConsumable(kv.Key))
            .ToList();

        if (!consumables.Any())
        {
            Narrator.Say("Rien de consommable dans ta cargaison.", Color.Grey);
            Narrator.Pause();
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green bold]── INVENTAIRE CONSOMMABLES ──[/]  [grey]PV : {state.PlayerHp}/{state.PlayerMaxHp}   Stamina : {state.Stamina}/{state.MaxStamina}[/]");
        AnsiConsole.WriteLine();

        var choices = consumables.Select(kv =>
        {
            var name = kv.Key;
            var qty  = kv.Value;
            var fx   = Effects[name];
            var label = $"[white]{name}[/] (x{qty})  [grey dim]{fx.Description}[/]";
            return new Choice(label, s => Use(s, name));
        }).ToList();

        choices.Add(new Choice("[grey]← Fermer[/]", _ => { }));
        ChoiceMenu.Resolve(new Situation("Quoi consommer ?", choices, Spectre.Console.Color.Green), state);
    }
}
