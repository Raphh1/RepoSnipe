using Spectre.Console;

namespace VoidTrader;

// ─────────────────────────────────────────────────────────────────────────────
// TYPES
// ─────────────────────────────────────────────────────────────────────────────

enum EnemyRole    { Normal, Tank, Ranged, Support }
enum CombatStance { Normal, Offensive, Defensive, Dodge }
enum EnemyIntent  { Normal, Heavy, Defend, Charge, Disarm }

record Enemy(
    string    Name,
    int       MaxHp,
    int       DamageMin,
    int       DamageMax,
    int       LootMin,
    int       LootMax,
    string    Description,
    int       CaptureChance = 20,
    int       KillChance    = 15,
    bool      IsBoss        = false,
    EnemyRole Role          = EnemyRole.Normal
);

enum CombatOutcome { Victory, Stunned, Captured, Dead, Fled, Quiz }

class CombatState
{
    public int          EnemyHp;
    public int          EnemyStunTurns;
    public int          EnemyBurnDmg;
    public int          EnemyBurnTurns;
    public bool         EnemyBlinded;
    public bool         PlayerFled;
    public bool         ImmunityUsed;
    // Système tactique
    public CombatStance PlayerStance             = CombatStance.Normal;
    public int          Momentum                 = 0;   // 0-3 ; à 3 = finisher dispo
    public bool         ClassActionUsed          = false;
    public EnemyIntent  CurrentIntent            = EnemyIntent.Normal;
    public bool         EnemyCharging            = false; // prépare un Heavy au tour suivant
    public int          EnemyWeaponDisabledTurns = 0;
    public int          LastPlayerDmg            = 0;    // dégâts infligés ce tour (pour momentum)
}

// Pour le combat multi-ennemis
class SquadMember
{
    public Enemy Base;
    public int   Hp;
    public int   StunTurns;
    public bool  Defeated => Hp <= 0;

    public SquadMember(Enemy e)
    {
        Base = e;
        Hp   = e.Role switch
        {
            EnemyRole.Tank    => (int)(e.MaxHp * 1.4f),
            EnemyRole.Ranged  => (int)(e.MaxHp * 0.8f),
            EnemyRole.Support => (int)(e.MaxHp * 0.7f),
            _                 => e.MaxHp,
        };
    }
}

class MultiCombatState
{
    public int  Momentum   = 0;
    public bool Fled       = false;
    public bool ClassUsed  = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// CLASSE COMBAT
// ─────────────────────────────────────────────────────────────────────────────

static class Combat
{
    private static readonly Random Rng = new();

    // ── POOLS D'ENNEMIS ──────────────────────────────────────────────────────

    public static readonly List<Enemy> TierLow =
    [
        new("Pickpocket désespéré",    20,  3,  8,   80,  300, "Un gamin avec un couteau rouillé."),
        new("Ivrogne agressif",        25,  2,  7,   60,  200, "Il sent le carburant frelaté."),
        new("Garde corrompu",          35,  5, 12,  200,  600, "Il a oublié pour qui il travaille."),
        new("Vagabond armé",           30,  4, 10,  100,  400, "Il veut juste survivre."),
        new("Scavenger opportuniste",  28,  4, 11,  150,  500, "Il récupère des armes dans des épaves."),
    ];

    public static readonly List<Enemy> TierMid =
    [
        new("Pirate solitaire",        50,  8, 18,  400,  900, "Freelance du crime. Expérimenté.", CaptureChance: 25),
        new("Mercenaire bas de gamme", 55,  9, 20,  500, 1000, "Payé pour te faire mal."),
        new("Contrebandier défensif",  45,  7, 16,  350,  800, "Il transporte quelque chose d'illégal."),
        new("Chasseur de primes",      60, 10, 22,  600, 1200, "Il a ta tête dans sa liste.", CaptureChance: 35),
        new("Gang de rue spatial",     65,  8, 19,  450,  950, "Trois contre un."),
    ];

    public static readonly List<Enemy> TierHigh =
    [
        new("Élite des Faucons Noirs",   120, 20, 40, 1000, 2500, "Entraîné, équipé, motivé.",          CaptureChance: 15, KillChance: 25),
        new("Garde de l'Emporium",       130, 18, 38,  800, 2000, "Armure lourde, zéro humour.",         CaptureChance: 40, KillChance: 5),
        new("Assassin du Conclave",      100, 25, 48, 1500, 3500, "Tu ne l'as pas vu venir.",            KillChance: 35),
        new("Bras droit d'Alanossa",     140, 22, 42, 1200, 3000, "Il a survécu à des dizaines de combats.", CaptureChance: 20, KillChance: 25),
        new("Soldat de Noctis",          115, 20, 38,  900, 2200, "Fanatique.",                          KillChance: 20),
    ];

    public static readonly List<Enemy> TierBoss =
    [
        new("Alanossa",               200, 30, 55, 4000, 8000, "Le pirate le plus dangereux de l'univers connu.", CaptureChance: 10, KillChance: 40, IsBoss: true),
        new("La Faucon",              180, 28, 52, 3500, 7000, "Cheffe des Faucons Noirs. Calculée.",             CaptureChance: 15, KillChance: 35, IsBoss: true),
        new("Directeur Pale",         160, 25, 48, 3000, 6000, "Il est calme. C'est pire.",                       CaptureChance: 30, KillChance: 25, IsBoss: true),
        new("Garde du Corps d'Eliotis",150, 22, 45, 2500, 5500, "Eliotis ne pardonne pas.",                       CaptureChance: 5,  KillChance: 45, IsBoss: true),
    ];

    // ── BOSS PAR STATION ──────────────────────────────────────────────────────

    public static readonly Dictionary<string, Enemy> NamedBosses = new()
    {
        ["Arc Ouest Apocalypse"]     = TierBoss[0],
        ["Le Nid des Faucons"]       = TierBoss[1],
        ["Les Abysses de Velkor"]    = TierBoss[2],
        ["Star Quest"]               = TierBoss[3],
        ["Emporium Requiem"]         = TierBoss[2],
        ["La Citadelle Écarlate"]    = TierBoss[1],
        ["Scotty Golden North"]      = TierBoss[3],
        ["La Couronne d'Eos"]        = TierBoss[0],
    };

    public static Enemy? StationBoss(GameState state)
        => NamedBosses.TryGetValue(state.CurrentStation, out var b) ? b : null;

    // ── SCALING ───────────────────────────────────────────────────────────────

    public static Enemy Scale(Enemy base_, int level)
    {
        if (level <= 0) return base_;
        var mult = 1f + level * 0.25f;
        return base_ with
        {
            MaxHp     = (int)(base_.MaxHp     * mult),
            DamageMin = (int)(base_.DamageMin * mult),
            DamageMax = (int)(base_.DamageMax * mult),
            LootMin   = (int)(base_.LootMin   * mult),
            LootMax   = (int)(base_.LootMax   * mult),
        };
    }

