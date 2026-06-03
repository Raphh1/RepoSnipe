using Spectre.Console;

namespace VoidTrader;

/// <summary>
/// Exécute des événements chargés depuis JSON.
/// Les outcomes nommés mappent vers des effets de jeu concrets.
/// </summary>
static class JsonEvents
{
    static readonly Random Rng = new();

    // ── OUTCOME CODES ────────────────────────────────────────────────────────
    // Les codes peuvent être chaînés avec '+' : "credits_medium+rep_small"

    public static void Apply(GameState state, string outcomeCode, string flavorText)
    {
        if (!string.IsNullOrEmpty(flavorText))
            Narrator.Say(flavorText, Color.Grey);

        foreach (var code in outcomeCode.Split('+', StringSplitOptions.RemoveEmptyEntries))
            ApplySingle(state, code.Trim());
    }

    static void ApplySingle(GameState state, string code)
    {
        switch (code)
        {
            // ── CRÉDITS ──────────────────────────────────────────────────────
            case "credits_tiny":
                var t = Rng.Next(50, 200);
                state.Credits += t;
                Display.ShowEvent($"+{t}cr.", Color.Green); break;

            case "credits_small":
                var s = Rng.Next(100, 500);
                state.Credits += s;
                Display.ShowEvent($"+{s}cr.", Color.Green); break;

            case "credits_medium":
                var m = Rng.Next(400, 1400);
                state.Credits += m;
                Display.ShowEvent($"+{m}cr.", Color.Green); break;

            case "credits_large":
                var l = Rng.Next(1400, 3500);
                state.Credits += l;
                Display.ShowEvent($"+{l}cr.", Color.Gold1); break;

            case "credits_huge":
                var h = Rng.Next(3000, 8000);
                state.Credits += h;
                Display.ShowEvent($"+{h}cr !", Color.Gold1); break;

            case "lose_credits_tiny":
                var lt = Rng.Next(50, 200);
                state.Credits = Math.Max(0, state.Credits - lt);
                Display.ShowEvent($"-{lt}cr.", Color.Red); break;

            case "lose_credits_small":
                var ls = Rng.Next(100, 500);
                state.Credits = Math.Max(0, state.Credits - ls);
                Display.ShowEvent($"-{ls}cr.", Color.Red); break;

            case "lose_credits_medium":
                var lm = Rng.Next(400, 1400);
                state.Credits = Math.Max(0, state.Credits - lm);
                Display.ShowEvent($"-{lm}cr.", Color.Red); break;

            // ── PV JOUEUR ─────────────────────────────────────────────────────
            case "hp_tiny":
                var ht = Math.Min(state.PlayerMaxHp - state.PlayerHp, Rng.Next(5, 15));
                state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + Rng.Next(5, 15));
                if (ht > 0) Display.ShowEvent($"+{ht} PV.", Color.Green); break;

            case "hp_small":
                var hs = Math.Min(state.PlayerMaxHp - state.PlayerHp, Rng.Next(15, 30));
                state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + Rng.Next(15, 30));
                if (hs > 0) Display.ShowEvent($"+{hs} PV.", Color.Green); break;

