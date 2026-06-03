namespace VoidTrader;

static class Universe
{
    private static readonly Random Rng = new();

    public static readonly List<Station> Stations =
    [
        // ═══════════════════════════════════════════════════════════════════
        // STATIONS DE DÉPART
        // ═══════════════════════════════════════════════════════════════════
        new("La Carcasse",
            ["Ferraille", "Eau", "Rations", "Cellules de carburant"],
            X: 0, Y: 0, IsPeaceful: false, IsStarting: true,
            Description: "Une épave reconvertie en habitat de fortune. Sombre, malodorante, surpeuplée."),

        new("Port Méridien",
            ["Rations", "Eau", "Pièces techniques", "Médicaments", "Cellules de carburant"],
            X: 100, Y: 50, IsPeaceful: true, IsStarting: true,
            Description: "Station commerciale banale. Ni riche ni pauvre. Le point de départ des gens honnêtes."),

        new("Forge Alpha",
            ["Pièces techniques", "Ferraille", "Cellules de carburant", "Explosifs"],
            X: -80, Y: 100, IsPeaceful: false, IsStarting: true,
            Description: "Station industrielle couverte de suie. Bruyante, chaude, efficace."),

        new("Les Bas-Fonds de Vega",
            ["Armes", "Explosifs", "Ferraille", "Marchandises illégales", "Cellules de carburant"],
            X: 150, Y: -80, IsPeaceful: false, IsStarting: true,
            Description: "Station semi-légale à la frontière de plusieurs territoires. Personne ne pose de questions."),

        new("La Citadelle",
            ["Armes", "Pièces techniques", "Médicaments", "Cellules de carburant"],
            X: -120, Y: -60, IsPeaceful: false, IsStarting: true,
            Description: "Ancienne base militaire reconvertie en résidence pour l'élite. Propre, froide, silencieuse."),

        new("Fort Kharos",
            ["Armes", "Explosifs", "Cellules de carburant", "Ferraille"],
            X: 200, Y: 150, IsPeaceful: false, IsStarting: true,
            Description: "Base abandonnée reprise par des guerriers. Les murs portent les cicatrices des batailles passées."),

        // ═══════════════════════════════════════════════════════════════════
        // STATIONS BAS NIVEAU (5 000–15 000cr)
        // ═══════════════════════════════════════════════════════════════════
        new("La Ferronnerie",
            ["Ferraille", "Pièces techniques", "Pièces détachées", "Cellules de carburant"],
            X: 60, Y: 30, IsPeaceful: false,
            Unlock: new(Credits: 5_000),
            Description: "Un astéroïde creusé et habité. Poussiéreux, bruyant, honnête."),

        new("Port des Brumes",
            ["Marchandises illégales", "Artefacts", "Cellules de carburant"],
            X: -60, Y: -40, IsPeaceful: false,
            Unlock: new(Credits: 5_000),
            Description: "Station orbitale dans un nuage de gaz dense. Personne ne pose de questions ici."),

        new("Station Rocaille",
            ["Minerai brut", "Eau", "Cellules de carburant", "Ferraille"],
            X: 90, Y: -70, IsPeaceful: false,
            Unlock: new(Credits: 5_000),
            Description: "Vieille station minière abandonnée puis réoccupée. Rouillée, mal éclairée, mais accueillante."),

        new("Le Marché Flottant",
            ["Rations", "Eau", "Ferraille", "Pièces techniques", "Médicaments", "Armes", "Vivres"],
            X: 30, Y: 100, IsPeaceful: true,
            Unlock: new(Credits: 3_000),
            Description: "Convoi de vaisseaux soudés ensemble. Festif et chaotique — des centaines de vendeurs crient en même temps."),

        new("Avant-Poste Kalem",
            ["Armes", "Armures", "Rations", "Médicaments", "Cellules de carburant"],
            X: -40, Y: 90, IsPeaceful: false,
            Unlock: new(Credits: 8_000),
            Description: "Petite station militaire désaffectée. Occupée par des vétérans sans guerre. Disciplinée mais mélancolique."),

        new("La Bulle",
            ["Objets expérimentaux", "Plantes médicinales", "Médicaments"],
            X: 70, Y: 80, IsPeaceful: true,
            Unlock: new(Credits: 8_000),
            Description: "Station de recherche abandonnée. Les couloirs sentent les produits chimiques. Les gens parlent tout seuls."),

        new("Terminus Sud",
            ["Cellules de carburant", "Rations", "Eau", "Pièces techniques"],
            X: 130, Y: -40, IsPeaceful: true,
            Unlock: new(Credits: 5_000),
            Description: "Station carrefour entre plusieurs routes commerciales. Animée, professionnelle, tout le monde est pressé."),

        new("Les Décombres de Vael",
            ["Artefacts", "Pièces détachées", "Ferraille"],
            X: -90, Y: 40, IsPeaceful: true,
            Unlock: new(Credits: 10_000),
            Description: "Épave d'une ancienne cité spatiale. Apocalyptique et silencieuse — des gens vivent dans les débris."),

        // ═══════════════════════════════════════════════════════════════════
        // STATIONS NIVEAU MOYEN (15 000–40 000cr)
        // ═══════════════════════════════════════════════════════════════════
        new("Nexus Aldara",
            ["Rations", "Eau", "Pièces techniques", "Médicaments", "Armes", "Vivres", "Cellules de carburant"],
            X: 200, Y: 80, IsPeaceful: true,
            Unlock: new(Credits: 20_000),
            Description: "Grande station commerciale prospère. Propre, organisée, légèrement froide. Tout se paye."),

        new("Le Sanctuaire des Dérives",
            ["Médicaments", "Plantes médicinales", "Eau", "Vivres"],
            X: -150, Y: 80, IsPeaceful: true,
            Unlock: new(Credits: 15_000),
            Description: "Station médicale et refuge de l'Ordre des Soigneurs. Calme, odeur d'antiseptique, des blessés partout."),

        new("Fort Ossian",
            ["Armes", "Armures", "Explosifs", "Pièces détachées", "Cellules de carburant"],
            X: 180, Y: -120, IsPeaceful: false,
            Unlock: new(Credits: 25_000),
            Description: "Station militaire privée des Mercenaires d'Ossian. Tendue — tout le monde est armé, les regards jaugent."),

        new("La Forge Noire",
            ["Armes", "Armures", "Marchandises illégales", "Pièces détachées", "Explosifs"],
            X: -120, Y: -160, IsPeaceful: false,
            Unlock: new(Credits: 20_000),
            Description: "Station de fabrication secrète. Sombre, bruit de métal, secrets partout."),

        new("Station Belvédère",
            ["Cartes stellaires", "Or", "Vivres", "Médicaments"],
            X: 240, Y: -60, IsPeaceful: true,
            Unlock: new(Credits: 25_000, Reputation: 50),
            Description: "Station panoramique au bord d'une nébuleuse. Belle, luxueuse — des riches qui contemplent l'univers. Les gens à mauvaise réputation ne sont pas les bienvenus."),

        new("L'Entrepôt Zéro",
            ["Ferraille", "Pièces techniques", "Armes", "Marchandises illégales"],
            X: -190, Y: 110, IsPeaceful: false,
            Unlock: new(Credits: 15_000, Resource: "Or", ResourceAmount: 5),
            Description: "Énorme entrepôt spatial désaffecté. Silencieux, des caisses partout. On se sent épié."),

        new("Colonie Perséphone",
            ["Vivres", "Eau", "Plantes médicinales", "Rations"],
            X: 160, Y: 180, IsPeaceful: true,
            Unlock: new(Credits: 15_000),
            Description: "Colonie agricole spatiale. Rurale, simple, méfiante envers les étrangers au départ."),

        new("Le Purgatoire",
            ["Armes", "Marchandises illégales", "Cellules de carburant"],
            X: -80, Y: -180, IsPeaceful: false,
            Unlock: new(Credits: 20_000),
            Description: "Station pénitentiaire reconvertie. Oppressante, hiérarchie brutale, survie de chaque instant."),

        // ═══════════════════════════════════════════════════════════════════
        // STATIONS AVANCÉES (définies par le joueur)
        // ═══════════════════════════════════════════════════════════════════
        new("Emporium Requiem",
            ["Armes", "Or", "Vivres", "Pièces techniques", "Cellules de carburant"],
            X: 400, Y: 200, IsPeaceful: true,
            Unlock: new(Credits: 50_000, Reputation: -200),
            BannedClasses: ["Vagabond", "Ferrailleur", "Accro"],
            Description: "Planète artificielle de l'Emporium Requiem. Puissance de feu spectaculaire, richesse évidente. Les criminels notoires n'y sont pas admis."),

        new("Arc Ouest Apocalypse",
            ["Armes", "Explosifs", "Marchandises illégales", "Ferraille", "Matière noire", "Cellules de carburant"],
            X: -300, Y: 350, IsPeaceful: false,
            Unlock: new(Resource: "Ferraille", ResourceAmount: 500),
            Description: "Fragment d'une station titanesque. Contrôlée par Alanossa, pirate dangereux et instable."),

        new("Esmeralda",
            ["Vivres", "Plantes médicinales", "Eau", "Médicaments"],
            X: 250, Y: -300, IsPeaceful: true,
            Unlock: new(Resource: "Vivres", ResourceAmount: 200),
            BannedClasses: ["Seigneur de guerre"],
            Description: "Petite planète du roi Maxance. Faune dense, animaux en harmonie, meilleurs vivres de l'univers."),

        new("Scotty Golden North",
            ["Or", "Vivres", "Pièces techniques", "Médicaments"],
            X: -200, Y: -250, IsPeaceful: true,
            Unlock: new(Credits: 30_000),
            Description: "Fragment de la grande station. Dirigée par Samy Scotty. Immense, riche, futuriste. Casino."),

        new("Star Quest",
            ["Vivres", "Or", "Médicaments"],
            X: 350, Y: -150, IsPeaceful: false,
            Unlock: new(Credits: 20_000),
            Description: "Planète de Mister Eliotis. Un seul but : faire la fête. Gouffre financier si on se laisse aller."),

        new("L'Arc du Pic de l'Est",
            ["Pièces techniques", "Pièces détachées", "Armes", "Cellules de carburant"],
            X: 300, Y: 400, IsPeaceful: false,
            Unlock: new(Resource: "Pièces techniques", ResourceAmount: 100),
            Description: "Fragment de la grande station. Ingénieur Ramaster. Seul endroit pour les améliorations majeures."),

        // ═══════════════════════════════════════════════════════════════════
        // STATIONS HAUT NIVEAU (40 000cr+)
        // ═══════════════════════════════════════════════════════════════════
        new("La Couronne d'Eos",
            ["Matière noire", "Or", "Artefacts", "Cartes stellaires"],
            X: 500, Y: 300, IsPeaceful: true,
            Unlock: new(Credits: 80_000),
            Description: "Méga-station de la Coalition des Grands Marchands. Extravagante, tout est doré."),

        new("Les Puits de Noctis",
            ["Minerai exotique", "Matière noire", "Minerai brut", "Cellules de carburant"],
            X: -450, Y: 250, IsPeaceful: false,
            Unlock: new(Credits: 60_000),
            Description: "Planète minière hostile. Atmosphère toxique. Les mineurs ont l'air de fantômes."),

        new("L'Académie Stellaire",
            ["Cartes stellaires", "Artefacts", "Objets expérimentaux", "Plantes médicinales"],
            X: 350, Y: -220, IsPeaceful: true,
            Unlock: new(Credits: 60_000, Reputation: 100),
            Description: "Station d'enseignement et de recherche d'élite. Intellectuelle, silencieuse, incompréhensible. Les indésirables ne reçoivent pas de laissez-passer."),

        new("Le Nid des Faucons",
            ["Armes", "Explosifs", "Marchandises illégales", "Or"],
            X: -360, Y: -300, IsPeaceful: false,
            Unlock: new(Credits: 50_000),
            Description: "Base des Faucons Noirs. Dangereuse mais ordonnée — ces pirates ont des règles."),

        new("Sanctum Machina",
            ["Pièces détachées", "Objets expérimentaux", "Cartes stellaires"],
            X: 450, Y: -320, IsPeaceful: true,
            Unlock: new(Credits: 70_000),
            Description: "Planète entièrement robotisée. Parfaite, trop parfaite. Aucun humain visible au début."),

        new("La Citadelle Écarlate",
            ["Armes", "Or", "Artefacts"],
            X: -500, Y: 400, IsPeaceful: true,
            Unlock: new(Credits: 50_000, Reputation: 500),
            Description: "Forteresse de l'Ordre des Gardiens Écarlates. Austère, martiale, sacrée."),

        new("Les Abysses de Velkor",
            ["Objets expérimentaux", "Plantes médicinales", "Marchandises illégales"],
            X: 260, Y: 450, IsPeaceful: false,
            Unlock: new(Credits: 45_000),
            Description: "Station dans les profondeurs d'une planète gazeuse. Claustrophobique, expérimentale, dangereuse."),

        // ═══════════════════════════════════════════════════════════════════
        // STATIONS RARES
        // ═══════════════════════════════════════════════════════════════════
        new("L'Arc Perdu",
            ["Artefacts", "Pièces détachées", "Or"],
            X: -400, Y: -350, IsPeaceful: false, IsRare: true,
            Description: "Fragment sud de la grande station. Raphazarus. Apparaît rarement. Tout peut arriver ici."),

        new("Le Vaisseau Fantôme Errant",
            ["Artefacts", "Armes", "Matière noire"],
            X: 100, Y: -400, IsPeaceful: false, IsRare: true,
            Description: "Vaisseau colossal sans équipage apparent. Silence total, lumières qui clignotent."),

        new("La Station du Jugement",
            ["Or", "Artefacts"],
            X: -200, Y: 300, IsPeaceful: true, IsRare: true,
            Description: "Station mystérieuse hors du temps. Des silhouettes en robe. Un seul échange par run."),

        new("Épave de l'Aurore Noire",
            ["Armes", "Pièces détachées", "Artefacts"],
            X: 400, Y: -100, IsPeaceful: false, IsRare: true,
            Description: "Épave d'un vaisseau de guerre légendaire. Grandiose et triste, traces d'une bataille épique."),

        new("L'Île Volante de Marris",
            ["Artefacts"],
            X: -30, Y: -250, IsPeaceful: true, IsRare: true,
            Description: "Petit astéroïde habité par un seul homme. Paisible, absurde. Marris parle à ses plantes."),

        new("Le Conclave des Ombres",
            ["Cartes stellaires", "Marchandises illégales", "Artefacts"],
            X: 50, Y: 350, IsPeaceful: false, IsRare: true,
            Description: "Station invisible aux scanners. Conspiration, chuchotements, tous portent des masques."),

        // ═══════════════════════════════════════════════════════════════════
        // NOUVELLES STATIONS
        // ═══════════════════════════════════════════════════════════════════

        new("La Colonie Errante",
            ["Vivres", "Eau", "Médicaments", "Rations"],
            X: -10, Y: 220, IsPeaceful: true,
            Unlock: new(Credits: 8_000),
            Description: "Vaisseau-colonie en migration perpétuelle. Ils fuient quelque chose. Personne n'explique quoi."),

        new("La Forge des Damnés",
            ["Armes", "Explosifs", "Marchandises illégales", "Pièces techniques"],
            X: -230, Y: -80, IsPeaceful: false,
            Unlock: new(Credits: 18_000),
            Description: "Usine clandestine d'armes illégales. Bruyante, chaude, dangereuse. Les ouvriers ont l'air d'esclaves."),

        new("Le Phare de Vorn",
            ["Cartes stellaires", "Cellules de carburant", "Artefacts"],
            X: 320, Y: 120, IsPeaceful: true,
            Unlock: new(Credits: 12_000),
            Description: "Station-phare à la croisée de dix routes commerciales. L'informateur le plus fiable de l'espace connu y réside."),

        new("L'Arche de Sélène",
            ["Vivres", "Plantes médicinales", "Eau", "Artefacts"],
            X: 180, Y: 320, IsPeaceful: true,
            Unlock: new(Credits: 22_000, Reputation: 50),
            Description: "Vaisseau-arche agricole géant. Des milliers de personnes y vivent et y cultivent. Un paradis fragile."),

        new("Station Terminus Noir",
            ["Marchandises illégales", "Armes", "Matière noire", "Cellules de carburant"],
            X: -350, Y: -150, IsPeaceful: false,
            Unlock: new(Credits: 30_000),
            Description: "Dernière station avant le vide profond. Ceux qui arrivent ici cherchent soit à disparaître soit à trouver quelque chose d'introuvable."),

        new("L'Observatoire",
            ["Cartes stellaires", "Artefacts", "Objets expérimentaux"],
            X: 420, Y: 380, IsPeaceful: true, IsRare: true,
            Description: "Immense télescope orbital habité par une seule astronome. Elle a vu quelque chose dans les données. Elle ne dort plus."),

        // ═══════════════════════════════════════════════════════════════════
        // NOUVELLES STATIONS
        // ═══════════════════════════════════════════════════════════════════

        // Station habitée par des créatures non-humaines intelligentes
        new("Nid de Vorreth",
            ["Matière noire", "Artefacts", "Plantes médicinales", "Objets expérimentaux"],
            X: -280, Y: 430, IsPeaceful: false,
            Unlock: new(Credits: 35_000),
            Description: "Colonie d'êtres arthropodes intelligents — les Vorreth. Leurs structures de cire géantes défient la logique. Ils ne tuent pas sans raison, mais leurs raisons sont impénétrables."),

        // Station d'une espèce liquide
        new("La Station-Océan de Rhassil",
            ["Eau", "Plantes médicinales", "Médicaments", "Artefacts"],
            X: 340, Y: 290, IsPeaceful: true,
            Unlock: new(Credits: 28_000, Reputation: 50),
            Description: "Station immergée d'une race amphibie. L'air y est humide, les couloirs sinueux, les habitants glissants et silencieux. Le commerce se fait par gestes."),

        // Station-prison devenue État indépendant
        new("La République de Cellule 9",
            ["Marchandises illégales", "Armes", "Ferraille", "Cellules de carburant"],
            X: -420, Y: -100, IsPeaceful: false,
            Unlock: new(Credits: 22_000),
            Description: "Ex-prison orbitale. Les prisonniers ont éliminé les gardiens. Ils ont fondé un État. Leur constitution est tatouée sur les murs. Le Président a trois condamnations à vie."),

        // Station de robots devenus indépendants
        new("Assemblée Première",
            ["Pièces détachées", "Objets expérimentaux", "Pièces techniques", "Cartes stellaires"],
            X: 470, Y: -180, IsPeaceful: true,
            Unlock: new(Credits: 55_000, Reputation: 100),
            Description: "Station construite et habitée par des robots qui ont décidé de leur propre existence. Pas d'hostilité — juste une indifférence polie et des échanges très précis. Ils testent les organiques."),

        // Station-bazar flottante très dangereuse
        new("Le Marché des Damnés",
            ["Marchandises illégales", "Armes", "Artefacts", "Or", "Matière noire"],
            X: -150, Y: -400, IsPeaceful: false,
            Unlock: new(Credits: 40_000),
            Description: "Bazar illégal géant fusionnant les épaves de trente vaisseaux. Tout se vend. Tout s'achète. Toi y compris. Plusieurs factions se font la guerre dans les couloirs du fond."),

        // Station mystique / culte
        new("Le Sanctuaire du Vide",
            ["Artefacts", "Plantes médicinales", "Matière noire"],
            X: 80, Y: -450, IsPeaceful: true, IsRare: true,
            Description: "Lieu de pèlerinage d'un culte ancien. Ses membres portent des masques d'étoile. Ils ne parlent pas directement — tout passe par des intermédiaires appelés 'porte-voix'. Les non-croyants sont tolérés. Pour l'instant."),
    ];