    public static Enemy GetScaled(GameState state, int depth)
    {
        var pool = depth switch
        {
            <= 1 => TierLow,
            <= 3 => TierMid,
            <= 5 => TierHigh,
            _    => TierBoss,
        };
        if (state.Day > 15 && pool == TierLow)  pool = TierMid;
        if (state.Day > 30 && pool == TierMid)  pool = TierHigh;

        return Scale(pool[Rng.Next(pool.Count)], Math.Max(0, depth - 1));
    }

    public static int MaxLootTier(GameState state)
        => state.BossesDefeated switch
        {
            >= 6 => 5,
            >= 4 => 4,
            >= 2 => 3,
            >= 1 => 2,
            _    => 1,
        };

    // ── INTENTION ENNEMIE ─────────────────────────────────────────────────────

    static EnemyIntent GenerateIntent(Enemy enemy, CombatState cs)
    {
        var roll = Rng.Next(100);

        // Basse santé → plus agressif
        if (cs.EnemyHp < enemy.MaxHp * 0.35f)
            return roll < 50 ? EnemyIntent.Heavy : roll < 70 ? EnemyIntent.Charge : EnemyIntent.Normal;

        // Boss → répertoire plus varié
        if (enemy.IsBoss)
        {
            return roll switch
            {
                < 25 => EnemyIntent.Normal,
                < 40 => EnemyIntent.Heavy,
                < 55 => EnemyIntent.Defend,
                < 72 => EnemyIntent.Charge,
                < 88 => EnemyIntent.Disarm,
                _    => EnemyIntent.Normal,
            };
        }

        return roll switch
        {
            < 50 => EnemyIntent.Normal,
            < 65 => EnemyIntent.Heavy,
            < 75 => EnemyIntent.Defend,
            < 87 => EnemyIntent.Charge,
            _    => EnemyIntent.Disarm,
        };
    }

    // ── MOTEUR DE COMBAT SOLO ─────────────────────────────────────────────────

    public static CombatOutcome Start(GameState state, Enemy enemy)
    {
        var cs = new CombatState { EnemyHp = enemy.MaxHp };
        state.Stamina = state.MaxStamina;

        Narrator.Say($"Combat contre {enemy.Name}.", Color.Red);
        AnsiConsole.MarkupLine($"[grey]{enemy.Description}[/]");
        ShowThreatWarning(state, enemy);
        AnsiConsole.WriteLine();

        while (true)
        {
            // ── Effets sur la durée ───────────────────────────────────────
            if (cs.EnemyBurnTurns > 0)
            {
                cs.EnemyHp    = Math.Max(0, cs.EnemyHp - cs.EnemyBurnDmg);
                cs.EnemyBurnTurns--;
                Display.ShowEvent($"Effet actif — {cs.EnemyBurnDmg} dégâts. {enemy.Name} : {cs.EnemyHp}/{enemy.MaxHp} PV", Color.OrangeRed1);
                if (cs.EnemyHp <= 0) return ResolveVictory(state, enemy);
            }

            // ── Intention ennemie ─────────────────────────────────────────
            cs.CurrentIntent = cs.EnemyCharging ? EnemyIntent.Heavy : GenerateIntent(enemy, cs);

            // ── Affichage statut ──────────────────────────────────────────
            ShowCombatStatus(state, enemy, cs);

            // ── Tour du joueur ────────────────────────────────────────────
            cs.LastPlayerDmg = 0;
            var choices = BuildChoices(state, enemy, cs);
            var choice  = ChoiceMenu.Present(new Situation("Ton action :", choices), state);
            if (choice is null) continue;

            AnsiConsole.WriteLine();
            choice.Effect?.Invoke(state);

            if (cs.PlayerFled)      return CombatOutcome.Fled;
            if (cs.EnemyHp <= 0)   return ResolveVictory(state, enemy);
            if (state.IsImprisoned) return CombatOutcome.Captured;

            // ── Momentum ──────────────────────────────────────────────────
            if (cs.LastPlayerDmg > 0)
            {
                cs.Momentum = Math.Min(3, cs.Momentum + 1);
                if (cs.Momentum == 3)
                    Display.ShowEvent("⚡ MOMENTUM MAXIMUM — Finisher disponible !", Color.Gold1);
            }

            // ── Tour ennemi ───────────────────────────────────────────────
            if (cs.EnemyStunTurns > 0)
            {
                cs.EnemyStunTurns--;
                Display.ShowEvent($"{enemy.Name} est étourdi — perd son tour.", Color.Yellow);
            }
            else if (cs.EnemyWeaponDisabledTurns > 0)
            {
                cs.EnemyWeaponDisabledTurns--;
                var hackDmg = Rng.Next(2, 8);
                state.PlayerHp = Math.Max(0, state.PlayerHp - hackDmg);
                Display.ShowEvent($"[HACK] {enemy.Name} improvise — {hackDmg} dégâts. ({cs.EnemyWeaponDisabledTurns}t restant)", Color.Cyan1);
                if (state.PlayerHp <= 0) { Narrator.Say("Tu tombes.", Color.Red); Narrator.Pause(); return DetermineDefeat(enemy); }
            }
            else
            {
                int  enemyDmg = 0;
                bool isCrit   = false;
                bool skipped  = false;

                switch (cs.CurrentIntent)
                {
                    case EnemyIntent.Heavy:
                        cs.EnemyCharging = false;
                        enemyDmg = (int)(Rng.Next(enemy.DamageMin, enemy.DamageMax + 1) * 1.8f);
                        Narrator.Say($"{enemy.Name} frappe de toutes ses forces !", Color.Red);
                        break;

                    case EnemyIntent.Defend:
                        Display.ShowEvent($"{enemy.Name} reprend son souffle — aucune attaque ce tour.", Color.Grey);
                        skipped = true;
                        break;

                    case EnemyIntent.Charge:
                        cs.EnemyCharging = true;
                        enemyDmg = Math.Max(1, Rng.Next(1, Math.Max(2, enemy.DamageMin / 2)));
                        Display.ShowEvent($"{enemy.Name} se concentre — attaque légère. [red]Frappe dévastatrice au prochain tour.[/]", Color.OrangeRed1);
                        break;

                    case EnemyIntent.Disarm:
                        enemyDmg = Rng.Next(enemy.DamageMin / 2, Math.Max(enemy.DamageMin / 2 + 1, enemy.DamageMax / 2));
                        if (state.EquippedWeapon != null && Rng.Next(100) < 30)
                        {
                            var w = state.EquippedWeapon;
                            state.EquippedWeapon = null;
                            Display.ShowEvent($"{enemy.Name} t'arrache [orange1]{w.Name}[/] ! Tu te bats à mains nues.", Color.Red);
                        }
                        break;

                    default: // Normal
                        enemyDmg = Rng.Next(enemy.DamageMin, enemy.DamageMax + 1);
                        if (cs.EnemyBlinded) { enemyDmg = (int)(enemyDmg * 0.6f); cs.EnemyBlinded = false; }
                        isCrit = Rng.Next(100) < 10;
                        if (isCrit) { enemyDmg = (int)(enemyDmg * 1.8f); Display.ShowEvent("[bold yellow]COUP CRITIQUE ![/]", Color.Gold1); }
                        break;
                }

                if (!skipped && enemyDmg > 0)
                {
                    // Modificateur de stance
                    switch (cs.PlayerStance)
                    {
                        case CombatStance.Defensive:
                            enemyDmg = (int)(enemyDmg * 0.70f);
                            break;
                        case CombatStance.Offensive:
                            enemyDmg = (int)(enemyDmg * 1.20f);
                            break;
                        case CombatStance.Dodge:
                            if (Rng.Next(100) < 40)
                            {
                                Display.ShowEvent("Tu esquives parfaitement l'attaque !", Color.Cyan1);
                                enemyDmg = 0;
                            }
                            break;
                    }
                }

                if (!skipped && enemyDmg > 0)
                {
                    // Application armure
                    var armor = state.EquippedArmor;
                    if (armor != null)
                    {
                        var reduced = (int)(enemyDmg * armor.Defense / 100.0f);
                        enemyDmg -= reduced;
                        if (reduced > 0)
                            Display.ShowEvent($"[steelblue1]Armure absorbe {reduced} dégâts.[/]", Color.SteelBlue1);

                        if (armor.Effect == ArmorEffect.Thorns)
                        {
                            var thornDmg = (int)(enemyDmg * armor.EffectValue / 100.0f);
                            if (thornDmg > 0)
                            {
                                cs.EnemyHp = Math.Max(0, cs.EnemyHp - thornDmg);
                                Display.ShowEvent($"Épines — {thornDmg} dégâts renvoyés à {enemy.Name}.", Color.OrangeRed1);
                            }
                        }

                        if (armor.Effect == ArmorEffect.Immunity && !cs.ImmunityUsed && state.PlayerHp <= enemyDmg)
                        {
                            cs.ImmunityUsed = true;
                            enemyDmg = 0;
                            Display.ShowEvent("L'Armure du Vide absorbe le coup fatal. Une seule fois.", Color.Gold1);
                        }
                    }

                    if (enemyDmg > 0)
                    {
                        cs.Momentum    = 0;
                        state.PlayerHp = Math.Max(0, state.PlayerHp - enemyDmg);

                        var stanceNote = cs.PlayerStance switch
                        {
                            CombatStance.Defensive => " [steelblue1](Défensif -30%)[/]",
                            CombatStance.Offensive => " [red](Offensif — exposé +20%)[/]",
                            CombatStance.Dodge     => " [grey](Esquive manquée)[/]",
                            _                      => "",
                        };

                        Display.ShowEvent(
                            $"{enemy.Name} attaque{(isCrit ? " [bold](CRITIQUE)[/]" : "")}{stanceNote} — {enemyDmg} dégâts. " +
                            $"PV : {state.PlayerHp}/{state.PlayerMaxHp}", Color.Red);

                        if (state.PlayerHp <= 0)
                        {
                            Narrator.Say($"Tu tombes. {enemy.Name} se penche sur toi...", Color.Red);
                            Narrator.Pause();
                            return DetermineDefeat(enemy);
                        }
                    }
                }
            }

            // ── Régénération armure ───────────────────────────────────────
            if (state.EquippedArmor?.Effect == ArmorEffect.Regen)
            {
                var regen = state.EquippedArmor.EffectValue;
                state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + regen);
                if (regen > 0) Display.ShowEvent($"Régénération armure : +{regen} PV.", Color.Green);
            }

            // ── Récupération stamina ──────────────────────────────────────
            var staminaRegen = 20 + (state.EquippedArmor?.Effect == ArmorEffect.StaminaBoost ? state.EquippedArmor.EffectValue : 0);
            if (cs.PlayerStance == CombatStance.Defensive) staminaRegen += 10;
            state.Stamina = Math.Min(state.MaxStamina, state.Stamina + staminaRegen);

            AnsiConsole.WriteLine();
        }
    }

