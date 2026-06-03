using Spectre.Console;

namespace VoidTrader;

record SpaceEnemy(
    string Name,
    int    MaxHp,
    int    DamageMin,
    int    DamageMax,
    int    LootMin,
    int    LootMax,
    string Description,
    bool   CanBeBoarded = true,
    int    KillChance   = 10
);

static class SpaceCombat
{
    private static readonly Random Rng = new();

    public static readonly List<SpaceEnemy> TierLow =
    [
        new("Chasseur pirate rouillé",      40,  6, 14, 100,  400, "Un vieux rafiot armé d'un seul canon. Il compense par l'audace."),
        new("Drone de surveillance armé",   30,  4, 10,  80,  300, "Automatisé, donc stupide. Dangereux quand même.", CanBeBoarded: false),
        new("Petit contrebandier paniqué",  35,  5, 12, 120,  450, "Il voulait juste passer. Maintenant il tire."),
        new("Scavenger opportuniste",       38,  6, 13, 100,  380, "Il récupère tout ce qui traîne, y compris tes débris."),
    ];

    public static readonly List<SpaceEnemy> TierMid =
    [
        new("Corvette pirate",              70, 12, 22, 300,  800, "Rapide, bien armé. C'est son terrain.", KillChance: 15),
        new("Patrouille mercenaire",        75, 11, 20, 350,  900, "Payée pour contrôler ce secteur. Tu n'as pas de laissez-passer."),
        new("Chasseur des Faucons Noirs",   80, 13, 24, 400, 1000, "Emblème de la Faucon sur la carlingue. Entraîné.", KillChance: 20),
        new("Vaisseau de guerre mineur",    85, 14, 25, 450, 1100, "Trop lourd pour manœuvrer, trop armé pour ignorer."),
    ];

    public static readonly List<SpaceEnemy> TierHigh =
    [
        new("Destroyer de l'Emporium",     120, 18, 35, 700, 2000, "Le symbole de la puissance de l'Emporium Requiem. Impeccable.", KillChance: 25),
        new("Frégate des Faucons Noirs",   130, 20, 38, 800, 2200, "Plusieurs canons, un équipage entraîné, aucune pitié.", KillChance: 30),
        new("Vaisseau fantôme armé",       110, 16, 32, 600, 1800, "Il dérivait. Il ne dérive plus.", KillChance: 20),
        new("Chasseur de primes galactique",115,17, 33, 650, 1900, "Quelqu'un a mis ta tête à prix. Ce quelqu'un a payé.", KillChance: 25),
    ];

    public static readonly List<SpaceEnemy> TierBoss =
    [
        new("Le Vaisseau d'Alanossa",      200, 25, 45, 2000, 5000, "Alanossa aux commandes. La légende en personne.", CanBeBoarded: false, KillChance: 35),
        new("Cuirassé de l'Emporium",      180, 22, 40, 1500, 4000, "Le plus gros vaisseau militaire de l'empire.", CanBeBoarded: false, KillChance: 30),
        new("La Faucon — vaisseau amiral", 190, 24, 42, 1800, 4500, "Son vaisseau amiral. Une forteresse volante.", CanBeBoarded: false, KillChance: 35),
    ];

    public static SpaceEnemy GetForStation(string stationName)
    {
        var pool = stationName switch
        {
            "La Carcasse" or "Les Bas-Fonds de Vega" or "Fort Kharos"
                or "Port des Brumes" or "Station Rocaille" or "Avant-Poste Kalem"
                or "Forge Alpha" or "La Citadelle" or "La Ferronnerie" or "Terminus Sud"
                or "Les Décombres de Vael" or "La Bulle" or "La Colonie Errante"
                => TierLow,

            "Nexus Aldara" or "Fort Ossian" or "La Forge Noire" or "Le Purgatoire"
                or "L'Entrepôt Zéro" or "Colonie Perséphone" or "La Forge des Damnés"
                or "Le Phare de Vorn" or "L'Arche de Sélène"
                => TierMid,

            "Emporium Requiem" or "Arc Ouest Apocalypse" or "Le Nid des Faucons"
                or "Scotty Golden North" or "La Couronne d'Eos" or "La Citadelle Écarlate"
                or "Les Abysses de Velkor" or "Station Terminus Noir" or "L'Observatoire"
                => TierHigh,

            _ => TierMid,
        };
        return pool[Rng.Next(pool.Count)];
    }

    // ── COMBAT SPATIAL ──────────────────────────────────────────────────────