    public static Station Get(string name) =>
        Stations.First(s => s.Name == name);

    public static List<Station> OtherThan(string current) =>
        Stations.Where(s => s.Name != current).ToList();

    public static List<Station> AccessibleFrom(string current, GameState state)
    {
        return Stations
            .Where(s => s.Name != current)
            .Where(s => !s.IsRare || Rng.Next(100) < 15)
            .Where(s => IsUnlocked(s, state))
            .ToList();
    }

    public static bool IsUnlocked(Station s, GameState state)
    {
        if (s.IsStarting) return true;
        if (s.Unlock is null) return true;
        var u = s.Unlock;
        if (u.Credits         > 0 && state.Credits                  < u.Credits)         return false;
        if (u.Reputation      != 0 && state.Reputation               < u.Reputation)      return false;
        if (u.Day             > 0 && state.Day                       < u.Day)             return false;
        if (u.StationsVisited > 0 && state.VisitedStations.Count     < u.StationsVisited) return false;
        if (u.BossesDefeated  > 0 && state.BossesDefeated            < u.BossesDefeated)  return false;
        if (u.Resource is not null && state.Cargo.Get(u.Resource)    < u.ResourceAmount)  return false;
        return true;
    }

    /// <summary>
    /// Retourne un message expliquant pourquoi une station est verrouillée.
    /// Affiché quand le joueur essaie d'y aller sans remplir les conditions.
    /// </summary>
    public static string UnlockReason(Station s, GameState state)
    {
        if (s.Unlock is null) return "";
        var u = s.Unlock;
        var reasons = new List<string>();
        if (u.Credits         > 0 && state.Credits              < u.Credits)         reasons.Add($"{u.Credits:N0}cr requis (tu as {state.Credits:N0}cr)");
        if (u.Reputation      != 0 && state.Reputation           < u.Reputation)      reasons.Add($"réputation {u.Reputation}+ requise (tu as {state.Reputation})");
        if (u.Day             > 0 && state.Day                   < u.Day)             reasons.Add($"jour {u.Day} requis (tu es jour {state.Day})");
        if (u.StationsVisited > 0 && state.VisitedStations.Count < u.StationsVisited) reasons.Add($"{u.StationsVisited} stations visitées requises ({state.VisitedStations.Count} visitées)");
        if (u.BossesDefeated  > 0 && state.BossesDefeated        < u.BossesDefeated)  reasons.Add($"{u.BossesDefeated} boss vaincus requis ({state.BossesDefeated} vaincus)");
        if (u.Resource is not null && state.Cargo.Get(u.Resource) < u.ResourceAmount)  reasons.Add($"{u.ResourceAmount}x {u.Resource} requis");
        return string.Join(", ", reasons);
    }

