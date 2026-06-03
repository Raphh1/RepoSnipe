using Spectre.Console;

namespace VoidTrader;

/// <summary>
/// Arc principal — La Station Nexus, détruite pendant la Grande Guerre.
/// Ses 4 fragments ont été récupérés par 4 chefs de station.
/// Rallier (ou écraser) tous les chefs = reconstituer la Nexus = Seigneur de l'Espace.
///
/// Les 4 fragments et leurs gardiens :
///   Fragment A — Arc Ouest Apocalypse   → Alanossa
///   Fragment B — L'Arc du Pic de l'Est  → Ramaster
///   Fragment C — L'Arc Perdu            → Raphazarus
///   Fragment D — Nexus Aldara           → Directrice Aldara
/// </summary>
static class StationNexus
{
    private static readonly Random Rng = new();

    const string FragA = "Arc Ouest Apocalypse";
    const string FragB = "L'Arc du Pic de l'Est";
    const string FragC = "L'Arc Perdu";
    const string FragD = "Nexus Aldara";

    public static readonly string[] FragmentStations = [FragA, FragB, FragC, FragD];

    // ── LORE INTRO ─────────────────────────────────────────────────────────

    public static void ShowNexusLore(GameState state)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[gold1]LA STATION NEXUS[/]").RuleStyle("gold1"));
        Narrator.Say(
            "Avant la Grande Guerre, la Station Nexus était le carrefour de l'espace connu. " +
            "Une construction colossale, hub politique, commercial et militaire. " +
            "La guerre l'a brisée en quatre fragments — chacun récupéré par un chef de station différent. " +
            "Personne n'a jamais voulu les réunir. Jusqu'à maintenant peut-être.",
            Color.Gold1);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [grey]Fragments récupérés :[/]");
        foreach (var station in FragmentStations)
        {
            var done = state.RalliedStations.Contains(station);
            AnsiConsole.MarkupLine($"  {(done ? "[green]✔[/]" : "[grey]○[/]")} {station}");
        }
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule().RuleStyle("gold1"));
        Narrator.Pause();
    }

    // ── FRAGMENT A — ALANOSSA (Arc Ouest Apocalypse) ────────────────────

    public static void ResolveFragmentAlanossa(GameState state)
    {
        if (state.RalliedStations.Contains(FragA))
        {
            Narrator.Say("Alanossa a déjà accepté. Le fragment A de la Nexus est sécurisé.", Color.Gold1);
            Narrator.Pause();
            return;
        }

        state.NpcsMet.Add("Alanossa");
        Narrator.Say("Alanossa t'écoute depuis son trône de débris. Ses yeux évaluent chaque mot.", Color.OrangeRed1);
        AnsiConsole.MarkupLine($"  [grey]Réputation actuelle :[/] [yellow]{state.Reputation}[/]");
        AnsiConsole.WriteLine();

        ChoiceMenu.Resolve(new Situation("Alanossa détient le Fragment A de la Nexus. Comment tu l'approches ?",
        [
            new("Parler de la Nexus — lui proposer un rôle de chef dans la reconstruction", s =>
            {
                if (s.Reputation < 50)
                {
                    Narrator.Say("Elle rit. 'Avec ta réputation, tu veux me proposer un partenariat ? Reviens quand les gens te connaissent.' Réputation insuffisante (50 requis).", Color.Red);
                    Narrator.Pause();
                    return;
                }
                switch (Rng.Next(3))
                {
                    case 0:
                        s.RalliedStations.Add(FragA);
                        s.StationPiecesRallied++;
                        s.Reputation += 80;
                        Narrator.Say("Elle réfléchit longtemps. 'Un siège de pouvoir dans la Nexus reconstituée. Tu as du culot. J'accepte.' Fragment A sécurisé. +80 réputation.", Color.Gold1);
                        break;
                    case 1:
                        s.Reputation -= 20;
                        Narrator.Say("'Intéressant. Mais pas maintenant. Prouve-moi d'abord que t'es sérieux.' Elle te chasse. -20 rép. Reviens avec plus de préparation.", Color.OrangeRed1);
                        break;
                    case 2:
                        s.Reputation += 30;
                        var cr = Rng.Next(2000, 5000); s.Credits -= Math.Min(s.Credits, cr);
                        Narrator.Say($"Elle négocie. Durement. Finalement, un accord préliminaire : -{cr}cr, +30 rép. Elle veut te revoir.", Color.Yellow);
                        break;
                }
                Narrator.Pause();
            }, s => s.Reputation >= 50),

            new("Lui faire une démonstration de force — l'impressionner en combat", s =>
            {
                Narrator.Say("Elle se lève. Ses gardes reculent. 'Bien. Montrons.'", Color.Red);
                var boss = new Enemy("Alanossa", 220, 32, 58, 5000, 10000,
                    "La pirate la plus dangereuse de l'espace connu. Elle n'a jamais perdu.", CaptureChance: 5, KillChance: 40, IsBoss: true);
                var outcome = Combat.Start(s, boss);

                if (outcome == CombatOutcome.Victory)
                {
                    s.RalliedStations.Add(FragA);
                    s.StationPiecesRallied++;
                    s.BossesDefeated++;
                    s.StationBossesBeaten.Add(FragA);
                    s.Reputation += 150;
                    Narrator.Say("Elle est à terre. Elle rit. 'Impressionnant. La Nexus mérite un chef comme toi.' Fragment A sécurisé par la force. +150 réputation.", Color.Gold1);
                }
                else
                {
                    Situations.ApplyCombatOutcome(s, outcome);
                }
                Narrator.Pause();
            }),

            new("Lui offrir une alliance militaire formelle — signature de pacte", s =>
            {
                if (s.Credits < 8000)
                {
                    Narrator.Say("'Un pacte sans ressources ? C'est du papier.' Elle te chasse.", Color.Red);
                    Narrator.Pause();
                    return;
                }
                s.Credits -= 8000;
                s.RalliedStations.Add(FragA);
                s.StationPiecesRallied++;
                s.Reputation += 60;
                s.FactionMissions++;
                Narrator.Say("Le pacte est signé. -8 000cr. +60 réputation. Le Fragment A rejoint la coalition. Alanossa sera ton bras armé.", Color.Gold1);
                Narrator.Pause();
            }, s => s.Credits >= 8000),

            new("← Partir — pas prêt", _ => { Narrator.Say("Elle te regarde partir. 'Reviens quand tu seras prêt.'", Color.Grey); Narrator.Pause(); }),
        ], Color.OrangeRed1), state);
    }

    // ── FRAGMENT B — RAMASTER (L'Arc du Pic de l'Est) ───────────────────

    public static void ResolveFragmentRamaster(GameState state)
    {
        if (state.RalliedStations.Contains(FragB))
        {
            Narrator.Say("Ramaster a déjà accordé son soutien. Le fragment B de la Nexus est sécurisé.", Color.Gold1);
            Narrator.Pause();
            return;
        }

        state.NpcsMet.Add("Ramaster");
        Narrator.Say("Ramaster te reçoit dans sa salle du conseil. Des cartes de l'espace tapissent les murs. Il est vieux, mais ses yeux sont vifs.", Color.SteelBlue1);
        AnsiConsole.WriteLine();

        ChoiceMenu.Resolve(new Situation("Ramaster détient le Fragment B de la Nexus. Il est prudent, stratégique. Comment tu l'approches ?",
        [
            new("Lui présenter une vision politique de la Nexus reconstruite", s =>
            {
                if (s.Reputation < 100)
                {
                    Narrator.Say("Il secoue la tête. 'Votre réputation ne justifie pas que je risque mon fragment.' 100 de réputation requis.", Color.Red);
                    Narrator.Pause();
                    return;
                }
                switch (Rng.Next(3))
                {
                    case 0:
                        s.RalliedStations.Add(FragB);
                        s.StationPiecesRallied++;
                        s.Reputation += 100;
                        Narrator.Say("Il étudie tes plans pendant une heure. 'C'est... réalisable. Et nécessaire. Vous avez mon soutien.' Fragment B sécurisé. +100 réputation.", Color.Gold1);
                        break;
                    case 1:
                        s.Reputation += 20;
                        Narrator.Say("'Vos arguments sont solides. Mais j'ai besoin de garanties supplémentaires. Prouvez d'abord que les autres chefs sont d'accord.' +20 rép.", Color.Yellow);
                        break;
                    case 2:
                        s.Reputation += 40;
                        s.RalliedStations.Add(FragB);
                        s.StationPiecesRallied++;
                        Narrator.Say("'La paix avant la guerre, toujours.' Il signe. Fragment B sécurisé. +40 réputation.", Color.Gold1);
                        break;
                }
                Narrator.Pause();
            }, s => s.Reputation >= 100),

            new("Lui proposer un accord commercial — ses ressources, ton réseau", s =>
            {
                var cost = Rng.Next(5000, 12000);
                if (s.Credits < cost)
                {
                    Narrator.Say($"'Votre offre commerciale requiert au moins {cost}cr de capital. Revenez préparé.'", Color.Red);
                    Narrator.Pause();
                    return;
                }
                s.Credits -= cost;
                s.RalliedStations.Add(FragB);
                s.StationPiecesRallied++;
                s.Reputation += 70;
                Narrator.Say($"L'accord est signé. -{cost}cr. +70 réputation. Fragment B dans la coalition. Ramaster apporte ses flottes de transport.", Color.Gold1);
                Narrator.Pause();
            }, s => s.Credits >= 5000),

            new("L'attaquer — prendre le fragment par la force", s =>
            {
                Narrator.Say("Ramaster se lève lentement. 'Je vois. C'est votre choix.' Ses gardes entrent.", Color.Red);
                var boss = new Enemy("Ramaster et sa Garde", 180, 25, 45, 4000, 8000,
                    "Vieux stratège entouré de sa garde d'élite. Il a survécu à la Grande Guerre.", IsBoss: true);
                var outcome = Combat.Start(s, boss);

                if (outcome == CombatOutcome.Victory)
                {
                    s.RalliedStations.Add(FragB);
                    s.StationPiecesRallied++;
                    s.BossesDefeated++;
                    s.StationBossesBeaten.Add(FragB);
                    s.Reputation -= 50;
                    Narrator.Say("Ramaster s'effondre. 'Tu auras le fragment... mais pas mes hommes.' Fragment B pris de force. -50 réputation.", Color.OrangeRed1);
                }
                else
                {
                    Situations.ApplyCombatOutcome(s, outcome);
                }
                Narrator.Pause();
            }),

            new("← Partir — pas encore", _ => { Narrator.Say("'Revenez quand vous êtes prêt à vous engager sérieusement.'", Color.Grey); Narrator.Pause(); }),
        ], Color.SteelBlue1), state);
    }

    // ── FRAGMENT C — RAPHAZARUS (L'Arc Perdu) ───────────────────────────

    public static void ResolveFragmentRaphazarus(GameState state)
    {
        if (state.RalliedStations.Contains(FragC))
        {
            Narrator.Say("Raphazarus a déjà donné son accord. Le fragment C de la Nexus est sécurisé.", Color.Gold1);
            Narrator.Pause();
            return;
        }

        state.NpcsMet.Add("Raphazarus");
        Narrator.Say("Raphazarus sort de l'ombre. Il est grand, calme, et regarde à travers toi.", Color.Magenta1);
        AnsiConsole.WriteLine();

        ChoiceMenu.Resolve(new Situation("Raphazarus détient le Fragment C de la Nexus. Il est mystique, imprévisible. Comment tu l'approches ?",
        [
            new("Parler de la Nexus — évoquer la prophétie de la réunification", s =>
            {
                switch (Rng.Next(3))
                {
                    case 0:
                        s.RalliedStations.Add(FragC);
                        s.StationPiecesRallied++;
                        s.Reputation += 90;
                        Narrator.Say("Ses yeux s'éclairent. 'Le Cycle se referme. J'attendais quelqu'un comme toi depuis la fin de la Guerre.' Fragment C sécurisé. +90 réputation.", Color.Magenta1);
                        break;
                    case 1:
                        s.Reputation += 15;
                        Narrator.Say("'Tu ne comprends pas encore ce que tu demandes. Reviens quand tu auras prouvé ta valeur.' +15 rép.", Color.Yellow);
                        break;
                    case 2:
                        s.Reputation -= 10;
                        Narrator.Say("Il secoue la tête. 'Tu répètes des mots sans les comprendre. Va apprendre.' -10 rép.", Color.Red);
                        break;
                }
                Narrator.Pause();
            }),

            new("Lui offrir une arme légendaire en échange", s =>
            {
                var legWeapon = s.Weapons.FirstOrDefault(w => w.Tier >= 4);
                if (legWeapon == null)
                {
                    Narrator.Say("'Tu n'as rien à m'offrir qui vaille ce fragment.' Il ne te regarde plus.", Color.Red);
                    Narrator.Pause();
                    return;
                }
                s.Weapons.Remove(legWeapon);
                s.RalliedStations.Add(FragC);
                s.StationPiecesRallied++;
                s.Reputation += 50;
                Narrator.Say($"Il prend l'{legWeapon.Name} sans un mot. Puis hoche la tête. 'Le fragment est à toi.' Fragment C sécurisé. +50 réputation.", Color.Gold1);
                Narrator.Pause();
            }, s => s.Weapons.Any(w => w.Tier >= 4)),

            new("L'affronter — son sceptre légendaire comme enjeu", s =>
            {
                Narrator.Say("Il sourit pour la première fois. 'Enfin quelqu'un d'honnête.'", Color.Magenta1);
                var boss = new Enemy("Raphazarus", 200, 28, 52, 0, 0,
                    "Il manie le Sceptre du Vide. Chaque coup est imprévisible. Il est peut-être immortel.", IsBoss: true);
                var outcome = Combat.Start(s, boss);

                if (outcome == CombatOutcome.Victory)
                {
                    s.RalliedStations.Add(FragC);
                    s.StationPiecesRallied++;
                    s.BossesDefeated++;
                    s.StationBossesBeaten.Add(FragC);
                    // Le Sceptre de Raphazarus comme récompense
                    var sceptre = WeaponPool.Tier5.First(w => w.Name == "Le Sceptre de Raphazarus");
                    s.Weapons.Add(sceptre);
                    s.Reputation += 120;
                    Narrator.Say("Il s'incline. 'Bien joué. Prends le fragment — et le sceptre. Tu le mérites.' Fragment C sécurisé. +120 réputation. Sceptre de Raphazarus obtenu.", Color.Gold1);
                    Combat.ShowWeaponDrop(sceptre);
                }
                else
                {
                    Situations.ApplyCombatOutcome(s, outcome);
                }
                Narrator.Pause();
            }),

            new("← Repartir — trop imprévisible", _ => { Narrator.Say("Il ne te retient pas. Il disparaît dans l'ombre.", Color.Grey); Narrator.Pause(); }),
        ], Color.Magenta1), state);
    }

    // ── FRAGMENT D — DIRECTRICE ALDARA (Nexus Aldara) ───────────────────

    public static void ResolveFragmentAldara(GameState state)
    {
        if (state.RalliedStations.Contains(FragD))
        {
            Narrator.Say("La Directrice Aldara est déjà dans la coalition. Le fragment D est sécurisé.", Color.Gold1);
            Narrator.Pause();
            return;
        }

        if (state.Reputation < 100)
        {
            Narrator.Say("La Directrice Aldara ne reçoit pas les inconnus. Réputation insuffisante (100 requis).", Color.Red);
            Narrator.Pause();
            return;
        }

        state.NpcsMet.Add("Directrice Aldara");
        Narrator.Say("La Directrice Aldara te reçoit dans sa salle de conférence. Tout ici est précis, calculé, froid. Elle attend.", Color.SteelBlue1);
        AnsiConsole.WriteLine();

        ChoiceMenu.Resolve(new Situation("La Directrice Aldara détient le Fragment D de la Nexus. Elle est pragmatique, ambitieuse. Comment tu l'approches ?",
        [
            new("Lui présenter les fragments déjà sécurisés — montrer ta progression", s =>
            {
                var count = s.StationPiecesRallied;
                if (count < 2)
                {
                    Narrator.Say("'Deux fragments au minimum avant que je considère votre proposition. Vous n'en avez pas assez.' Revenez avec au moins 2 fragments.", Color.Red);
                    Narrator.Pause();
                    return;
                }
                s.RalliedStations.Add(FragD);
                s.StationPiecesRallied++;
                s.Reputation += 120;
                Narrator.Say($"Elle regarde tes données. '{count} fragments. Vous êtes sérieux.' Elle signe. Fragment D sécurisé. +120 réputation. La coalition est complète.", Color.Gold1);
                Narrator.Pause();
            }, s => s.StationPiecesRallied >= 2),

            new("Lui proposer la présidence de la Nexus reconstituée", s =>
            {
                if (s.Reputation < 200)
                {
                    Narrator.Say("'Vous n'avez pas l'autorité pour promettre ça.' Réputation 200 requis.", Color.Red);
                    Narrator.Pause();
                    return;
                }
                s.RalliedStations.Add(FragD);
                s.StationPiecesRallied++;
                s.Reputation += 100;
                s.IsFactionLeader = true;
                Narrator.Say("Elle réfléchit. 'La présidence... et vous reconstruisez sous mon autorité.' Elle tend la main. Fragment D sécurisé. Tu es chef de facto. IsFactionLeader. +100 réputation.", Color.Gold1);
                Narrator.Pause();
            }, s => s.Reputation >= 200),

            new("L'éliminer — prendre le fragment par la force", s =>
            {
                Narrator.Say("Elle ne bouge pas. 'J'attendais que quelqu'un essaie.' Ses drones s'activent.", Color.Red);
                var boss = new Enemy("Directrice Aldara + Drones", 190, 26, 48, 4500, 9000,
                    "Froide, méthodique. Elle a prévu tous les scénarios de combat.", IsBoss: true);
                var outcome = Combat.Start(s, boss);

                if (outcome == CombatOutcome.Victory)
                {
                    s.RalliedStations.Add(FragD);
                    s.StationPiecesRallied++;
                    s.BossesDefeated++;
                    s.StationBossesBeaten.Add(FragD);
                    s.Reputation -= 30;
                    Narrator.Say("Les drones tombent. Elle tombe. 'Tu as gagné... mais tu auras besoin d'autre chose que la force.' Fragment D pris. -30 réputation.", Color.OrangeRed1);
                }
                else
                {
                    Situations.ApplyCombatOutcome(s, outcome);
                }
                Narrator.Pause();
            }),

            new("← Partir — pas encore prêt", _ => { Narrator.Say("'Revenez quand vous aurez quelque chose de concret à m'offrir.'", Color.Grey); Narrator.Pause(); }),
        ], Color.SteelBlue1), state);
    }

    // ── VÉRIFICATION : NEXUS RECONSTITUÉE ──────────────────────────────

    public static void CheckNexusComplete(GameState state)
    {
        if (state.StationPiecesRallied < 4) return;
        if (state.CompletedObjectives.Contains("space_lord")) return;

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[gold1 bold]LA STATION NEXUS RENAÎT[/]").RuleStyle("gold1"));
        Narrator.Say(
            "Les quatre fragments sont réunis. Les quatre chefs ont signé — ou ont été vaincus. " +
            "La Station Nexus se reconstitue dans l'espace, visible de toutes les stations à des milliers de kilomètres. " +
            "Tout le monde sait qui a fait ça. Ton nom résonne dans chaque couloir de chaque station connue.",
            Color.Gold1);
        AnsiConsole.WriteLine();

        if (!state.IsFactionLeader)
        {
            state.IsFactionLeader = true;
            AnsiConsole.MarkupLine("  [gold1 bold]Tu es le Seigneur de l'Espace.[/]");
        }

        Narrator.Pause();
    }
}