            case "hp_medium":
                var hm = Math.Min(state.PlayerMaxHp - state.PlayerHp, Rng.Next(30, 55));
                state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + Rng.Next(30, 55));
                if (hm > 0) Display.ShowEvent($"+{hm} PV.", Color.Green); break;

            case "lose_hp_tiny":
                var lht = Rng.Next(5, 15);
                state.PlayerHp = Math.Max(1, state.PlayerHp - lht);
                Display.ShowEvent($"-{lht} PV joueur.", Color.Red); break;

            case "lose_hp_small":
                var lhs = Rng.Next(10, 25);
                state.PlayerHp = Math.Max(1, state.PlayerHp - lhs);
                Display.ShowEvent($"-{lhs} PV joueur.", Color.Red); break;

            case "lose_hp_medium":
                var lhm = Rng.Next(25, 50);
                state.PlayerHp = Math.Max(1, state.PlayerHp - lhm);
                Display.ShowEvent($"-{lhm} PV joueur.", Color.Red); break;

            // ── PV VAISSEAU ───────────────────────────────────────────────────
            case "ship_repair_small":
                var sr = Math.Min(state.ShipMaxHp - state.ShipHp, Rng.Next(10, 25));
                state.ShipHp = Math.Min(state.ShipMaxHp, state.ShipHp + Rng.Next(10, 25));
                if (sr > 0) Display.ShowEvent($"+{sr} PV vaisseau.", Color.Green); break;

            case "lose_ship_small":
                var lsr = Rng.Next(10, 25);
                state.ShipHp = Math.Max(1, state.ShipHp - lsr);
                Display.ShowEvent($"-{lsr} PV vaisseau.", Color.Red); break;

            case "lose_ship_medium":
                var lsm = Rng.Next(25, 50);
                state.ShipHp = Math.Max(1, state.ShipHp - lsm);
                Display.ShowEvent($"-{lsm} PV vaisseau.", Color.Red); break;

            // ── STAMINA ───────────────────────────────────────────────────────
            case "stamina_full":
                state.Stamina = state.MaxStamina;
                Display.ShowEvent("Stamina au max.", Color.Cyan1); break;

            case "stamina_small":
                state.Stamina = Math.Min(state.MaxStamina, state.Stamina + Rng.Next(20, 40));
                Display.ShowEvent("+stamina.", Color.Cyan1); break;

            // ── RÉPUTATION ───────────────────────────────────────────────────
            case "rep_tiny":
                state.Reputation += Rng.Next(3, 8);
                Display.ShowEvent($"+{Rng.Next(3, 8)} réputation.", Color.Green); break;

            case "rep_small":
                var rs = Rng.Next(8, 20);
                state.Reputation += rs;
                Display.ShowEvent($"+{rs} réputation.", Color.Green); break;

            case "rep_medium":
                var rm = Rng.Next(20, 40);
                state.Reputation += rm;
                Display.ShowEvent($"+{rm} réputation.", Color.Green); break;

            case "rep_big":
                var rb = Rng.Next(40, 80);
                state.Reputation += rb;
                Display.ShowEvent($"+{rb} réputation !", Color.Gold1); break;

            case "rep_minus_tiny":
                state.Reputation -= Rng.Next(3, 8);
                Display.ShowEvent($"-{Rng.Next(3, 8)} réputation.", Color.Red); break;

            case "rep_minus_small":
                var rms = Rng.Next(8, 20);
                state.Reputation -= rms;
                Display.ShowEvent($"-{rms} réputation.", Color.Red); break;

            case "rep_minus_medium":
                var rmm = Rng.Next(20, 40);
                state.Reputation -= rmm;
                Display.ShowEvent($"-{rmm} réputation.", Color.Red); break;

            // ── CARBURANT ─────────────────────────────────────────────────────
            case "fuel_small":
                state.Fuel = Math.Min(state.MaxFuel, state.Fuel + Rng.Next(1, 3));
                Display.ShowEvent("+carburant.", Color.Cyan1); break;

            case "lose_fuel":
                state.Fuel = Math.Max(0, state.Fuel - 1);
                Display.ShowEvent("-1 carburant.", Color.Red); break;

            // ── OBJETS ────────────────────────────────────────────────────────
            case "item_food":
                var food = new[] { "Rations", "Vivres", "Eau" };
                var fi = food[Rng.Next(food.Length)];
                state.Cargo.Add(fi, 1);
                Display.ShowEvent($"+1 {fi}.", Color.Green); break;

            case "item_common":
                var commons = new[] { "Ferraille", "Pièces techniques", "Médicaments" };
                var ci = commons[Rng.Next(commons.Length)];
                state.Cargo.Add(ci, 1);
                Display.ShowEvent($"+1 {ci}.", Color.Green); break;

            case "item_rare":
                var rares = new[] { "Artefacts", "Plantes médicinales", "Cartes stellaires" };
                var ri = rares[Rng.Next(rares.Length)];
                state.Cargo.Add(ri, 1);
                Display.ShowEvent($"+1 {ri} !", Color.Gold1); break;

            case "item_weapon_low":
                var wl = WeaponPool.RollForTier(Rng.Next(1, 3));
                state.Weapons.Add(wl);
                Combat.ShowWeaponDrop(wl); break;

            case "item_weapon_mid":
                var wm = WeaponPool.RollForTier(Rng.Next(2, 4));
                state.Weapons.Add(wm);
                Combat.ShowWeaponDrop(wm); break;

            // ── COMBATS ───────────────────────────────────────────────────────
            case "combat_easy":
                var ce = Combat.TierLow[Rng.Next(Combat.TierLow.Count)];
                Situations.ApplyCombatOutcome(state, Combat.Start(state, ce)); break;

            case "combat_medium":
                var cm2 = Combat.TierMid[Rng.Next(Combat.TierMid.Count)];
                Situations.ApplyCombatOutcome(state, Combat.Start(state, cm2)); break;

            case "combat_hard":
                var ch = Combat.TierHigh[Rng.Next(Combat.TierHigh.Count)];
                Situations.ApplyCombatOutcome(state, Combat.Start(state, ch)); break;

            // ── STATUTS ───────────────────────────────────────────────────────
            case "imprisoned":
                state.IsImprisoned = true;
                Display.ShowEvent("Tu es arrêté.", Color.Red); break;

            case "addiction":
                state.AddictionLevel++;
                Display.ShowEvent($"+1 dépendance (niveau {state.AddictionLevel}).", Color.OrangeRed1); break;

            case "faction_mission":
                state.FactionMissions++;
                Display.ShowEvent("+1 mission faction.", Color.Cyan1); break;

            case "npc_met":
                state.NpcsMet.Add($"Inconnu-{Rng.Next(1000)}");
                break;

            case "nothing":
            default:
                break;
        }
    }

    // ── PRÉSENTATION D'UN EVENT JSON ─────────────────────────────────────────

    public static void RunExplorationEvent(GameState state, string zoneType)
    {
        var ev = ContentLoader.GetExplorationEvent(zoneType);
        if (ev == null) return;

        Narrator.Say(ev.Setup, Color.Grey);
        if (ev.Choices.Length == 0) { Narrator.Pause(); return; }

        var choices = ev.Choices.Select(c =>
            new Choice(c.Label, s =>
            {
                Apply(s, c.Outcome, c.Flavor);
                Narrator.Pause();
            })).ToList();

        ChoiceMenu.Resolve(new Situation("Que fais-tu ?", choices, Color.Grey), state);
    }

    public static void RunWanderEvent(GameState state, int danger)
    {
        var ev = ContentLoader.GetWanderEvent(state.CurrentStation, danger);
        if (ev == null) return;

        Narrator.Say(ev.Setup, Color.Grey);
        if (ev.Choices.Length == 0) { Narrator.Pause(); return; }

        var choices = ev.Choices.Select(c =>
            new Choice(c.Label, s =>
            {
                Apply(s, c.Outcome, c.Flavor);
                Narrator.Pause();
            })).ToList();

        ChoiceMenu.Resolve(new Situation("Comment tu réagis ?", choices, Color.Grey), state);
    }
}