    public static bool IsBanned(Station s, GameState state) =>
        s.BannedClasses is not null && s.BannedClasses.Contains(state.Class.Name);

    // ── NIVEAU DE DANGER (1 = sûr, 5 = mortel) ────────────────────────────────
    // Calculé pour éviter d'annoter les 45 stations. Sert à la fois à l'affichage
    // et au scaling des combats/rencontres (voir Combat & Encounters).
    public static int Danger(Station s)
    {
        int lvl = s.Name switch
        {
            // Bastions de pirates / zones mortelles
            "Arc Ouest Apocalypse" or "Le Nid des Faucons" or "Le Purgatoire"
                or "Les Puits de Noctis" or "Le Conclave des Ombres"
                or "Épave de l'Aurore Noire" => 5,

            // Haut niveau
            "Emporium Requiem" or "Star Quest" or "Fort Ossian" or "La Forge Noire"
                or "La Forge des Damnés" or "Station Terminus Noir" or "L'Arc Perdu"
                or "Le Vaisseau Fantôme Errant" or "Les Abysses de Velkor" => 4,

            // Niveau moyen
            "Nexus Aldara" or "L'Entrepôt Zéro" or "Les Décombres de Vael"
                or "L'Arc du Pic de l'Est" => 3,

            // Bas niveau / départs rudes
            "La Carcasse" or "Les Bas-Fonds de Vega" or "Fort Kharos"
                or "Port des Brumes" or "Station Rocaille" or "Forge Alpha"
                or "La Citadelle" or "Avant-Poste Kalem" or "La Ferronnerie" => 2,

            // Tout le reste (commercial / paisible / luxe) → sûr
            _ => 1,
        };
        if (s.IsPeaceful) lvl = Math.Max(1, lvl - 1);
        return Math.Clamp(lvl, 1, 5);
    }

