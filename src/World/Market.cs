namespace VoidTrader;

static class Market
{
    private static readonly Random Rng = new();

    private static readonly Dictionary<string, int> BasePrices = new()
    {
        // Essentiels
        ["Cellules de carburant"] = 150,
        ["Eau"]                   = 60,
        ["Rations"]               = 90,
        ["Vivres"]                = 120,
        // Matériaux
        ["Ferraille"]             = 80,
        ["Minerai brut"]          = 140,
        ["Pièces techniques"]     = 400,
        ["Pièces détachées"]      = 350,
        // Médical / Bio
        ["Médicaments"]           = 280,
        ["Plantes médicinales"]   = 380,
        ["Objets expérimentaux"]  = 900,
        // Valeur haute
        ["Armes"]                 = 700,
        ["Explosifs"]             = 600,
        ["Minerai exotique"]      = 950,
        ["Matière noire"]         = 1800,
        ["Or"]                    = 2000,
        ["Artefacts"]             = 1200,
        ["Cartes stellaires"]     = 700,
        // Spécial
        ["Marchandises illégales"]= 800,
    };

    public static int GetPrice(string item, string? station = null, int day = 0)
    {
        var base_ = BasePrices.GetValueOrDefault(item, 100);
        var variance = Rng.Next(-30, 31);
        var price    = Math.Max(10, base_ + base_ * variance / 100);

        if (station != null)
        {
            var mod = StationModifier(item, station);
            price   = (int)(price * mod);
        }

        // Inflation : +2% par jour au-delà du jour 10, plafonnée à +80%
        if (day > 10)
        {
            var infDays = Math.Min(day - 10, 40); // 40 jours max = +80%
            price = (int)(price * (1f + infDays * 0.02f));
        }

        return Math.Max(10, price);
    }

    // Modificateur de prix selon la station — produits locaux et demandes spécifiques.
    // < 1.0 = bien produit ici (bon marché), > 1.0 = bien rare ou très demandé.
    public static float StationModifier(string item, string station) => station switch
    {
        // ── Stations industrielles → Ferraille et pièces bon marché ──────────
        "Forge Alpha" or "La Ferronnerie" or "L'Arc du Pic de l'Est"
            => item is "Ferraille" or "Pièces techniques" or "Pièces détachées" or "Cellules de carburant" ? 0.65f
             : item is "Vivres" or "Médicaments" or "Plantes médicinales" ? 1.45f
             : 1.0f,

        // ── Stations agricoles/biologiques → Vivres et plantes bon marché ────
        "Esmeralda" or "Colonie Perséphone" or "L'Arche de Sélène"
            => item is "Vivres" or "Plantes médicinales" or "Eau" or "Rations" ? 0.60f
             : item is "Armes" or "Explosifs" or "Marchandises illégales" ? 2.20f
             : 1.0f,

        // ── Sanctuaires médicaux → Médicaments bon marché ────────────────────
        "Le Sanctuaire des Dérives"
            => item is "Médicaments" or "Plantes médicinales" ? 0.55f
             : item is "Armes" or "Explosifs" ? 1.80f
             : 1.0f,

        // ── Stations minières → Minerais bon marché ───────────────────────────
        "Les Puits de Noctis" or "Station Rocaille"
            => item is "Minerai brut" or "Minerai exotique" or "Ferraille" ? 0.55f
             : item is "Médicaments" or "Vivres" ? 1.50f
             : 1.0f,

        // ── Stations militaires → Armes moins chères ─────────────────────────
        "La Citadelle" or "Fort Kharos" or "Fort Ossian" or "Avant-Poste Kalem"
            => item is "Armes" or "Armures" or "Explosifs" ? 0.70f
             : item is "Vivres" or "Plantes médicinales" ? 1.35f
             : 1.0f,

        // ── Marchés noirs → Marchandises illégales moins chères ──────────────
        "Arc Ouest Apocalypse" or "Les Bas-Fonds de Vega" or "Port des Brumes"
          or "Le Nid des Faucons" or "La Forge des Damnés"
            => item is "Marchandises illégales" or "Armes" ? 0.65f
             : item is "Or" or "Médicaments" ? 1.60f
             : 1.0f,

        // ── Stations de luxe → Or bon marché, biens de base chers ───────────
        "Emporium Requiem" or "Scotty Golden North" or "La Couronne d'Eos" or "Station Belvédère"
            => item is "Or" or "Artefacts" or "Cartes stellaires" ? 0.75f
             : item is "Ferraille" or "Marchandises illégales" ? 2.50f
             : 1.0f,

        // ── Stations scientifiques → Objets expérimentaux bon marché ─────────
        "La Bulle" or "L'Académie Stellaire" or "Les Abysses de Velkor" or "Sanctum Machina"
            => item is "Objets expérimentaux" or "Plantes médicinales" ? 0.60f
             : item is "Armes" or "Explosifs" ? 1.70f
             : 1.0f,

        // ── Matière noire → aux Puits et Abysses ─────────────────────────────
        "Les Abysses de Velkor" or "Le Vaisseau Fantôme Errant"
            => item is "Matière noire" ? 0.65f : 1.0f,

        _ => 1.0f,
    };