    // ── CONSTRUCTION DES CHOIX ────────────────────────────────────────────────

    static List<Choice> BuildChoices(GameState state, Enemy enemy, CombatState cs)
    {
        var weapon  = state.EquippedWeapon;
        var choices = new List<Choice>();

        // ── FINISHER (priorité d'affichage) ──────────────────────────────
        if (cs.Momentum >= 3)
        {
            choices.Add(new("💥 [bold gold1]FINISHER[/] — Attaque dévastatrice  [grey dim](3× dégâts, reset momentum)[/]", s =>
            {
                cs.PlayerStance  = CombatStance.Offensive;
                var dmg = CalcDamage(s, weapon, false) * 3;
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                cs.LastPlayerDmg = dmg;
                cs.Momentum      = 0;
                Narrator.Say($"FINISHER ! {dmg} dégâts dévastateurs !", Color.Gold1);
                Display.ShowEvent($"{enemy.Name} : {cs.EnemyHp}/{enemy.MaxHp} PV", Color.Red);
                ApplySelfDamage(s, weapon);
            }));
        }

        // ── ATTAQUE NORMALE ───────────────────────────────────────────────
        var atkLabel = weapon != null
            ? $"⚔  Attaquer avec {weapon.Name}  [grey dim](-20 stamina)[/]"
            : $"👊 Combat à mains nues  [grey dim](-20 stamina)[/]";

        choices.Add(new(atkLabel, s =>
        {
            cs.PlayerStance = CombatStance.Normal;
            if (s.Stamina <= 0)
            {
                var weakDmg = Rng.Next(3, 10);
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - weakDmg);
                cs.LastPlayerDmg = weakDmg;
                Narrator.Say("Tu es épuisé. Attaque faible.", Color.Yellow);
                Display.ShowEvent($"Attaque épuisée — {weakDmg} dégâts.", Color.Yellow);
            }
            else
            {
                s.Stamina -= 20;
                var dmg = CalcDamage(s, weapon, false);
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                cs.LastPlayerDmg = dmg;
                Display.ShowEvent($"Tu infliges {dmg} dégâts. {enemy.Name} : {cs.EnemyHp}/{enemy.MaxHp} PV", Color.Green);
                ApplySelfDamage(s, weapon);
            }
        }));

        // ── ATTAQUE OFFENSIVE ─────────────────────────────────────────────
        if (state.Stamina >= 20)
        {
            choices.Add(new("⚡ Attaque offensive  [grey dim]+30% dmg, tu t'exposes (+20% dmg reçu)  (-20 stamina)[/]", s =>
            {
                cs.PlayerStance  = CombatStance.Offensive;
                s.Stamina       -= 20;
                var dmg          = (int)(CalcDamage(s, weapon, false) * 1.3f);
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                cs.LastPlayerDmg = dmg;
                Display.ShowEvent($"[Offensif] {dmg} dégâts ! Tu t'exposes.", Color.OrangeRed1);
                ApplySelfDamage(s, weapon);
            }));
        }

        // ── ATTAQUE DÉFENSIVE ─────────────────────────────────────────────
        if (state.Stamina >= 15)
        {
            choices.Add(new("🛡 Attaque défensive  [grey dim]-30% dmg reçu ce tour, +10 stamina  (-15 stamina)[/]", s =>
            {
                cs.PlayerStance  = CombatStance.Defensive;
                s.Stamina       -= 15;
                s.Stamina        = Math.Min(s.MaxStamina, s.Stamina + 10);
                var dmg          = CalcDamage(s, weapon, false);
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                cs.LastPlayerDmg = dmg;
                Display.ShowEvent($"[Défensif] {dmg} dégâts. Tu te protèges.", Color.SteelBlue1);
                ApplySelfDamage(s, weapon);
            }));
        }

