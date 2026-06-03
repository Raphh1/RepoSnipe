using Spectre.Console;

namespace VoidTrader;

// Un PNJ qui s'obsède sur le joueur et le suit entre les stations.
// Level 0 → inactif
// Level 1 → première rencontre bizarre, début de surveillance
// Level 2 → tu le revois, c'est pas une coïncidence
// Level 3 → objets volés, réputation attaquée, notes inquiétantes
// Level 4 → confrontation directe, attaques furtives, terreur installée
// Level 5 → il frappe de nulle part — peut tuer

static class StalkerSystem
{
    private static readonly Random Rng = new();

    static readonly string[] Names =
    [
        "Tessik","Voral","Mund","Rhayne","Cossel","Breth","Sulwa","Nael",
        "Drenne","Petik","Halvak","Selm","Yorra","Queth","Barin","Foss",
        "Olia","Wren","Cassel","Tuvok","Merri","Sool","Vanthas","Lorre",
    ];

    static readonly string[] Obsessions =
    [
        "ta façon de marcher dans les couloirs",
        "ton vaisseau — il dit qu'il lui ressemble",
        "ta voix — il prétend entendre quelqu'un qu'il a perdu",
        "ce que tu transportes — il veut savoir ce que tu caches",
        "tes décisions — il les note toutes, il les analyse",
        "ton passé — il prétend savoir qui tu étais avant",
        "tes combats — il les regarde, il les rejoue dans sa tête",
        "ton visage — il dit que t'es quelqu'un d'important sans le savoir",
        "tes mouvements — il peut prédire où tu vas",
        "ta réputation — il l'étudie comme une science",
    ];

    // ── PROBABILITÉ D'APPARITION ─────────────────────────────────────────────

    // Appelé dans Encounters.Roll avant le tirage normal.
    // Renvoie true si un événement stalker a été déclenché (stoppe le tirage normal).
    public static bool MaybeTrigger(GameState state)
    {
        // Pas de stalker actif → petite chance d'en créer un (seulement dans les zones dangereuses)
        if (state.StalkerLevel == 0)
        {
            var danger = Universe.Danger(Universe.Get(state.CurrentStation));
            if (danger < 2) return false;      // pas dans les zones pacifiques
            if (Rng.Next(100) >= 7) return false;  // 7% de chance de spawn

            InitStalker(state);
            ChoiceMenu.Resolve(FirstEncounter(state), state);
            return true;
        }

        // Stalker actif → le compteur d'actions détermine quand il frappe
        if (state.StalkerActionsLeft > 0)
        {
            state.StalkerActionsLeft--;

            // Messages passifs selon l'intensité (même si l'event complet n'est pas déclenché)
            if (state.StalkerActionsLeft <= 1)
                ShowPassiveMessage(state);

            return false;
        }

        // Compteur arrivé à 0 → déclencher l'event complet
        ChoiceMenu.Resolve(StalkerEvent(state), state);
        return true;
    }

    static void InitStalker(GameState state)
    {
        state.StalkerName      = Names[Rng.Next(Names.Length)];
        state.StalkerObsession = Obsessions[Rng.Next(Obsessions.Length)];
        state.StalkerLevel     = 1;
        state.StalkerActionsLeft = Rng.Next(4, 8);  // il revient bientôt
    }

    static void ResetCountdown(GameState state)
    {
        // Plus le niveau est élevé, plus les événements sont fréquents
        state.StalkerActionsLeft = state.StalkerLevel switch
        {
            1 => Rng.Next(5, 10),
            2 => Rng.Next(3, 7),
            3 => Rng.Next(2, 5),
            4 => Rng.Next(1, 4),
            _ => Rng.Next(1, 3),
        };
    }

    // ── MESSAGES PASSIFS (ambiance) ──────────────────────────────────────────