    public static (string Label, string Color) DangerInfo(Station s)
    {
        if (s.IsRare) return ("inconnu", "magenta1");
        return Danger(s) switch
        {
            1 => ("sûr",       "green"),
            2 => ("modéré",    "yellow"),
            3 => ("risqué",    "orange1"),
            4 => ("dangereux", "red"),
            _ => ("mortel",    "red bold"),
        };
    }

    // Rendu compact réutilisable : ☠☠☠ + mot-clé coloré
    public static string DangerBadge(Station s)
    {
        var (label, color) = DangerInfo(s);
        var skulls = s.IsRare ? "?" : new string('☠', Danger(s));
        return $"[{color}]{skulls} {label}[/]";
    }

    public static long DistanceMkm(Station a, Station b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return (long)Math.Round(Math.Sqrt(dx * dx + dy * dy));
    }

    public static int FuelCost(Station from, Station to) =>
        Math.Max(1, (int)Math.Ceiling(DistanceMkm(from, to) / 100.0));
}

record UnlockCondition(
    int    Credits          = 0,
    string? Resource        = null,
    int    ResourceAmount   = 0,
    int    Reputation       = 0,
    int    Day              = 0,
    int    StationsVisited  = 0,
    int    BossesDefeated   = 0
);

record Station(
    string Name,
    List<string> Goods,
    int X, int Y,
    bool IsPeaceful      = false,
    bool IsStarting      = false,
    bool IsRare          = false,
    string Description   = "",
    UnlockCondition? Unlock      = null,
    List<string>? BannedClasses = null
);