        // ── ATTAQUE EN ESQUIVE ────────────────────────────────────────────
        if (state.Stamina >= 10)
        {
            choices.Add(new("💨 Attaque en esquive  [grey dim]40% d'éviter le prochain coup, -40% dmg infligé  (-10 stamina)[/]", s =>
            {
                cs.PlayerStance  = CombatStance.Dodge;
                s.Stamina       -= 10;
                var dmg          = (int)(CalcDamage(s, weapon, false) * 0.6f);
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                cs.LastPlayerDmg = Math.Max(1, dmg);
                Display.ShowEvent($"[Esquive] {dmg} dégâts. Tu te repositionnes.", Color.Cyan1);
                ApplySelfDamage(s, weapon);
            }));
        }

        // ── ATTAQUE SPÉCIALE ──────────────────────────────────────────────
        if (weapon != null && weapon.Effect is not (WeaponEffect.None or WeaponEffect.Silence or WeaponEffect.ArmorPierce) && state.Stamina >= 35)
        {
            choices.Add(new($"✨ Attaque spéciale — {weapon.EffectDesc}  [grey dim](-35 stamina)[/]", s =>
            {
                cs.PlayerStance = CombatStance.Offensive;
                s.Stamina -= 35;
                var dmg = CalcDamage(s, weapon, true);
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                cs.LastPlayerDmg = dmg;
                Display.ShowEvent($"Attaque spéciale ! {dmg} dégâts.", Color.Gold1);
                if (Rng.Next(100) < weapon.EffectChance)
                    ApplyEffect(weapon.Effect, cs, s, dmg, enemy.Name);
                else
                    Display.ShowEvent("L'effet spécial ne s'est pas déclenché.", Color.Grey);
                ApplySelfDamage(s, weapon);
            }));
        }

        // ── FRAPPE CONCENTRÉE ─────────────────────────────────────────────
        if (state.Stamina >= 50)
        {
            choices.Add(new("🎯 Frappe concentrée  [grey dim](-50 stamina, +60% dégâts)[/]", s =>
            {
                cs.PlayerStance = CombatStance.Normal;
                s.Stamina -= 50;
                var dmg = (int)(CalcDamage(s, weapon, false) * 1.6f);
                cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                cs.LastPlayerDmg = dmg;
                Display.ShowEvent($"Frappe concentrée — {dmg} dégâts !", Color.Gold1);
            }));
        }

        // ── ACTION DE CLASSE ──────────────────────────────────────────────
        if (!cs.ClassActionUsed)
        {
            var classAction = BuildClassAction(state, enemy, cs);
            if (classAction != null) choices.Add(classAction);
        }

        // ── FUIR ──────────────────────────────────────────────────────────
        choices.Add(new("🏃 Fuir", s =>
        {
            var chance = 50 + (s.Fuel > 2 ? 15 : 0) + (s.Class.Name == "Contrebandier" ? 20 : 0);
            if (Rng.Next(100) < chance)
            {
                s.Fuel = Math.Max(0, s.Fuel - 1);
                cs.PlayerFled = true;
                Narrator.Say("Tu t'échappes. -1 carburant.", Color.Yellow);
            }
            else
                Narrator.Say($"{enemy.Name} te coupe la route.", Color.Red);
        }, s => s.Fuel > 0));

        // ── NÉGOCIER ──────────────────────────────────────────────────────
        choices.Add(new("🗣 Négocier", s =>
        {
            var chance = 20 + Math.Max(0, s.Reputation / 8);
            if (Rng.Next(100) < chance)
            {
                Narrator.Say("Tu trouves les mots. L'ennemi baisse son arme.", Color.Green);
                s.Reputation += 5;
                cs.EnemyHp = 0;
            }
            else
                Narrator.Say("Ça ne marche pas.", Color.Red);
        }));

        // ── INTIMIDER ─────────────────────────────────────────────────────
        if (state.Reputation > 0)
        {
            choices.Add(new("😤 Intimider", s =>
            {
                var chance = s.Reputation / 5 + (s.Class.Name == "Seigneur de guerre" ? 30 : 0);
                if (Rng.Next(100) < chance)
                {
                    Narrator.Say($"Ta réputation parle. {enemy.Name} recule.", Color.Green);
                    cs.EnemyHp = 0;
                }
                else
                    Narrator.Say("Il n'est pas impressionné.", Color.Red);
            }));
        }