    static void ShowPassiveMessage(GameState state)
    {
        if (state.StalkerName == null) return;

        var msgs = state.StalkerLevel switch
        {
            1 => new[] {
                $"Tu passes dans un couloir. Quelqu'un se retourne. Tu l'as peut-être déjà vu.",
                "Une ombre dans ta vision périphérique. Quand tu te retournes, personne.",
                "Quelqu'un a touché à tes affaires. T'es pas sûr. Peut-être.",
            },
            2 => new[] {
                $"Tu sens que tu es observé. Depuis un moment maintenant.",
                $"Un mouvement au fond du couloir. Puis plus rien.",
                $"Le nom {state.StalkerName} revient dans les conversations ambiantes. Peut-être une coïncidence.",
                "Des pas derrière toi. Ils s'arrêtent quand les tiens s'arrêtent.",
            },
            3 => new[] {
                $"{state.StalkerName} était là. Tu le sais sans l'avoir vu.",
                "Un objet a changé de place dans ton vaisseau. Personne d'autre n'aurait pu entrer.",
                "Quelqu'un parle de toi ici. Des choses que tu aurais préférées garder pour toi.",
                $"Tu trouves un message. Trois mots. Pas de signature. '{state.StalkerName.ToUpper()[0]} T'A VU.'",
                "La station semble te regarder différemment. Quelqu'un a dit quelque chose.",
            },
            4 => new[] {
                $"{state.StalkerName} t'a suivi jusqu'ici. Il n'essaie plus de se cacher.",
                "Il y a quelqu'un dans les ombres de ce couloir. Il n'avance pas. Il attend.",
                $"Une note sous ta porte de vaisseau. '{state.StalkerName} voulait que tu saches : il est toujours là.'",
                "Tu fermes les yeux une seconde. Quand tu les rouvres, quelque chose a changé dans la salle.",
                "Le bruit de pas s'est arrêté juste derrière toi.",
            },
            _ => new[] {
                $"[red bold]{state.StalkerName} EST DANS CETTE STATION.[/]",
                "Tu as les cheveux sur la nuque dressés depuis une heure. Il y a une raison.",
                "Quelqu'un a laissé quelque chose devant ton vaisseau. Un objet qui t'appartient. Que t'avais perdu.",
                "Il sait où tu dors. Il sait où tu manges. Il sait où tu vas.",
            },
        };

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey dim]...[/]");
        AnsiConsole.MarkupLine($"[grey italic]{msgs[Rng.Next(msgs.Length)]}[/]");
        Thread.Sleep(400);
    }

    // ── PREMIÈRE RENCONTRE (subtile) ─────────────────────────────────────────

    static Situation FirstEncounter(GameState state)
    {
        var name = state.StalkerName!;
        var obs  = state.StalkerObsession!;

        var intros = new[]
        {
            $"Un inconnu nommé {name} te regarde de l'autre bout de la salle. Pas agressivement. Juste... trop longtemps.",
            $"Quelqu'un te double dans un couloir et ralentit juste devant toi pour te regarder. Il dit '{name}' en s'éloignant. Son prénom ou le tien ?",
            $"Un type est assis là depuis que t'es arrivé. Quand tu bouges, ses yeux te suivent. Son badge dit {name}.",
            $"En sortant d'un stand, tu te retrouves nez-à-nez avec quelqu'un. Il sourit comme s'il te connaissait. 'Je t'ai remarqué', dit-il. Son nom, {name}.",
        };

        Narrator.Say(intros[Rng.Next(intros.Length)], Color.Grey);
        Narrator.Say($"Quelque chose dans son regard est fixé sur {obs}.", Color.Grey);

        ResetCountdown(state);

        return new Situation($"{name} te regarde. Comment tu réagis ?",
        [
            new("Ignorer complètement — c'est rien", s =>
            {
                Narrator.Say("T'as tourné le dos. Il t'a regardé partir. T'as pas vu son expression.", Color.Grey);
                Narrator.Pause();
                // Level reste à 1, mais il reviendra
            }),
            new("Le regarder dans les yeux et continuer", s =>
            {
                if (Rng.Next(100) < 50)
                    Narrator.Say($"{name} sourit légèrement. Il baisse les yeux le premier. Mais c'est lui qui a arrêté.", Color.Yellow);
                else
                {
                    Narrator.Say($"{name} ne cligne pas des yeux. Pendant de longues secondes. Puis il sourit.", Color.Red);
                    s.StalkerLevel = 2;  // ça l'encourage
                }
                Narrator.Pause();
            }),
            new("Lui parler — demander ce qu'il veut", s =>
            {
                s.StalkerLevel = 2;
                var réponses = new[]
                {
                    $"'Je voulais juste voir si tu me reconnaissais. Tu devrais.' Il repart sans explication.",
                    $"'T'as quelque chose que je cherche depuis longtemps.' Il regarde {obs}. 'Je trouverai pas quoi avant toi.'",
                    $"'Je te suis depuis {s.CurrentStation}. J'apprends des choses.' Puis il s'en va.",
                    $"Il dit ton surnom. Le seul que personne devrait connaître ici. Il s'éloigne.",
                };
                Narrator.Say(réponses[Rng.Next(réponses.Length)], Color.OrangeRed1);
                Narrator.Pause();
            }),
            new("Le menacer de partir — c'est quoi son problème", s =>
            {
                if (Rng.Next(100) < 60)
                {
                    Narrator.Say($"{name} s'éloigne sans un mot. Peut-être réglé. Probablement pas.", Color.Grey);
                    s.StalkerLevel = 0;  // résolu dès le départ si on est assez direct
                    Display.ShowEvent("Il est parti. Peut-être pour de bon.", Color.Green);
                }
                else
                {
                    Narrator.Say($"{name} sourit. 'D'accord.' Il repart. Son sourire était trop calme.", Color.Red);
                    s.StalkerLevel = 2;
                }
                Narrator.Pause();
            }),
        ], Color.Grey);
    }

    // ── ÉVÉNEMENTS ESCALADANTS ───────────────────────────────────────────────

    static Situation StalkerEvent(GameState state)
    {
        ResetCountdown(state);

        return state.StalkerLevel switch
        {
            1 => Level1Event(state),
            2 => Level2Event(state),
            3 => Level3Event(state),
            4 => Level4Event(state),
            _ => Level5Event(state),
        };
    }

    // ── LEVEL 1 : il se montre, c'est encore ambigu ──────────────────────────

    static Situation Level1Event(GameState state)
    {
        var name = state.StalkerName!;
        state.StalkerLevel = 2;

        Narrator.Say($"Tu tournes au coin d'un couloir. {name} est là. Il ne dit rien. Il attendait.", Color.Grey);

        return new Situation("Il te regarde. Il n'avance pas.",
        [
            new("Lui demander comment il t'a retrouvé", s =>
            {
                Narrator.Say($"'C'est pas difficile quand on sait quoi chercher.' Il sourit. 'Je sais des choses sur toi.'", Color.OrangeRed1);
                Narrator.Pause();
            }),
            new("Changer de route sans un mot", s =>
            {
                Narrator.Say($"Tu changes de direction. Quand tu jettes un œil derrière toi cinq minutes plus tard, {name} n'est plus là. Mais il était là.", Color.Grey);
                Narrator.Pause();
            }),
            new("Le frapper préventivement", s =>
            {
                Narrator.Say("Il esquive. Il est plus rapide que t'as évalué.", Color.Red);
                var e = new Enemy(name, 50, 10, 22, 0, 0, $"Il t'attendait. Il était prêt.", CaptureChance: 0, KillChance: 15);
                var outcome = Combat.Start(s, e);
                if (outcome == CombatOutcome.Victory)
                {
                    Narrator.Say($"{name} s'enfuit blessé. Peut-être réglé. Ou aggravé.", Color.Yellow);
                    s.StalkerLevel = Rng.Next(2) == 0 ? 0 : 3;
                    if (s.StalkerLevel == 0) Display.ShowEvent("Il est parti en courant. Fini, peut-être.", Color.Green);
                    else Display.ShowEvent("Il est parti blessé. Mais pas mort. Et maintenant il a une raison.", Color.Red);
                }
                else
                {
                    Situations.ApplyCombatOutcome(s, outcome);
                    s.StalkerLevel = 4;
                }
            }),
        ], Color.Grey);
    }

    // ── LEVEL 2 : il est visiblement en train de te surveiller ──────────────

    static Situation Level2Event(GameState state)
    {
        var name = state.StalkerName!;
        state.StalkerLevel = 3;

        Narrator.Say($"{name} est dans le même espace que toi. Encore. Il t'a clairement suivi dans cette station.", Color.OrangeRed1);

        return new Situation($"{name} ne se cache plus vraiment.",
        [
            new("Le confronter — 'Tu me suis.'", s =>
            {
                var resps = new[]
                {
                    "'T'es observatif. J'aime ça chez toi.' Il sourit. Son regard ne sourit pas.",
                    "'Je me promène.' Il tient un carnet. Ton nom y est écrit plusieurs fois.",
                    $"'Je suis partout où tu vas.' Pause. 'C'est pas une menace. C'est un fait.'",
                    "'Et si c'était le cas ?' Il te regarde calmement. 'Qu'est-ce que tu ferais ?'",
                };
                Narrator.Say(resps[Rng.Next(resps.Length)], Color.Red);
                Narrator.Pause();
            }),
            new("Signaler aux autorités de la station", s =>
            {
                var danger = Universe.Danger(Universe.Get(s.CurrentStation));
                if (danger <= 2 && Rng.Next(100) < 55)
                {
                    s.StalkerLevel = 0;
                    Narrator.Say($"Les autorités prennent ça au sérieux. {name} est escorté hors de la station. C'est réglé.", Color.Green);
                    Display.ShowEvent("Stalker expulsé. +10 réputation.", Color.Green);
                    s.Reputation += 10;
                }
                else
                {
                    Narrator.Say($"Les autorités s'en foutent. Ou {name} a de l'influence ici. Il est encore là. Et il sait que t'as essayé.", Color.Red);
                    s.StalkerLevel = 4;
                }
                Narrator.Pause();
            }),
            new("Tendre un piège — l'attirer quelque part et l'affronter", s =>
            {
                Narrator.Say($"T'as trouvé un angle mort dans un couloir. T'attendais {name}.", Color.OrangeRed1);
                var e = new Enemy(name, 65, 12, 25, 200, 600, "Il était prêt mais pas autant que toi.", CaptureChance: 5, KillChance: 20);
                var outcome = Combat.Start(s, e);
                if (outcome == CombatOutcome.Victory)
                {
                    var roll = Rng.Next(3);
                    if (roll == 0) { s.StalkerLevel = 0; Narrator.Say($"{name} est à terre. Il rampe vers une sortie. 'C'est fini.'", Color.Green); Display.ShowEvent("Stalker neutralisé. Enfin.", Color.Green); }
                    else if (roll == 1) { s.StalkerLevel = 4; Narrator.Say($"{name} récupère et fuit. Mais maintenant il est blessé et en colère.", Color.Red); }
                    else { s.StalkerLevel = 0; Narrator.Say($"{name} s'effondre. Pas mort. Mais cassé. Il ne reviendra pas.", Color.Gold1); Display.ShowEvent("Stalker mis hors d'état. Terminé.", Color.Gold1); }
                }
                else Situations.ApplyCombatOutcome(s, outcome);
            }),
            new("L'ignorer encore — c'est peut-être juste une coïncidence", s =>
            {
                Narrator.Say("T'as fait comme si de rien. Il t'a regardé faire.", Color.Grey);
                Narrator.Pause();
                // Level déjà mis à 3
            }),
        ], Color.OrangeRed1);
    }

    // ── LEVEL 3 : vol d'objets, réputation attaquée, messages ───────────────

    static Situation Level3Event(GameState state)
    {
        var name = state.StalkerName!;
        state.StalkerLevel = 4;

        var eventType = Rng.Next(3);
        switch (eventType)
        {
            // Vol
            case 0:
                if (state.Cargo.All.Any())
                {
                    var item = state.Cargo.All.Keys.ElementAt(Rng.Next(state.Cargo.All.Count));
                    state.Cargo.Remove(item, 1);
                    Narrator.Say($"Il manque quelque chose dans ta cargaison. 1x {item}. Il n'y a pas de traces d'effraction.", Color.Red);
                    Display.ShowEvent($"-1 {item}. {name}.", Color.Red);
                }
                else
                {
                    var vol = Rng.Next(200, 800);
                    state.Credits = Math.Max(0, state.Credits - vol);
                    Narrator.Say($"-{vol}cr de ton vaisseau. La serrure n'a pas été forcée.", Color.Red);
                }
                Narrator.Say($"Un mot est posé là où l'objet était. '{name}'.", Color.Red);
                break;

            // Réputation attaquée — rumeurs
            case 1:
                var repPerte = Rng.Next(20, 50);
                state.Reputation -= repPerte;
                Narrator.Say($"Des gens te regardent différemment depuis ce matin. {name} a parlé. Des choses vraies ou inventées — ça n'a plus d'importance.", Color.Red);
                Display.ShowEvent($"-{repPerte} réputation. {name} répand quelque chose.", Color.Red);
                break;

            // Note / message inquiétant
            default:
                var messages = new[]
                {
                    $"'Je sais où tu vas après ça.' — {name}",
                    $"'J'ai compté tes crédits. T'as pas fait une mauvaise semaine.' — {name}",
                    $"'Ta cargaison m'intéresse. Mais c'est pas pour ça que je te suis.' — {name}",
                    $"'T'as fait des erreurs. Je les ai toutes notées.' — {name}",
                    $"'C'est bientôt.' — {name}",
                    $"'Je t'ai vu dormir. Tu bouges beaucoup.' — {name}",
                };
                var msg = messages[Rng.Next(messages.Length)];
                Narrator.Say($"Une note glissée sous la porte de ton vaisseau.", Color.Red);
                AnsiConsole.MarkupLine($"\n  [red dim italic]\"{msg}\"[/]\n");
                break;
        }

        return new Situation($"{name} est partout. Qu'est-ce que tu fais ?",
        [
            new("L'affronter maintenant — trouver et régler ça", s =>
            {
                Narrator.Say($"T'as cherché {name} dans la station. Il n'était nulle part visible. Puis il était derrière toi.", Color.Red);
                var e = new Enemy(name, 85, 15, 30, 400, 1200, $"Il te connaît mieux que tu le connais. C'est son avantage.", CaptureChance: 5, KillChance: 25);
                var outcome = Combat.Start(s, e);
                if (outcome == CombatOutcome.Victory)
                {
                    s.StalkerLevel = 0;
                    Narrator.Say($"{name} est à terre. T'as regardé dans ses yeux. T'as vu quelque chose que t'aurais préféré pas voir.", Color.Gold1);
                    Display.ShowEvent("Stalker neutralisé.", Color.Gold1);
                }
                else Situations.ApplyCombatOutcome(s, outcome);
            }),
            new("Quitter la station immédiatement", s =>
            {
                Narrator.Say($"T'as décidé de partir. Tu pars. Mais en montant dans ton vaisseau, t'as vu une ombre disparaître dans le sas d'amarrage d'à côté.", Color.Red);
                // Il suit dans la prochaine station
                Display.ShowEvent($"{name} a vu que tu partais. Il va partir aussi.", Color.Red);
                Narrator.Pause();
            }),
            new("Engager quelqu'un pour le surveiller à ton tour", s =>
            {
                var cout = Rng.Next(500, 1500);
                if (s.Credits >= cout) {
                    s.Credits -= cout;
                    if (Rng.Next(100) < 55) {
                        s.StalkerLevel = 2;
                        Narrator.Say($"-{cout}cr. Ton contact a trouvé quelque chose. Le niveau de menace baisse, mais {name} est toujours là.", Color.Yellow);
                        Display.ShowEvent($"{name} neutralisé partiellement. Intensité réduite.", Color.Yellow);
                    } else {
                        Narrator.Say($"-{cout}cr. Ton contact a disparu. {name} l'a peut-être trouvé en premier.", Color.Red);
                    }
                } else Narrator.Say("T'as pas assez pour engager quelqu'un.", Color.Grey);
                Narrator.Pause();
            }),
        ], Color.Red);
    }

    // ── LEVEL 4 : confrontations directes, violence proche ──────────────────

    static Situation Level4Event(GameState state)
    {
        var name = state.StalkerName!;
        state.StalkerLevel = 5;

        Narrator.Say($"{name} est là. Il ne se cache plus du tout. Il te regarde avancer vers lui sans bouger.", Color.Red);
        AnsiConsole.Write(new Rule($"[red bold]{name.ToUpper()}[/]").RuleStyle("red"));

        return new Situation($"{name}. Il te bloque le chemin.",
        [
            new("Se battre — finir ça maintenant", s =>
            {
                Narrator.Say($"Il t'attendait. Il est prêt. Il a passé des semaines à t'observer pour ce moment.", Color.Red);
                var e = new Enemy(name, 110, 18, 36, 600, 1800, $"Il connaît tes habitudes de combat. Il a préparé une contre-stratégie.", CaptureChance: 0, KillChance: 30);
                var outcome = Combat.Start(s, e);
                if (outcome == CombatOutcome.Victory)
                {
                    s.StalkerLevel = 0;
                    s.Reputation += 20;
                    Narrator.Say($"{name} tombe. Il te regarde en tombant. Ses derniers mots : '{Rng.Next(3) switch { 0 => "Tu étais exactement comme je pensais.", 1 => "Je t'ai vu faire pire.", _ => "Ça valait quand même le voyage." }}'", Color.Gold1);
                    Display.ShowEvent("Stalker éliminé définitivement. +20 réputation.", Color.Gold1);
                }
                else Situations.ApplyCombatOutcome(s, outcome);
            }),
            new("Essayer de comprendre ce qu'il veut vraiment", s =>
            {
                var revelations = new[]
                {
                    $"Il sort quelque chose. Une photo de quelqu'un. Quelqu'un qui te ressemble. 'Tu lui as pris quelque chose.' T'es pas sûr de comprendre. Il t'attaque quand même.",
                    $"'J'étais comme toi. Avant.' Il parle d'une chose que t'aurais dû faire différemment. Un moment que t'as oublié. Lui pas.",
                    $"'Je collectionne les gens comme toi. T'es pas le premier.' Il te montre un carnet. Des noms. Il attaque.",
                    $"Il dit quelque chose d'incompréhensible. Puis il attaque. Peut-être que c'est jamais eu de sens.",
                };
                Narrator.Say(revelations[Rng.Next(revelations.Length)], Color.Red);
                var e = new Enemy(name, 110, 18, 36, 600, 1800, "Il attaque.", CaptureChance: 0, KillChance: 30);
                Situations.ApplyCombatOutcome(s, Combat.Start(s, e));
                if (s.StalkerLevel > 0 && !s.IsDead) { s.StalkerLevel = 0; Display.ShowEvent($"{name} neutralisé dans la confrontation.", Color.Gold1); }
            }),
        ], Color.Red);
    }

    // ── LEVEL 5 : il frappe de nulle part ────────────────────────────────────

    static Situation Level5Event(GameState state)
    {
        var name = state.StalkerName!;

        var scénarios = new[]
        {
            "Tu rentres dans ton vaisseau. Il était à l'intérieur. Il attendait dans le noir.",
            "T'es dans un couloir désert. Une silhouette décroche du plafond. C'est lui.",
            $"Un message d'urgence te fait sortir. Faux. {name} t'attendait à la sortie.",
            "T'as éteint les lumières. Le temps de les rallumer, quelque chose a changé dans la pièce.",
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red bold]⚠[/]").RuleStyle("red"));
        Narrator.Say(scénarios[Rng.Next(scénarios.Length)], Color.Red);
        Narrator.Say($"Pas le temps de réfléchir. {name} attaque.", Color.Red);

        var finalEnemy = new Enemy(
            name, 140, 22, 45, 800, 2500,
            $"Il t'a étudié pendant des semaines. Il frappe là où ça fait le plus mal.",
            CaptureChance: 0, KillChance: 40
        );

        // Pas de choix — il frappe. C'est le principe du level 5.
        state.StalkerLevel = 0;  // reset après l'event final
        var outcome = Combat.Start(state, finalEnemy);
        if (outcome == CombatOutcome.Victory)
        {
            state.Reputation += 30;
            Display.ShowEvent($"{name} éliminé. C'était lui ou toi. +30 réputation.", Color.Gold1);
        }
        else Situations.ApplyCombatOutcome(state, outcome);

        // Situation vide post-combat
        return new Situation("C'est fini.", [ new("Continuer", _ => { Narrator.Pause(); }) ], Color.Grey);
    }
}
