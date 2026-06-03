using Spectre.Console;

namespace VoidTrader;

record Enemy(
    string Name,
    int    MaxHp,
    int    DamageMin,
    int    DamageMax,
    int    LootMin,
    int    LootMax,
    string Description,
    int    CaptureChance = 20,
    int    KillChance    = 15
);

enum CombatOutcome { Victory, Stunned, Captured, Dead, Fled }

class CombatState
{
    public int  EnemyHp;
    public int  EnemyStunTurns;
    public int  EnemyBurnDmg;
    public int  EnemyBurnTurns;
    public bool EnemyBlinded;
    public bool PlayerFled;
    public bool ImmunityUsed;
}

static class Combat
{
    private static readonly Random Rng = new();

    // ── POOLS D'ENNEMIS ─────────────────────────────────────────────────────

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
        new("Élite des Faucons Noirs",   120, 20, 40, 1000, 2500, "Entraîné, équipé, motivé. Chaque coup est calculé.", CaptureChance: 15, KillChance: 25),
        new("Garde de l'Emporium",       130, 18, 38,  800, 2000, "Armure lourde, zéro humour. Il a vu pire que toi.", CaptureChance: 40, KillChance: 5),
        new("Assassin du Conclave",      100, 25, 48, 1500, 3500, "Tu ne l'as pas vu venir. Tu ne le verras pas repartir non plus.", KillChance: 35),
        new("Bras droit d'Alanossa",     140, 22, 42, 1200, 3000, "Il a survécu à des dizaines de combats. Ça se voit à chaque mouvement.", CaptureChance: 20, KillChance: 25),
        new("Soldat de Noctis",          115, 20, 38,  900, 2200, "Défend les intérêts du Directeur Pale. Fanatique.", KillChance: 20),
    ];

    public static readonly List<Enemy> TierBoss =
    [
        new("Alanossa",               200, 30, 55, 4000, 8000, "Le pirate le plus dangereux de l'univers connu. Tu dois vraiment être sûr de toi.", CaptureChance: 10, KillChance: 40),
        new("La Faucon",              180, 28, 52, 3500, 7000, "Cheffe des Faucons Noirs. Chaque mouvement calculé à l'avance.", CaptureChance: 15, KillChance: 35),
        new("Directeur Pale",         160, 25, 48, 3000, 6000, "Il a fait manger les mineurs moins cher que ses gardes. Il est calme. C'est pire.", CaptureChance: 30, KillChance: 25),
        new("Garde du Corps d'Eliotis",150, 22, 45, 2500, 5500, "Eliotis ne pardonne pas qu'on trouble sa fête.", CaptureChance: 5, KillChance: 45),
    ];

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

    // ── MOTEUR DE COMBAT ────────────────────────────────────────────────────

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
            // Effets sur la durée
            if (cs.EnemyBurnTurns > 0)
            {
                cs.EnemyHp = Math.Max(0, cs.EnemyHp - cs.EnemyBurnDmg);
                cs.EnemyBurnTurns--;
                Display.ShowEvent($"Effet actif — {cs.EnemyBurnDmg} dégâts. {enemy.Name} : {cs.EnemyHp}/{enemy.MaxHp} PV", Color.OrangeRed1);
                if (cs.EnemyHp <= 0) return ResolveVictory(state, enemy);
            }

            ShowCombatStatus(state, enemy, cs);

            var choices = BuildChoices(state, enemy, cs);
            var choice  = ChoiceMenu.Present(new Situation("Ton action :", choices), state);
            if (choice is null) continue;

            AnsiConsole.WriteLine();
            choice.Effect?.Invoke(state);

            if (cs.PlayerFled)    return CombatOutcome.Fled;
            if (cs.EnemyHp <= 0)  return ResolveVictory(state, enemy);
            if (state.IsImprisoned) return CombatOutcome.Captured;

            // Tour ennemi
            if (cs.EnemyStunTurns > 0)
            {
                cs.EnemyStunTurns--;
                Display.ShowEvent($"{enemy.Name} est étourdi — perd son tour.", Color.Yellow);
            }
            else
            {
                var enemyDmg = Rng.Next(enemy.DamageMin, enemy.DamageMax + 1);
                if (cs.EnemyBlinded) { enemyDmg = (int)(enemyDmg * 0.6f); cs.EnemyBlinded = false; }
                var isCrit = Rng.Next(100) < 10;
                if (isCrit) enemyDmg = (int)(enemyDmg * 1.8f);

                // Réduction par armure
                var armor = state.EquippedArmor;
                if (armor != null)
                {
                    var reduced = (int)(enemyDmg * armor.Defense / 100.0f);
                    enemyDmg   -= reduced;
                    if (reduced > 0)
                        Display.ShowEvent($"[steelblue1]Armure absorbe {reduced} dégâts.[/]", Color.SteelBlue1);

                    // Thorns
                    if (armor.Effect == ArmorEffect.Thorns)
                    {
                        var thornDmg = (int)(enemyDmg * armor.EffectValue / 100.0f);
                        if (thornDmg > 0)
                        {
                            cs.EnemyHp = Math.Max(0, cs.EnemyHp - thornDmg);
                            Display.ShowEvent($"Épines — {thornDmg} dégâts renvoyés à {enemy.Name}.", Color.OrangeRed1);
                        }
                    }

                    // Immunity (absorbe une mort)
                    if (armor.Effect == ArmorEffect.Immunity && cs.ImmunityUsed == false && state.PlayerHp <= enemyDmg)
                    {
                        cs.ImmunityUsed = true;
                        enemyDmg = 0;
                        Display.ShowEvent("L'Armure du Vide absorbe le coup fatal. Une seule fois.", Color.Gold1);
                    }
                }

                state.PlayerHp = Math.Max(0, state.PlayerHp - enemyDmg);
                Display.ShowEvent($"{enemy.Name} attaque{(isCrit ? " [bold](CRITIQUE)[/]" : "")} — {enemyDmg} dégâts. Tes PV : {state.PlayerHp}/{state.PlayerMaxHp}", Color.Red);

                if (state.PlayerHp <= 0)
                {
                    Narrator.Say($"Tu tombes. {enemy.Name} se penche sur toi...", Color.Red);
                    Narrator.Pause();
                    return DetermineDefeat(enemy);
                }
            }

            // Régénération armure
            if (state.EquippedArmor?.Effect == ArmorEffect.Regen)
            {
                var regen = state.EquippedArmor.EffectValue;
                state.PlayerHp = Math.Min(state.PlayerMaxHp, state.PlayerHp + regen);
                if (regen > 0) Display.ShowEvent($"Régénération armure : +{regen} PV.", Color.Green);
            }

            state.Stamina = Math.Min(state.MaxStamina,
                state.Stamina + 20 + (state.EquippedArmor?.Effect == ArmorEffect.StaminaBoost ? state.EquippedArmor.EffectValue : 0));
            AnsiConsole.WriteLine();
        }
    }

    static List<Choice> BuildChoices(GameState state, Enemy enemy, CombatState cs)
    {
        var weapon  = state.EquippedWeapon;
        var choices = new List<Choice>();

        // ── ATTAQUE DE BASE ──
        var atkLabel = weapon != null
            ? $"⚔  Attaquer avec {weapon.Name}  [grey dim](-20 stamina)[/]"
            : $"👊 Combat à mains nues  [grey dim](-20 stamina)[/]";

        choices.Add(new(atkLabel, s =>
        {
            if (s.Stamina <= 0)
            {
                Narrator.Say("Tu es épuisé. Attaque faible.", Color.Yellow);
                var weakDmg = Rng.Next(3, 10);
                cs.EnemyHp  = Math.Max(0, cs.EnemyHp - weakDmg);
                Display.ShowEvent($"Attaque épuisée — {weakDmg} dégâts.", Color.Yellow);
            }
            else
            {
                s.Stamina -= 20;
                var dmg = CalcDamage(s, weapon, false);
                cs.EnemyHp = Math.Max(0, cs.EnemyHp - dmg);
                Display.ShowEvent($"Tu infliges {dmg} dégâts. {enemy.Name} : {cs.EnemyHp}/{enemy.MaxHp} PV", Color.Green);
                ApplySelfDamage(s, weapon);
            }
        }));

        // ── ATTAQUE SPÉCIALE ──
        if (weapon != null && weapon.Effect is not (WeaponEffect.None or WeaponEffect.Silence or WeaponEffect.ArmorPierce) && state.Stamina >= 35)
        {
            choices.Add(new($"✨ Attaque spéciale — {weapon.EffectDesc}  [grey dim](-35 stamina)[/]", s =>
            {
                s.Stamina -= 35;
                var dmg = CalcDamage(s, weapon, true);
                cs.EnemyHp = Math.Max(0, cs.EnemyHp - dmg);
                Display.ShowEvent($"Attaque spéciale ! {dmg} dégâts.", Color.Gold1);
                if (Rng.Next(100) < weapon.EffectChance)
                    ApplyEffect(weapon.Effect, cs, s, dmg, enemy.Name);
                else
                    Display.ShowEvent("L'effet spécial ne s'est pas déclenché.", Color.Grey);
                ApplySelfDamage(s, weapon);
            }));
        }

        // ── FRAPPE CONCENTRÉE ──
        if (state.Stamina >= 50)
        {
            choices.Add(new("🎯 Frappe concentrée  [grey dim](-50 stamina, +60% dégâts)[/]", s =>
            {
                s.Stamina -= 50;
                var dmg = (int)(CalcDamage(s, weapon, false) * 1.6f);
                cs.EnemyHp = Math.Max(0, cs.EnemyHp - dmg);
                Display.ShowEvent($"Frappe concentrée — {dmg} dégâts !", Color.Gold1);
            }));
        }

        // ── FUIR ──
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

        // ── NÉGOCIER ──
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

        // ── INTIMIDER ──
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

        // ── SE SOIGNER ──
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

    // ── HELPERS ─────────────────────────────────────────────────────────────

    static void ApplySelfDamage(GameState state, WeaponData? weapon)
    {
        if (weapon is null || weapon.SelfDmgChance == 0) return;
        if (Rng.Next(100) >= weapon.SelfDmgChance) return;

        var selfDmg = weapon.SelfDmgMax > 0
            ? Rng.Next(weapon.SelfDmgMax / 2, weapon.SelfDmgMax + 1)
            : 0;

        var message = weapon.Name switch
        {
            "Lance-flammes compact"        => $"Les flammes te lèchent au passage. -{selfDmg} PV joueur.",
            "Fusil à pompe galactique"     => $"Le recul t'arrache l'épaule. -{selfDmg} PV joueur.",
            "Gatling à plasma Bonne Nuit"  => $"Une balle ricoche et te touche. C'est pas la première fois que ça arrive avec ce truc. -{selfDmg} PV joueur.",
            "Canon à trou noir miniaturisé"=> $"Le champ gravitationnel te compresse brièvement les os. Douloureux. -{selfDmg} PV joueur.",
            "Canon à singularité"          => $"La singularité aspire aussi dans ta direction. -{selfDmg} PV joueur. Tu la fermes et tu continues.",
            "Dernière Parole"              => $"La détonation t'arrache l'audition temporairement et quelques PV. -{selfDmg} PV joueur.",
            "Le Sceptre de Raphazarus"     => $"L'effet aléatoire se retourne partiellement contre toi. Personne est surpris. -{selfDmg} PV joueur.",
            "Bombe à paradoxe"             => $"La bombe explose dans toutes les directions, y compris la tienne. -{selfDmg} PV joueur. Ça fait partie du plan... probablement.",
            "L'Insistance"                 => $"Tirs infinis, mais quelques balles dévient. -{selfDmg} PV joueur.",
            _                              => $"{weapon.Name} te blesse dans le processus. -{selfDmg} PV joueur.",
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

            if (weapon.Effect == WeaponEffect.ArmorPierce)
                baseDmg = (int)(baseDmg * 1.3f);

            var critChance = special ? weapon.CritChance + 10 : weapon.CritChance;
            if (Rng.Next(100) < critChance)
            {
                baseDmg = (int)(baseDmg * 2.0f);
                Display.ShowEvent("[bold yellow]COUP CRITIQUE ![/]", Color.Gold1);
            }
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
            if (Rng.Next(100) < 10)
            {
                baseDmg = (int)(baseDmg * 2.0f);
                Display.ShowEvent("[bold yellow]COUP CRITIQUE ![/]", Color.Gold1);
            }
        }

        return Math.Max(1, baseDmg);
    }

    static CombatOutcome ResolveVictory(GameState state, Enemy enemy)
    {
        var loot = Rng.Next(enemy.LootMin, enemy.LootMax + 1);
        state.Credits    += loot;
        state.Reputation += 10;

        // Détecte si c'était un boss — butin légendaire garanti
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

    static void ShowThreatWarning(GameState state, Enemy enemy)
    {
        // Évalue le niveau de menace relatif au joueur
        var playerPower = state.PlayerHp
            + (state.EquippedWeapon?.DamageMax ?? 15)
            + state.Stamina / 2;

        var enemyPower = enemy.MaxHp + enemy.DamageMax * 3;

        var ratio = (float)enemyPower / playerPower;

        if (ratio >= 3.0f)
        {
            AnsiConsole.MarkupLine("[red bold]⚠ DANGER EXTRÊME — Ce combat est probablement impossible. Fuis ou négocie.[/]");
        }
        else if (ratio >= 2.0f)
        {
            AnsiConsole.MarkupLine("[red]⚠ ADVERSAIRE REDOUTABLE — La négociation ou la fuite sont des options sérieuses.[/]");
        }
        else if (ratio >= 1.4f)
        {
            AnsiConsole.MarkupLine("[yellow]⚡ ADVERSAIRE DIFFICILE — Sois stratégique. Les attaques spéciales peuvent aider.[/]");
        }
        // En dessous de 1.4 : rien, combat équilibré ou favorable
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
            AnsiConsole.MarkupLine($"  [red]⚠ Risque self-damage :[/] {w.SelfDmgChance}% de chance de te blesser ({w.SelfDmgMax / 2}–{w.SelfDmgMax} dégâts)");
        AnsiConsole.MarkupLine($"  [grey dim]Utilisation :[/] [yellow]Équiper depuis 'Gérer les armes'[/]   [grey dim]Valeur revente :[/] ~{w.Tier * 300 + w.DamageMax * 2}cr");
        AnsiConsole.WriteLine();
    }

    static void ShowCombatStatus(GameState state, Enemy enemy, CombatState cs)
    {
        var pHp = state.PlayerHp > state.PlayerMaxHp * 0.5 ? "green" : state.PlayerHp > state.PlayerMaxHp * 0.25 ? "yellow" : "red";
        var eHp = cs.EnemyHp > enemy.MaxHp * 0.5 ? "green" : cs.EnemyHp > enemy.MaxHp * 0.25 ? "yellow" : "red";
        var sta = state.Stamina > state.MaxStamina * 0.5 ? "cyan1" : state.Stamina > state.MaxStamina * 0.25 ? "yellow" : "red";

        var wName     = state.EquippedWeapon != null ? $"[orange1]{state.EquippedWeapon.Name}[/]" : "[grey]mains nues[/]";
        var armorName = state.EquippedArmor  != null ? $"  [steelblue1]🛡 {state.EquippedArmor.Name} (-{state.EquippedArmor.Defense}% dmg)[/]" : "";
        var eStatus   = cs.EnemyStunTurns > 0 ? " [yellow](étourdi)[/]" : cs.EnemyBlinded ? " [yellow](aveuglé)[/]" : "";

        var table = new Table().Border(TableBorder.Rounded).HideHeaders().AddColumn("").AddColumn("");
        table.AddRow(
            $"[white]Toi[/]  [{pHp}]{state.PlayerHp}/{state.PlayerMaxHp} PV[/]  [{sta}]Stamina {state.Stamina}/{state.MaxStamina}[/]  {wName}{armorName}",
            $"[red]{enemy.Name}[/]{eStatus}  [{eHp}]{cs.EnemyHp}/{enemy.MaxHp} PV[/]"
        );
        AnsiConsole.Write(table);
    }
}
