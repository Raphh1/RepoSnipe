using Spectre.Console;

namespace VoidTrader;

static class Menus
{
    public static void OpenMarket(GameState state)
    {
        var station = Universe.Get(state.CurrentStation);

        if (state.Class.PeacefulBan && station.IsPeaceful)
        {
            Narrator.Say("Les commerçants te reconnaissent et ferment leurs stands à ton approche.");
            Narrator.Pause();
            return;
        }

        while (true)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[steelblue1]── Marché de {state.CurrentStation} ──[/]  [yellow]{state.Credits}cr[/]  [grey]{state.ReputationLabel}[/]");
            var note = Market.ReputationMarketNote(state.Reputation);
            if (!string.IsNullOrEmpty(note)) AnsiConsole.MarkupLine($"  {note}");
            var econNote = Market.EconomicNote(state.CurrentStation);
            if (econNote != null) AnsiConsole.MarkupLine($"  {econNote}");
            AnsiConsole.WriteLine();

            var choices = new List<Choice>();

            foreach (var g in station.Goods)
            {
                var price  = ApplyBuyPrice(state, Market.GetPrice(g, state.CurrentStation, state.Day), g);
                var banned = state.Class.CannotBuyWeapons && g == "Armes";
                var consumInfo = Consumables.IsConsumable(g)
                    ? $"  [green dim]{Consumables.Effects[g].Description}[/]" : "";
                var label  = banned
                    ? $"[grey]Stand {g} — {price}cr (interdit)[/]"
                    : state.Credits >= price
                        ? $"Acheter [white]{g}[/] — [yellow]{price}cr[/]{consumInfo}"
                        : $"[grey]Acheter {g} — {price}cr (fonds insuffisants)[/]{consumInfo}";

                choices.Add(new Choice(label, s =>
                {
                    if (banned)            { Narrator.Say("Ton éducation te l'interdit."); return; }
                    if (s.Credits < price) { Narrator.Say("Pas assez de crédits."); return; }
                    s.Credits -= price;

                    // Les "Armes" donnent une arme spécifique aléatoire selon la station
                    if (g == "Armes")
                    {
                        var weapon = WeaponPool.RollForStation(s.CurrentStation);
                        s.Weapons.Add(weapon);
                        Narrator.Say($"Tu achètes une arme pour {price}cr.");
                        Combat.ShowWeaponDrop(weapon);
                    }
                    else if (g == "Armures")
                    {
                        var armor = ArmorPool.RollForStation(s.CurrentStation);
                        s.Armors.Add(armor);
                        Narrator.Say($"Tu achètes une armure pour {price}cr.");
                        Combat.ShowArmorDrop(armor);
                    }
                    else
                    {
                        s.Cargo.Add(g, 1);
                        Narrator.Say($"Tu achètes {g} pour {price}cr. Le marchand empoche sans un mot.");
                    }
                }));
            }

            if (state.Cargo.All.Any())
            {
                choices.Add(new Choice("── [grey]Vendre ma cargaison[/]", s =>
                {
                    Narrator.Say("Tu étales ta cargaison sur le comptoir...");
                    Sell(s);
                }));
            }

            choices.Add(new Choice("← Quitter le marché", _ => { }));

            var choice = ChoiceMenu.Present(new Situation("Stands disponibles :", choices), state);
            if (choice is null || choice.Label.Contains("Quitter")) break;
            choice.Effect?.Invoke(state);
            Narrator.Pause();
        }
    }

    public static void Sell(GameState state)
    {
        if (!state.Cargo.All.Any()) { AnsiConsole.MarkupLine("[grey]Cargaison vide.[/]"); return; }

        var sellMod = Market.SellModifier(state.Reputation) * Market.FactionSellBonus(state.Faction);

        var choices = state.Cargo.All.Select(kv =>
        {
            var good  = kv.Key;
            var qty   = kv.Value;
            var price = (int)(Market.GetPrice(good, state.CurrentStation, state.Day) * sellMod);
            if (state.Class.MedicBonus && good == "Médicaments")
                price = (int)(price * 1.5);

            return new Choice($"[white]{good}[/] (x{qty}) — [yellow]{price}cr[/] l'unité",
                s =>
                {
                    s.Cargo.Remove(good, 1);
                    s.Credits += price;
                    Narrator.Say($"Vendu {good} pour {price}cr.");
                });
        }).ToList();

        choices.Add(new Choice("← Retour", _ => { }));
        ChoiceMenu.Resolve(new Situation("Que veux-tu vendre ?", choices), state);
    }

    public static void ManageWeapons(GameState state)
    {
        if (!state.Weapons.Any()) { AnsiConsole.MarkupLine("[grey]Aucune arme en possession.[/]"); Narrator.Pause(); return; }

        var choices = state.Weapons.Select(w =>
        {
            var equipped  = state.EquippedWeapon == w;
            var selfWarn  = w.SelfDmgChance > 0 ? $" [red]⚠{w.SelfDmgChance}% self-dmg[/]" : "";
            var effectStr = w.Effect != WeaponEffect.None ? $" — [cyan1]{w.EffectDesc}[/]" : "";
            var sellVal   = w.Tier * 300 + w.DamageMax * 2;
            var label     = equipped
                ? $"[green]✔ {w.Name}[/]  [grey]T{w.Tier} — {w.DamageMin}-{w.DamageMax} dmg — Crit {w.CritChance}%[/]{effectStr}{selfWarn}  [grey dim](équipée — revente ~{sellVal}cr)[/]"
                : $"{w.Name}  [grey]T{w.Tier} — {w.DamageMin}-{w.DamageMax} dmg — Crit {w.CritChance}%[/]{effectStr}{selfWarn}  [grey dim](revente ~{sellVal}cr)[/]";

            return new Choice(label, s =>
            {
                ChoiceMenu.Resolve(new Situation($"Que fais-tu avec {w.Name} ?",
                [
                    new("Équiper / Ranger", gs =>
                    {
                        if (gs.Class.CannotBuyWeapons)
                        {
                            Narrator.Say("Ton éducation pacifiste t'interdit de porter des armes. Tu peux la vendre en revanche.", Color.Grey);
                            Narrator.Pause();
                            return;
                        }
                        if (gs.EquippedWeapon == w) { gs.EquippedWeapon = null; Narrator.Say($"Tu ranges {w.Name}."); }
                        else { gs.EquippedWeapon = w; Narrator.Say($"Tu équipes {w.Name}.", Color.Green); }
                        Narrator.Pause();
                    }),
                    new($"Vendre (~{sellVal}cr)", gs =>
                    {
                        if (gs.EquippedWeapon == w) gs.EquippedWeapon = null;
                        gs.Weapons.Remove(w);
                        gs.Credits += sellVal;
                        Narrator.Say($"Tu vends {w.Name} pour {sellVal}cr.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("← Annuler", _ => { }),
                ]), s);
            });
        }).ToList();

        choices.Add(new Choice("[grey]← Retour[/]", _ => { }));
        ChoiceMenu.Resolve(new Situation("Tes armes :", choices, Color.OrangeRed1), state);
    }

    public static void ManageArmors(GameState state)
    {
        if (!state.Armors.Any()) { AnsiConsole.MarkupLine("[grey]Aucune armure en possession.[/]"); Narrator.Pause(); return; }

        var choices = state.Armors.Select(a =>
        {
            var equipped  = state.EquippedArmor == a;
            var effectStr = a.Effect != ArmorEffect.None ? $" — [cyan1]{a.Effect} ({a.EffectValue})[/]" : "";
            var label     = equipped
                ? $"[green]✔ {a.Name}[/]  [grey]T{a.Tier} — -{a.Defense}% dmg — +{a.HpBonus} PV[/]{effectStr}  [grey dim](équipée — revente ~{a.SellValue}cr)[/]"
                : $"{a.Name}  [grey]T{a.Tier} — -{a.Defense}% dmg — +{a.HpBonus} PV[/]{effectStr}  [grey dim](revente ~{a.SellValue}cr)[/]";

            return new Choice(label, s =>
            {
                ChoiceMenu.Resolve(new Situation($"Que fais-tu avec {a.Name} ?",
                [
                    new("Équiper / Retirer", gs =>
                    {
                        if (gs.EquippedArmor == a)
                        {
                            gs.PlayerMaxHp   -= a.HpBonus;
                            gs.PlayerHp       = Math.Min(gs.PlayerHp, gs.PlayerMaxHp);
                            gs.EquippedArmor  = null;
                            Narrator.Say($"Tu retires {a.Name}. -PV max.", Color.Grey);
                        }
                        else
                        {
                            if (gs.EquippedArmor != null)
                            {
                                gs.PlayerMaxHp  -= gs.EquippedArmor.HpBonus;
                                gs.PlayerHp      = Math.Min(gs.PlayerHp, gs.PlayerMaxHp);
                            }
                            gs.EquippedArmor  = a;
                            gs.PlayerMaxHp   += a.HpBonus;
                            Narrator.Say($"Tu équipes {a.Name}. +{a.HpBonus} PV max.", Color.Green);
                        }
                        Narrator.Pause();
                    }),
                    new($"Vendre (~{a.SellValue}cr)", gs =>
                    {
                        if (gs.EquippedArmor == a)
                        {
                            gs.PlayerMaxHp  -= a.HpBonus;
                            gs.PlayerHp      = Math.Min(gs.PlayerHp, gs.PlayerMaxHp);
                            gs.EquippedArmor = null;
                        }
                        gs.Armors.Remove(a);
                        gs.Credits += a.SellValue;
                        Narrator.Say($"Vendu {a.Name} pour {a.SellValue}cr.", Color.Yellow);
                        Narrator.Pause();
                    }),
                    new("← Annuler", _ => { }),
                ]), s);
            });
        }).ToList();

        choices.Add(new Choice("[grey]← Retour[/]", _ => { }));
        ChoiceMenu.Resolve(new Situation("Tes armures :", choices, Color.SteelBlue1), state);
    }

    public static void HealOutsideCombat(GameState state)
    {
        if (state.Cargo.Get("Médicaments") == 0) { Narrator.Say("T'as pas de médicaments."); Narrator.Pause(); return; }
        Consumables.Use(state, "Médicaments");
    }

    public static void DoRefuel(GameState state)
    {
        var price   = Market.GetPrice("Cellules de carburant", state.CurrentStation, state.Day);
        var missing = state.MaxFuel - state.Fuel;

        AnsiConsole.MarkupLine($"[grey]Prix :[/] {price}cr/unité   [grey]Manquant :[/] {missing}   [grey]Pour remplir :[/] {price * missing}cr");

        var choices = new List<Choice>();
        for (int i = 1; i <= missing; i++)
        {
            var amount = i;
            var total  = price * amount;
            if (state.Credits < total) break;
            choices.Add(new Choice($"+{amount} unité{(amount > 1 ? "s" : "")} — [yellow]{total}cr[/]",
                s => { s.Credits -= total; s.Fuel += amount; Narrator.Say($"Ravitaillé +{amount} unités pour {total}cr. Réservoir : {s.Fuel}/{s.MaxFuel}."); Narrator.Pause(); }));
        }

        if (!choices.Any()) { AnsiConsole.MarkupLine("[red]Pas assez de crédits pour acheter du carburant.[/]"); Narrator.Pause(); return; }
        choices.Add(new Choice("[grey]← Annuler[/]", _ => { }));
        ChoiceMenu.Resolve(new Situation("Combien de carburant ?", choices, Spectre.Console.Color.Blue), state);
    }

    static int ApplyBuyPrice(GameState state, int basePrice, string item)
    {
        var price = basePrice * Market.BuyModifier(state.Reputation);
        if (state.Class.BuyDiscountPercent > 0)
            price *= (1 - state.Class.BuyDiscountPercent / 100.0f);
        if (state.Faction == FactionId.Emporium)
            price *= 0.85f;
        // Faucons Noirs : réduction sur l'armement
        if (state.Faction == FactionId.Faucons && (item == "Armes" || item == "Explosifs"))
            price *= 0.85f;
        return Math.Max(1, (int)price);
    }
}