    // Texte d'information économique pour une station (affiché au marché).
    public static string? EconomicNote(string station) => station switch
    {
        "Forge Alpha" or "La Ferronnerie" or "L'Arc du Pic de l'Est"
            => "[grey dim]Zone industrielle — pièces et ferraille produits localement. Vivres rares.[/]",
        "Esmeralda" or "Colonie Perséphone" or "L'Arche de Sélène"
            => "[grey dim]Zone agricole — vivres et plantes en surplus. Armement très cher.[/]",
        "Le Sanctuaire des Dérives"
            => "[grey dim]Station médicale — médicaments produits ici. Armes prohibées.[/]",
        "Les Puits de Noctis" or "Station Rocaille"
            => "[grey dim]Station minière — minerais bon marché. Vivres et soins rares.[/]",
        "La Citadelle" or "Fort Kharos" or "Fort Ossian"
            => "[grey dim]Zone militaire — armement produit ici. Vivres importés.[/]",
        "Arc Ouest Apocalypse" or "Les Bas-Fonds de Vega" or "Port des Brumes"
            => "[grey dim]Marché noir — marchandises illégales en circulation. Or et médicaments rares.[/]",
        "Emporium Requiem" or "Scotty Golden North" or "La Couronne d'Eos"
            => "[grey dim]Station de luxe — Or et artefacts échangés ici. Ferraille refusée.[/]",
        "La Bulle" or "L'Académie Stellaire" or "Les Abysses de Velkor"
            => "[grey dim]Station scientifique — objets expérimentaux fabriqués ici. Armes mal vues.[/]",
        _ => null,
    };

    // Modificateurs d'achat selon la réputation — les marchands te font payer plus cher si t'es un criminel
    public static float BuyModifier(int reputation) => reputation switch
    {
        >= 1000 => 0.80f,   // Légende     : -20%
        >= 500  => 0.90f,   // Respecté    : -10%
        >= 0    => 1.00f,   // Inconnu     : normal
        >= -500 => 1.20f,   // Criminel    : +20%
        _       => 1.40f,   // Ennemi public: +40%
    };

    public static float FactionSellBonus(FactionId faction) => faction == FactionId.Emporium ? 1.15f : 1.0f;

    // Modificateurs de vente — les marchands te proposent moins si ta tête est mise à prix
    public static float SellModifier(int reputation) => reputation switch
    {
        >= 1000 => 1.20f,   // Légende     : +20%
        >= 500  => 1.10f,   // Respecté    : +10%
        >= 0    => 1.00f,   // Inconnu     : normal
        >= -500 => 0.80f,   // Criminel    : -20%
        _       => 0.60f,   // Ennemi public: -40%
    };

    public static string ReputationMarketNote(int reputation) => reputation switch
    {
        >= 1000 => "[green]Ta légende te précède. Les marchands font des efforts.[/]",
        >= 500  => "[green]Ta réputation t'ouvre des portes. Petite remise.[/]",
        >= 0    => "",
        > -500  => "[red]Les marchands se méfient. Prix majorés, offres réduites.[/]",
        _       => "[red bold]Ta tête est mise à prix. Les marchands te voient venir de loin.[/]",
    };

    public static bool Exists(string item) => BasePrices.ContainsKey(item);
}
