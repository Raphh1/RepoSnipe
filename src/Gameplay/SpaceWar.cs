using Spectre.Console;

namespace VoidTrader;

// Guerre spatiale — invasion du vaisseau pendant un voyage.
// Probabilité basée sur le danger de la destination + statut recherché.
static class SpaceWar
{
    private static readonly Random Rng = new();

    public static void MaybeTriggerBoarding(GameState state, Station destination)
    {
        if (state.ShipHp <= 0 || state.IsDead) return;

        var danger = Universe.Danger(destination);

        // Base : 5% par niveau de danger au-delà de 1, plus une grosse prime si recherché
        var boardingChance = (danger - 1) * 5;
        if (state.Reputation <= -300) boardingChance += 15;
        if (state.Reputation <= -600) boardingChance += 15;
        if (state.Class.PiratesDoubled) boardingChance += 12;

        if (Rng.Next(100) >= boardingChance) return;

        // Type d'invasion selon la zone
        var boarders = danger >= 4
            ? new[] { "un commando de pillards de l'espace", "une escouade de mercenaires", "des pirates de l'Apocalypse" }
            : new[] { "des contrebandiers désespérés", "un groupe de raiders", "des pirates opportunistes" };

        var who = boarders[Rng.Next(boarders.Length)];

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red bold]INVASION[/]").RuleStyle("red"));
        Narrator.Say($"Alarme. {who.ToUpper()} ont forcé le sas d'amarrage. Ils sont dans le vaisseau.", Color.Red);

        ChoiceMenu.Resolve(BuildBoardingMenu(state, who, danger), state);
    }

    static Situation BuildBoardingMenu(GameState state, string who, int danger)
    {
        var waveCount = danger >= 4 ? 3 : 2;

        return new Situation($"Ton vaisseau est envahi par {who}. Combien de temps tu tiens ?",
        [
            new("Se battre — repousser la vague", s =>
            {
                BoardingWaveFight(s, danger, waveCount);
            }),

            new("Ouvrir les vannes d'atmosphère — les vider dans le vide", s =>
            {
                if (Rng.Next(100) < 55)
                {
                    var dmgShip = Rng.Next(15, 35);
                    s.ShipHp = Math.Max(1, s.ShipHp - dmgShip);
                    s.Reputation -= 10;
                    Narrator.Say($"Ça fonctionne. Le couloir se vide. -{dmgShip} PV vaisseau (décompression interne). -10 rép.", Color.OrangeRed1);
                    Narrator.Pause();
                }
                else
                {
                    Narrator.Say("Le sas est bloqué. Ça n'a pas fonctionné. Ils avancent.", Color.Red);
                    BoardingWaveFight(s, danger, waveCount);
                }
            }),

            new("Lancer un explosif dans le couloir", s =>
            {
                if (s.Cargo.Get("Explosifs") > 0)
                {
                    s.Cargo.Remove("Explosifs", 1);
                    var dmgShip = Rng.Next(20, 45);
                    s.ShipHp = Math.Max(1, s.ShipHp - dmgShip);
                    Narrator.Say($"L'explosion résonne dans tout le vaisseau. Ils reculent. -{dmgShip} PV vaisseau. -1 Explosif.", Color.OrangeRed1);
                    if (Rng.Next(100) < 65)
                    {
                        var loot = Rng.Next(400, 1200); s.Credits += loot;
                        s.Reputation += 10;
                        Narrator.Say($"Quelques survivants fuient en laissant leur butin. +{loot}cr. +10 rép.", Color.Gold1);
                    }
                    else
                        Narrator.Say("Les survivants se regroupent. Ils sont encore là.", Color.Red);
                    Narrator.Pause();
                }
                else
                {
                    Narrator.Say("T'as pas d'explosifs. T'essaies quand même. C'est surtout du bruit.", Color.Grey);
                    BoardingWaveFight(s, danger, waveCount);
                }
            }, s => true),

            new("Négocier — leur offrir un passage et une part de cargaison", s =>
            {
                if (s.Cargo.All.Any())
                {
                    var item = s.Cargo.All.Keys.First();
                    s.Cargo.Remove(item, Math.Min(2, s.Cargo.Get(item)));
                    s.Reputation -= 8;
                    Narrator.Say($"Ils prennent. Ils partent. -cargaison. -8 rép. Tu gardes ta vie.", Color.Yellow);
                }
                else
                {
                    var vol = Rng.Next(400, 1500); s.Credits = Math.Max(0, s.Credits - vol);
                    Narrator.Say($"T'as rien à donner. Ils prennent directement dans tes crédits. -{vol}cr.", Color.Red);
                }
                Narrator.Pause();
            }),

            new("Piéger le couloir et fuir vers le cockpit", s =>
            {
                if (Rng.Next(100) < 50)
                {
                    var dmgShip = Rng.Next(10, 30); s.ShipHp = Math.Max(1, s.ShipHp - dmgShip);
                    s.Fuel = Math.Max(0, s.Fuel - 1);
                    Narrator.Say($"Tu t'enfermes dans le cockpit, tu accélères. Ils abandonnent. -{dmgShip} PV vaisseau, -1 carburant.", Color.Yellow);
                    Narrator.Pause();
                }
                else
                {
                    Narrator.Say("Ils ont anticipé. Ils bloquent le cockpit. Pas le choix.", Color.Red);
                    BoardingWaveFight(s, danger, waveCount);
                }
            }, s => s.Fuel > 0),
        ], Color.Red);
    }

    static void BoardingWaveFight(GameState state, int danger, int waveCount)
    {
        var totalLoot = 0;

        for (int wave = 1; wave <= waveCount; wave++)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]── VAGUE {wave}/{waveCount} ──[/]");

            var e = Combat.Scale(Combat.GetScaled(state, wave - 1), Math.Max(0, danger - 2));
            var outcome = Combat.Start(state, e);

            if (outcome == CombatOutcome.Victory)
            {
                var loot = Rng.Next(300, 900);
                totalLoot += loot;
                state.Credits += loot;
                Display.ShowEvent($"Vague {wave} repoussée. +{loot}cr sur les envahisseurs.", Color.Gold1);

                if (wave < waveCount)
                {
                    AnsiConsole.MarkupLine("[grey]Ils se regroupent...[/]");
                    Thread.Sleep(600);
                }
            }
            else
            {
                // Défaite mid-vague : issues différentes de la fin de run normale
                Situations.ApplyCombatOutcome(state, outcome);
                if (state.IsDead) return;

                // Survivant : on prend tes affaires et on repart
                var vol = Rng.Next(300, 1200);
                state.Credits = Math.Max(0, state.Credits - vol);
                Narrator.Say($"Ils ont pris {vol}cr et repris le sas. Tu reprends conscience dans ton cockpit.", Color.Red);
                Narrator.Pause();
                return;
            }
        }

        if (totalLoot > 0)
        {
            state.Reputation += 15;
            Narrator.Say($"Toutes les vagues repoussées. Total récupéré : {totalLoot}cr. +15 réputation.", Color.Gold1);
        }
        Narrator.Pause();
    }
}