    public static CombatOutcome Start(GameState state, SpaceEnemy enemy)
    {
        var enemyHp  = enemy.MaxHp;
        var dodging  = false; // Manœuvre active ce tour
        var combatEnd = CombatOutcome.Victory; // pour les actions qui terminent le combat

        AnsiConsole.WriteLine();

        while (true)
        {
            ShowStatus(state, enemy, enemyHp);

            var choices = new List<Choice>
            {
                new("🔥 Tirer",
                    s =>
                    {
                        var dmg    = PlayerShipDamage(s);
                        enemyHp    = Math.Max(0, enemyHp - dmg);
                        var isCrit = dmg > 30;
                        Display.ShowEvent($"Tu tires{(isCrit ? " [bold](CRITIQUE !)[/]" : "")} — {dmg} dégâts. {enemy.Name} : {enemyHp}/{enemy.MaxHp} PV", Color.Green);
                    }),
                new("🌀 Manœuvrer (esquive le prochain tir)",
                    s => { dodging = true; Display.ShowEvent("Manœuvre d'esquive engagée. Prochain tir réduit de 60%.", Color.Cyan1); }),
                new("⚓ Aborder le vaisseau ennemi",
                    s =>
                    {
                        Narrator.Say("Tu t'amarres de force à la coque ennemie...", Color.OrangeRed1);
                        var crew    = new Enemy($"Équipage de {enemy.Name}", enemy.MaxHp / 2, Math.Max(1, enemy.DamageMin - 3), Math.Max(2, enemy.DamageMax - 5), enemy.LootMin, enemy.LootMax, "Combat dans les couloirs étroits.");
                        var outcome = Combat.Start(s, crew);
                        if (outcome == CombatOutcome.Victory) { enemyHp = 0; Display.ShowEvent("Équipage neutralisé. Vaisseau capturé !", Color.Gold1); }
                        else { Situations.ApplyCombatOutcome(s, outcome); combatEnd = outcome; }
                    },
                    s => enemy.CanBeBoarded && enemyHp < enemy.MaxHp * 0.4),
                new("🏃 Fuir en urgence",
                    s =>
                    {
                        var ok = Rng.Next(100) < 50 + (s.Fuel > 3 ? 20 : 0);
                        if (ok) { s.Fuel = Math.Max(0, s.Fuel - 1); Narrator.Say("Tu t'éloignes à pleine puissance. -1 carburant.", Color.Yellow); combatEnd = CombatOutcome.Fled; }
                        else Narrator.Say("Il te rattrape. Impossible de fuir.", Color.Red);
                    },
                    s => s.Fuel > 0),
                new("📡 Négocier une trêve",
                    s =>
                    {
                        var ok = Rng.Next(100) < 20 + Math.Max(0, s.Reputation / 8);
                        if (ok) { Narrator.Say("Trêve acceptée. Il repart sans insister.", Color.Green); s.Reputation += 5; combatEnd = CombatOutcome.Victory; }
                        else Narrator.Say("Il rejette la proposition et tire.", Color.Red);
                    },
                    s => s.Reputation > -200),
            };

            var choice = ChoiceMenu.Present(new Situation("Action vaisseau :", choices), state);
            if (choice is null) continue;

            AnsiConsole.WriteLine();

            // Actions qui terminent le combat prématurément
            if (combatEnd == CombatOutcome.Fled || (combatEnd == CombatOutcome.Victory && choice.Label.Contains("Négocier")))
                return combatEnd;
            if (combatEnd != CombatOutcome.Victory || choice.Label.Contains("Aborder"))
                combatEnd = CombatOutcome.Victory; // reset pour le tour prochain

            // Victoire
            if (enemyHp <= 0)
            {
                var loot = Rng.Next(enemy.LootMin, enemy.LootMax + 1);
                state.Credits    += loot;
                state.Reputation += 12;
                Narrator.Say($"{enemy.Name} explose dans le vide. +{loot}cr, +12 réputation.", Color.Gold1);
                Narrator.Pause();
                return CombatOutcome.Victory;
            }

            // Riposte ennemie
            var eDmg = Rng.Next(enemy.DamageMin, enemy.DamageMax + 1);
            if (dodging) { eDmg = (int)(eDmg * 0.4); dodging = false; }
            var eCrit = Rng.Next(100) < 8;
            if (eCrit) eDmg = (int)(eDmg * 1.8);

            state.ShipHp = Math.Max(0, state.ShipHp - eDmg);
            Display.ShowEvent($"{enemy.Name} tire{(eCrit ? " [bold](CRITIQUE !)[/]" : "")} — {eDmg} dégâts vaisseau. Tes PV : {state.ShipHp}/{state.ShipMaxHp}", Color.Red);

            if (state.ShipHp <= 0)
            {
                Narrator.Say("Ton vaisseau est détruit.", Color.Red);
                Narrator.Pause();
                return Rng.Next(100) < enemy.KillChance ? CombatOutcome.Dead : CombatOutcome.Captured;
            }

            AnsiConsole.WriteLine();
        }
    }

    static int PlayerShipDamage(GameState state)
    {
        var dmg  = Rng.Next(12, 28);
        var crit = Rng.Next(100) < 10;
        if (crit) { dmg = (int)(dmg * 2.2); Display.ShowEvent("[bold yellow]TIR CRITIQUE ![/]", Color.Gold1); }
        dmg += state.Class.Name switch
        {
            "Seigneur de guerre" => Rng.Next(5, 18),
            "Vétéran"            => Rng.Next(4, 12),
            "Mécanicien"         => Rng.Next(2, 8),
            "Vagabond"           => -Rng.Next(0, 6),
            _                    => 0,
        };
        return Math.Max(1, dmg);
    }

    static void ShowStatus(GameState state, SpaceEnemy enemy, int enemyHp)
    {
        var myColor = state.ShipHp > state.ShipMaxHp * 0.5 ? "green" : state.ShipHp > state.ShipMaxHp * 0.25 ? "yellow" : "red";
        var enColor = enemyHp > enemy.MaxHp * 0.5 ? "green" : enemyHp > enemy.MaxHp * 0.25 ? "yellow" : "red";

        var table = new Table().Border(TableBorder.Rounded).HideHeaders()
            .AddColumn("").AddColumn("");
        table.AddRow(
            $"[white]Ton vaisseau[/]  [{myColor}]{state.ShipHp}/{state.ShipMaxHp} PV[/]",
            $"[red]{enemy.Name}[/]  [{enColor}]{enemyHp}/{enemy.MaxHp} PV[/]"
        );
        AnsiConsole.Write(table);
    }
}