        // ── SE SOIGNER ────────────────────────────────────────────────────
        if (state.Cargo.Get("Médicaments") > 0)
        {
            choices.Add(new($"💊 Se soigner +30 PV  [grey dim](stock : {state.Cargo.Get("Médicaments")})[/]", s =>
            {
                s.Cargo.Remove("Médicaments", 1);
                s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 30);
                Display.ShowEvent($"Soigné. PV joueur : {s.PlayerHp}/{s.PlayerMaxHp}", Color.Green);
            }));
        }

        return choices;
    }

    // ── ACTIONS DE CLASSE ─────────────────────────────────────────────────────

    static Choice? BuildClassAction(GameState state, Enemy enemy, CombatState cs)
    {
        return state.Class.Name switch
        {
            "Seigneur de guerre" => new(
                "😤 [cyan1][Seigneur de guerre][/] Intimidation de combat  [grey dim](ennemi perd son tour, 1 fois/combat)[/]",
                s =>
                {
                    cs.ClassActionUsed = true;
                    var chance = 40 + Math.Max(0, s.Reputation / 5);
                    if (Rng.Next(100) < chance)
                    {
                        cs.EnemyStunTurns = 1;
                        Display.ShowEvent("Ton aura écrase l'ennemi. Il recule et perd son tour.", Color.Gold1);
                    }
                    else
                        Display.ShowEvent("L'intimidation n'a pas suffi cette fois.", Color.Grey);
                }),

            "Médecin" => new(
                "💊 [cyan1][Médecin][/] Soin rapide  [grey dim](+30 PV sans perdre son tour, -20 stamina, 1 fois/combat)[/]",
                s =>
                {
                    cs.ClassActionUsed = true;
                    s.Stamina = Math.Max(0, s.Stamina - 20);
                    var healed = Math.Min(30, s.PlayerMaxHp - s.PlayerHp);
                    s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 30);
                    Display.ShowEvent($"Soin rapide ! +{healed} PV. {s.PlayerHp}/{s.PlayerMaxHp}", Color.Green);
                },
                s => s.Stamina >= 20),

            "Hackeur" => new(
                "💻 [cyan1][Hackeur][/] Pirater l'équipement  [grey dim](arme ennemie désactivée 2 tours, -30 stamina, 1 fois/combat)[/]",
                s =>
                {
                    cs.ClassActionUsed = true;
                    s.Stamina = Math.Max(0, s.Stamina - 30);
                    if (Rng.Next(100) < 70)
                    {
                        cs.EnemyWeaponDisabledTurns = 2;
                        Display.ShowEvent($"[bold cyan1]HACK RÉUSSI ![/] L'armement de {enemy.Name} est désactivé 2 tours.", Color.Cyan1);
                    }
                    else
                        Display.ShowEvent("Le hack a échoué — systèmes de défense actifs.", Color.Grey);
                },
                s => s.Stamina >= 30),

            "Contrebandier" => new(
                "🏃 [cyan1][Contrebandier][/] Fuite garantie  [grey dim](100% succès, -1 carburant, 1 fois/combat)[/]",
                s =>
                {
                    cs.ClassActionUsed = true;
                    s.Fuel = Math.Max(0, s.Fuel - 1);
                    cs.PlayerFled = true;
                    Narrator.Say("Tu connais toutes les sorties. Fuite garantie. -1 carburant.", Color.Yellow);
                },
                s => s.Fuel > 0),

            "Vagabond" => new(
                "🦵 [cyan1][Vagabond][/] Coup bas  [grey dim](ignore armure, dégâts élevés, -10 réputation, 1 fois/combat)[/]",
                s =>
                {
                    cs.ClassActionUsed = true;
                    var dmg = CalcDamage(s, s.EquippedWeapon, false) + Rng.Next(15, 35);
                    cs.EnemyHp       = Math.Max(0, cs.EnemyHp - dmg);
                    cs.LastPlayerDmg = dmg;
                    s.Reputation    -= 10;
                    Display.ShowEvent($"Coup bas — {dmg} dégâts (armure ignorée). -10 réputation.", Color.Red);
                }),

            _ => null,
        };
    }

    // ── EFFETS D'ARMES ────────────────────────────────────────────────────────

    static void ApplyEffect(WeaponEffect effect, CombatState cs, GameState state, int dmg, string enemyName)
    {
        switch (effect)
        {
            case WeaponEffect.Stun:
                cs.EnemyStunTurns = 1;
                Display.ShowEvent("Étourdi ! L'ennemi perd son prochain tour.", Color.Gold1); break;

            case WeaponEffect.Paralyze:
                cs.EnemyStunTurns = 2;
                Display.ShowEvent("Paralysé ! L'ennemi perd 2 tours.", Color.Gold1); break;

            case WeaponEffect.Burn:
                cs.EnemyBurnDmg   = Rng.Next(8, 20);
                cs.EnemyBurnTurns = 3;
                Display.ShowEvent($"Brûlure ! {cs.EnemyBurnDmg} dégâts/tour pendant 3 tours.", Color.OrangeRed1); break;

            case WeaponEffect.Poison:
                cs.EnemyBurnDmg   = Rng.Next(5, 15);
                cs.EnemyBurnTurns = 4;
                Display.ShowEvent($"Empoisonné ! {cs.EnemyBurnDmg} dégâts/tour pendant 4 tours.", Color.Green); break;

            case WeaponEffect.Blind:
                cs.EnemyBlinded = true;
                Display.ShowEvent("Aveuglé ! -40% précision ennemi au prochain tour.", Color.Yellow); break;

            case WeaponEffect.Flee:
                cs.EnemyHp = 0;
                Display.ShowEvent("L'ennemi panique et fuit !", Color.Gold1); break;

            case WeaponEffect.Distraction:
                cs.EnemyStunTurns = 1;
                Display.ShowEvent("Distrait ! L'ennemi perd son tour.", Color.Yellow); break;

            case WeaponEffect.Confusion:
                var confDmg = Rng.Next(5, 20);
                state.PlayerHp = Math.Max(1, state.PlayerHp - confDmg);
                Display.ShowEvent($"L'ennemi confus te frappe pour {confDmg} dégâts.", Color.OrangeRed1); break;

            case WeaponEffect.Random:
                var rnd = (WeaponEffect)Rng.Next(1, 8);
                Display.ShowEvent($"Effet aléatoire : {rnd}.", Color.Magenta1);
                ApplyEffect(rnd, cs, state, dmg, enemyName); break;
        }
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    static void ApplySelfDamage(GameState state, WeaponData? weapon)
    {
        if (weapon is null || weapon.SelfDmgChance == 0) return;
        if (Rng.Next(100) >= weapon.SelfDmgChance) return;

        var selfDmg = weapon.SelfDmgMax > 0 ? Rng.Next(weapon.SelfDmgMax / 2, weapon.SelfDmgMax + 1) : 0;

        var message = weapon.Name switch
        {
            "Lance-flammes compact"        => $"Les flammes te lèchent au passage. -{selfDmg} PV.",
            "Fusil à pompe galactique"     => $"Le recul t'arrache l'épaule. -{selfDmg} PV.",
            "Gatling à plasma Bonne Nuit"  => $"Une balle ricoche et te touche. -{selfDmg} PV.",
            "Canon à trou noir miniaturisé"=> $"Le champ gravitationnel te compresse. -{selfDmg} PV.",
            "Canon à singularité"          => $"La singularité aspire dans ta direction. -{selfDmg} PV.",
            "Dernière Parole"              => $"La détonation t'arrache l'audition. -{selfDmg} PV.",
            "Le Sceptre de Raphazarus"     => $"L'effet aléatoire se retourne contre toi. -{selfDmg} PV.",
            "Bombe à paradoxe"             => $"La bombe explose dans toutes les directions. -{selfDmg} PV.",
            "L'Insistance"                 => $"Tirs infinis, quelques balles dévient. -{selfDmg} PV.",
            _                              => $"{weapon.Name} te blesse dans le processus. -{selfDmg} PV.",
        };

        if (selfDmg > 0)
        {
            state.PlayerHp = Math.Max(1, state.PlayerHp - selfDmg);
            Display.ShowEvent(message, Color.OrangeRed1);
        }
    }

    static int CalcDamage(GameState state, WeaponData? weapon, bool special)
    {
        int baseDmg;
        if (weapon != null)
        {
            baseDmg = Rng.Next(weapon.DamageMin, weapon.DamageMax + 1);
            baseDmg = (int)(baseDmg * WeaponPool.AffinityModifier(weapon, state.Class));
            if (weapon.Effect == WeaponEffect.ArmorPierce) baseDmg = (int)(baseDmg * 1.3f);
            var critChance = special ? weapon.CritChance + 10 : weapon.CritChance;
            if (Rng.Next(100) < critChance) { baseDmg = (int)(baseDmg * 2.0f); Display.ShowEvent("[bold yellow]COUP CRITIQUE ![/]", Color.Gold1); }
        }
        else
        {
            baseDmg = Rng.Next(5, 18) + state.Class.Name switch
            {
                "Seigneur de guerre" => Rng.Next(8, 20),
                "Vétéran"            => Rng.Next(5, 12),
                "Vagabond"           => -Rng.Next(0, 5),
                _                    => 0,
            };
            if (Rng.Next(100) < 10) { baseDmg = (int)(baseDmg * 2.0f); Display.ShowEvent("[bold yellow]COUP CRITIQUE ![/]", Color.Gold1); }
        }
        return Math.Max(1, baseDmg);
    }

    static CombatOutcome ResolveVictory(GameState state, Enemy enemy)
    {
        var loot = Rng.Next(enemy.LootMin, enemy.LootMax + 1);
        state.Credits    += loot;
        state.Reputation += 10;

        if (TierBoss.Any(b => b.Name == enemy.Name))
        {
            state.BossesDefeated++;
            var legendary = WeaponPool.RollForTier(5);
            state.Weapons.Add(legendary);
            Narrator.Say($"{enemy.Name} tombe. Une légende s'éteint. +{loot}cr, +10 réputation.", Color.Gold1);
            ShowWeaponDrop(legendary);
            Narrator.Pause();
            return CombatOutcome.Victory;
        }

        var dropRoll = Rng.Next(100);
        if (dropRoll < 20)
        {
            var dropped = WeaponPool.RollForTier(Rng.Next(1, 3));
            state.Weapons.Add(dropped);
            Narrator.Say($"{enemy.Name} est vaincu ! +{loot}cr, +10 rep.", Color.Gold1);
            ShowWeaponDrop(dropped);
        }
        else if (dropRoll < 30)
        {
            var dropped = ArmorPool.RollForTier(Rng.Next(1, 3));
            state.Armors.Add(dropped);
            Narrator.Say($"{enemy.Name} est vaincu ! +{loot}cr, +10 rep.", Color.Gold1);
            ShowArmorDrop(dropped);
        }
        else
            Narrator.Say($"{enemy.Name} est vaincu ! +{loot}cr, +10 réputation.", Color.Gold1);

        Narrator.Pause();
        return CombatOutcome.Victory;
    }

    static CombatOutcome DetermineDefeat(Enemy enemy)
    {
        var roll = Rng.Next(100);
        if (roll < enemy.KillChance)                        return CombatOutcome.Dead;
        if (roll < enemy.KillChance + enemy.CaptureChance)  return CombatOutcome.Captured;
        return CombatOutcome.Stunned;
    }

    // ── AFFICHAGE STATUT ──────────────────────────────────────────────────────

    static void ShowCombatStatus(GameState state, Enemy enemy, CombatState cs)
    {
        var pHp = state.PlayerHp > state.PlayerMaxHp * 0.5 ? "green"   : state.PlayerHp > state.PlayerMaxHp * 0.25 ? "yellow" : "red";
        var eHp = cs.EnemyHp    > enemy.MaxHp         * 0.5 ? "green"   : cs.EnemyHp    > enemy.MaxHp         * 0.25 ? "yellow" : "red";
        var sta = state.Stamina > state.MaxStamina     * 0.5 ? "cyan1"  : state.Stamina > state.MaxStamina     * 0.25 ? "yellow" : "red";

        var wName     = state.EquippedWeapon != null ? $"[orange1]{state.EquippedWeapon.Name}[/]" : "[grey]mains nues[/]";
        var armorName = state.EquippedArmor  != null ? $"  [steelblue1]🛡 {state.EquippedArmor.Name}[/]" : "";

        var eStatus = cs.EnemyStunTurns > 0              ? " [yellow](étourdi)[/]"
                    : cs.EnemyBlinded                    ? " [yellow](aveuglé)[/]"
                    : cs.EnemyWeaponDisabledTurns > 0    ? $" [cyan1](hacké {cs.EnemyWeaponDisabledTurns}t)[/]"
                    : cs.EnemyCharging                   ? " [red bold](se prépare !)[/]"
                    : "";

        var intentText = cs.CurrentIntent switch
        {
            EnemyIntent.Heavy   => "[red bold]⚠ Attaque lourde imminente[/]",
            EnemyIntent.Defend  => "[grey]☰ Se replie — pas d'attaque ce tour[/]",
            EnemyIntent.Charge  => "[orange1]⚡ Se concentre — frappe dévastatrice au prochain tour ![/]",
            EnemyIntent.Disarm  => "[yellow]✋ Vise ton arme[/]",
            _                   => "[grey]→ Attaque standard[/]",
        };

        var momentumBar = cs.Momentum switch
        {
            0 => "[grey]○○○[/]",
            1 => "[yellow]●○○[/]",
            2 => "[orange1]●●○[/]",
            _ => "[gold1 bold]●●● FINISHER ![/]",
        };

        var stanceLabel = cs.PlayerStance switch
        {
            CombatStance.Offensive => " [red](Offensif)[/]",
            CombatStance.Defensive => " [steelblue1](Défensif)[/]",
            CombatStance.Dodge     => " [cyan1](Esquive)[/]",
            _                      => "",
        };

        var table = new Table().Border(TableBorder.Rounded).HideHeaders()
            .AddColumn("").AddColumn("");

        table.AddRow(
            $"[white]Toi[/]{stanceLabel}  [{pHp}]{state.PlayerHp}/{state.PlayerMaxHp} PV[/]  [{sta}]Stamina {state.Stamina}/{state.MaxStamina}[/]  {wName}{armorName}\n  Momentum : {momentumBar}",
            $"[red]{enemy.Name}[/]{eStatus}  [{eHp}]{cs.EnemyHp}/{enemy.MaxHp} PV[/]\n  Intention : {intentText}"
        );
        AnsiConsole.Write(table);
    }

    static void ShowThreatWarning(GameState state, Enemy enemy)
    {
        var playerPower = state.PlayerHp + (state.EquippedWeapon?.DamageMax ?? 15) + state.Stamina / 2;
        var enemyPower  = enemy.MaxHp + enemy.DamageMax * 3;
        var ratio       = (float)enemyPower / playerPower;

        // Alerte équipement pour les boss
        if (enemy.IsBoss && (state.EquippedWeapon?.Tier ?? 0) < 3)
            AnsiConsole.MarkupLine("[yellow]⚠ Un boss de cette envergure exige une arme Tier 3+. Tes dégâts seront insuffisants.[/]");

        if      (ratio >= 3.0f) AnsiConsole.MarkupLine("[red bold]⚠ DANGER EXTRÊME — Ce combat est probablement impossible.[/]");
        else if (ratio >= 2.0f) AnsiConsole.MarkupLine("[red]⚠ ADVERSAIRE REDOUTABLE — La négociation ou la fuite sont sérieuses.[/]");
        else if (ratio >= 1.4f) AnsiConsole.MarkupLine("[yellow]⚡ ADVERSAIRE DIFFICILE — Sois stratégique.[/]");
    }

    public static void ShowWeaponDrop(WeaponData w)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [orange1 bold]── ARME RÉCUPÉRÉE ──[/]");
        AnsiConsole.MarkupLine($"  [white]{w.Name}[/]  [grey]Tier {w.Tier}[/]");
        AnsiConsole.MarkupLine($"  [grey]Dégâts :[/] {w.DamageMin}–{w.DamageMax}   [grey]Crit :[/] {w.CritChance}%");
        if (w.Effect != WeaponEffect.None)
            AnsiConsole.MarkupLine($"  [cyan1]Effet :[/] {w.EffectDesc} ({w.EffectChance}% de déclenchement)");
        if (w.SelfDmgChance > 0)
            AnsiConsole.MarkupLine($"  [red]⚠ Self-damage :[/] {w.SelfDmgChance}% de chance ({w.SelfDmgMax / 2}–{w.SelfDmgMax} dégâts)");
        AnsiConsole.MarkupLine($"  [grey dim]Valeur revente : ~{w.Tier * 300 + w.DamageMax * 2}cr[/]");
        AnsiConsole.WriteLine();
    }

    public static void ShowArmorDrop(ArmorData a)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [steelblue1 bold]── ARMURE RÉCUPÉRÉE ──[/]");
        AnsiConsole.MarkupLine($"  [white]{a.Name}[/]  [grey]Tier {a.Tier}[/]");
        AnsiConsole.MarkupLine($"  [grey]Réduction dégâts :[/] {a.Defense}%   [grey]PV bonus :[/] +{a.HpBonus}");
        if (!string.IsNullOrEmpty(a.Description))
            AnsiConsole.MarkupLine($"  [grey dim]{a.Description}[/]");
        if (a.Effect != ArmorEffect.None)
            AnsiConsole.MarkupLine($"  [cyan1]Effet passif :[/] {a.Effect} — {a.EffectValue}");
        AnsiConsole.MarkupLine($"  [grey dim]Valeur revente : ~{a.SellValue}cr[/]");
        AnsiConsole.WriteLine();
    }

    // ── COMBAT MULTI-ENNEMIS ──────────────────────────────────────────────────

    public static CombatOutcome StartMulti(GameState state, List<Enemy> enemies)
    {
        if (enemies.Count == 0) return CombatOutcome.Victory;
        if (enemies.Count == 1) return Start(state, enemies[0]);

        var squad = enemies.Select(e => new SquadMember(e)).ToList();
        var mcs   = new MultiCombatState();

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red bold]COMBAT MULTIPLE[/]").RuleStyle("red"));
        Narrator.Say($"Tu fais face à {squad.Count} adversaires simultanément !", Color.Red);
        ShowSquadRoles(squad);
        AnsiConsole.WriteLine();

        while (squad.Any(m => !m.Defeated) && !mcs.Fled && !state.IsDead)
        {
            ShowSquadStatus(state, squad, mcs.Momentum);
            var alive = squad.Where(m => !m.Defeated).ToList();
            var choices = BuildMultiChoices(state, squad, alive, mcs);

            var choice = ChoiceMenu.Present(new Situation("Qui attaques-tu ?", choices), state);
            if (choice is null) continue;

            AnsiConsole.WriteLine();
            choice.Effect?.Invoke(state);

            if (mcs.Fled)           return CombatOutcome.Fled;
            if (state.IsImprisoned) return CombatOutcome.Captured;

            // Tous les ennemis vivants attaquent
            foreach (var m in alive.Where(mm => !mm.Defeated))
            {
                if (state.PlayerHp <= 0) break;
                ExecuteSquadMemberAttack(state, m, squad, mcs);
            }

            if (state.PlayerHp <= 0)
            {
                Narrator.Say("Trop nombreux. Tu tombes.", Color.Red);
                Narrator.Pause();
                var dominant = alive.OrderByDescending(m => m.Base.KillChance).First();
                return DetermineDefeat(dominant.Base);
            }

            state.Stamina = Math.Min(state.MaxStamina, state.Stamina + 15);
            AnsiConsole.WriteLine();
        }

        return squad.All(m => m.Defeated) ? CombatOutcome.Victory : CombatOutcome.Fled;
    }

    static List<Choice> BuildMultiChoices(GameState state, List<SquadMember> squad, List<SquadMember> alive, MultiCombatState mcs)
    {
        var choices = new List<Choice>();
        var weapon  = state.EquippedWeapon;

        // Finisher
        if (mcs.Momentum >= 3)
        {
            choices.Add(new("💥 [gold1 bold]FINISHER[/] — Cible l'ennemi le plus faible  [grey dim](3× dégâts)[/]", s =>
            {
                var target = alive.OrderBy(m => m.Hp).First();
                var dmg    = CalcDamage(s, weapon, false) * 3;
                target.Hp  = Math.Max(0, target.Hp - dmg);
                mcs.Momentum = 0;
                Narrator.Say($"FINISHER sur {target.Base.Name} — {dmg} dégâts !", Color.Gold1);
                if (target.Defeated) Display.ShowEvent($"{target.Base.Name} est éliminé !", Color.Gold1);
            }));
        }

        // Attaquer chaque ennemi vivant
        foreach (var m in alive)
        {
            var target  = m;
            var roleTag = target.Base.Role switch
            {
                EnemyRole.Tank    => " [grey](Tank 🛡)[/]",
                EnemyRole.Ranged  => " [grey](Tireur 🎯)[/]",
                EnemyRole.Support => " [grey](Soutien 💚)[/]",
                _                 => "",
            };
            var hpColor = target.Hp > target.Base.MaxHp * 0.5f ? "green" : target.Hp > target.Base.MaxHp * 0.25f ? "yellow" : "red";

            choices.Add(new(
                $"⚔ Attaquer [red]{target.Base.Name}[/]{roleTag}  [{hpColor}]{target.Hp}/{target.Base.MaxHp} PV[/]",
                s =>
                {
                    // Le Tank redirige 30% des dégâts des autres cibles si vivant et différent de la cible
                    var tank = alive.FirstOrDefault(mm => mm.Base.Role == EnemyRole.Tank && mm != target && !mm.Defeated);

                    var dmg = CalcDamage(s, weapon, false);

                    if (tank != null)
                    {
                        var absorbed = (int)(dmg * 0.3f);
                        dmg         -= absorbed;
                        tank.Hp      = Math.Max(0, tank.Hp - absorbed);
                        Display.ShowEvent($"{tank.Base.Name} (Tank) absorbe {absorbed} dégâts pour protéger {target.Base.Name}.", Color.SteelBlue1);
                        if (tank.Defeated) Display.ShowEvent($"{tank.Base.Name} s'effondre !", Color.Gold1);
                    }

                    target.Hp    = Math.Max(0, target.Hp - dmg);
                    mcs.Momentum = Math.Min(3, mcs.Momentum + 1);

                    Display.ShowEvent($"{dmg} dégâts sur {target.Base.Name}. ({target.Hp}/{target.Base.MaxHp} PV)", Color.Green);
                    if (target.Defeated) Display.ShowEvent($"{target.Base.Name} est éliminé !", Color.Gold1);
                    if (mcs.Momentum == 3) Display.ShowEvent("⚡ MOMENTUM MAX — Finisher dispo !", Color.Gold1);
                }));
        }

        // Action de classe (simplifiée en multi)
        if (!mcs.ClassUsed)
        {
            switch (state.Class.Name)
            {
                case "Médecin":
                    choices.Add(new("💊 [cyan1][Médecin][/] Soin rapide  [grey dim](+30 PV, -20 stamina)[/]", s =>
                    {
                        mcs.ClassUsed = true;
                        s.Stamina  = Math.Max(0, s.Stamina - 20);
                        s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 30);
                        Display.ShowEvent($"Soin rapide ! +{Math.Min(30, s.PlayerMaxHp - s.PlayerHp + 30)} PV.", Color.Green);
                    }, s => s.Stamina >= 20));
                    break;

                case "Contrebandier":
                    choices.Add(new("🏃 [cyan1][Contrebandier][/] Fuite garantie  [grey dim](-1 carburant)[/]", s =>
                    {
                        mcs.ClassUsed = true;
                        s.Fuel = Math.Max(0, s.Fuel - 1);
                        mcs.Fled = true;
                        Narrator.Say("Fuite garantie. -1 carburant.", Color.Yellow);
                    }, s => s.Fuel > 0));
                    break;
            }
        }

        // Fuir
        choices.Add(new("🏃 Fuir", s =>
        {
            var chance = 35 + (s.Class.Name == "Contrebandier" ? 25 : 0);
            if (Rng.Next(100) < chance)
            {
                s.Fuel = Math.Max(0, s.Fuel - 1);
                mcs.Fled = true;
                Narrator.Say("Tu te fais une ouverture et tu décampes. -1 carburant.", Color.Yellow);
            }
            else
                Narrator.Say("Impossible de fuir — ils encerclent la sortie.", Color.Red);
        }, s => s.Fuel > 0));

        // Soigner
        if (state.Cargo.Get("Médicaments") > 0)
        {
            choices.Add(new($"💊 Se soigner  [grey dim](stock : {state.Cargo.Get("Médicaments")})[/]", s =>
            {
                s.Cargo.Remove("Médicaments", 1);
                s.PlayerHp = Math.Min(s.PlayerMaxHp, s.PlayerHp + 30);
                Display.ShowEvent($"+30 PV. {s.PlayerHp}/{s.PlayerMaxHp}", Color.Green);
            }));
        }

        return choices;
    }

    static void ExecuteSquadMemberAttack(GameState state, SquadMember m, List<SquadMember> squad, MultiCombatState mcs)
    {
        if (m.StunTurns > 0) { m.StunTurns--; Display.ShowEvent($"{m.Base.Name} est étourdi.", Color.Yellow); return; }

        int dmg;

        switch (m.Base.Role)
        {
            case EnemyRole.Support:
                // Soigne l'allié le plus faible
                var weakest = squad.Where(s => !s.Defeated && s != m).OrderBy(s => s.Hp).FirstOrDefault();
                if (weakest != null)
                {
                    var heal = Rng.Next(15, 30);
                    weakest.Hp = Math.Min(weakest.Base.MaxHp, weakest.Hp + heal);
                    Display.ShowEvent($"{m.Base.Name} (Soutien) soigne {weakest.Base.Name} de {heal} PV.", Color.Green);
                }
                // Attaque faible
                dmg = Rng.Next(m.Base.DamageMin / 3, m.Base.DamageMax / 3 + 1);
                break;

            case EnemyRole.Ranged:
                // Tireur : attaque toujours, même partiellement étourdi
                dmg = Rng.Next(m.Base.DamageMin, m.Base.DamageMax + 1);
                break;

            case EnemyRole.Tank:
                // Tank : attaque lourde, peut étourdir
                dmg = (int)(Rng.Next(m.Base.DamageMin, m.Base.DamageMax + 1) * 1.2f);
                if (Rng.Next(100) < 20)
                {
                    Display.ShowEvent($"{m.Base.Name} (Tank) frappe fort — tu es déstabilisé !", Color.Red);
                    mcs.Momentum = 0;
                }
                break;

            default:
                dmg = Rng.Next(m.Base.DamageMin, m.Base.DamageMax + 1);
                break;
        }

        // Réduction armure
        var armor = state.EquippedArmor;
        if (armor != null && dmg > 0)
        {
            var red = (int)(dmg * armor.Defense / 100.0f);
            dmg -= red;
        }

        if (dmg > 0)
        {
            state.PlayerHp = Math.Max(0, state.PlayerHp - dmg);
            Display.ShowEvent($"{m.Base.Name} te frappe — {dmg} dégâts. PV : {state.PlayerHp}/{state.PlayerMaxHp}", Color.Red);
        }
    }

    static void ShowSquadRoles(List<SquadMember> squad)
    {
        AnsiConsole.MarkupLine("[grey]Composition de la menace :[/]");
        foreach (var m in squad)
        {
            var (roleLabel, roleColor) = m.Base.Role switch
            {
                EnemyRole.Tank    => ("Tank    🛡  — absorbe les dégâts pour ses alliés", "steelblue1"),
                EnemyRole.Ranged  => ("Tireur  🎯  — attaque à distance chaque tour",     "orange1"),
                EnemyRole.Support => ("Soutien 💚  — soigne ses alliés chaque tour",      "green"),
                _                 => ("Normal",                                            "grey"),
            };
            AnsiConsole.MarkupLine($"  [{roleColor}]{m.Base.Name}[/]  [grey dim]{roleLabel}[/]  {m.Hp} PV");
        }
    }

    static void ShowSquadStatus(GameState state, List<SquadMember> squad, int momentum)
    {
        var table = new Table().Border(TableBorder.Rounded).HideHeaders()
            .AddColumn("Toi").AddColumn("Ennemis");

        var pHp = state.PlayerHp > state.PlayerMaxHp * 0.5 ? "green" : state.PlayerHp > state.PlayerMaxHp * 0.25 ? "yellow" : "red";
        var momentumBar = momentum switch { 0 => "[grey]○○○[/]", 1 => "[yellow]●○○[/]", 2 => "[orange1]●●○[/]", _ => "[gold1 bold]●●● FINISHER[/]" };

        var enemyLines = string.Join("\n", squad.Select(m =>
        {
            if (m.Defeated) return $"  [grey dim]✗ {m.Base.Name}[/]";
            var hpColor = m.Hp > m.Base.MaxHp * 0.5f ? "green" : m.Hp > m.Base.MaxHp * 0.25f ? "yellow" : "red";
            var roleTag = m.Base.Role switch { EnemyRole.Tank => "🛡", EnemyRole.Ranged => "🎯", EnemyRole.Support => "💚", _ => "" };
            return $"  {roleTag} [red]{m.Base.Name}[/]  [{hpColor}]{m.Hp}/{m.Base.MaxHp}[/]";
        }));

        table.AddRow(
            $"[{pHp}]{state.PlayerHp}/{state.PlayerMaxHp} PV[/]\nMomentum : {momentumBar}",
            enemyLines);

        AnsiConsole.Write(table);
    }

    // GetForStation conservé pour compatibilité
    public static Enemy GetForStation(string stationName)
    {
        var pool = stationName switch
        {
            "La Carcasse" or "Les Bas-Fonds de Vega" or "Fort Kharos"
                or "Port des Brumes" or "Station Rocaille" or "Avant-Poste Kalem"
                or "Forge Alpha" or "La Citadelle" or "La Ferronnerie" or "Terminus Sud"
                or "Les Décombres de Vael" or "La Bulle" => TierLow,

            "Nexus Aldara" or "Fort Ossian" or "La Forge Noire" or "Le Purgatoire"
                or "L'Entrepôt Zéro" or "Colonie Perséphone" => TierMid,

            "Emporium Requiem" or "Arc Ouest Apocalypse" or "Le Nid des Faucons"
                or "Scotty Golden North" or "Star Quest" or "La Couronne d'Eos"
                or "La Citadelle Écarlate" or "Les Abysses de Velkor" => TierHigh,

            _ => TierMid,
        };
        return pool[Rng.Next(pool.Count)];
    }
}
