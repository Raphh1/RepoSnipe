namespace VoidTrader;

enum WeaponEffect
{
    None, Stun, Poison, Burn, Blind, Flee,
    Distraction, ArmorPierce, Silence, Confusion, Paralyze, Random
}

record WeaponData(
    string Name,
    int    Tier,
    int    DamageMin,
    int    DamageMax,
    int    CritChance,
    WeaponEffect Effect       = WeaponEffect.None,
    int          EffectChance = 0,
    string       EffectDesc   = "",
    int          SelfDmgChance = 0,   // % de chance de se blesser en utilisant cette arme
    int          SelfDmgMax   = 0     // dégâts max sur soi (min = SelfDmgMax/2)
);

static class WeaponPool
{
    private static readonly Random Rng = new();

    public static readonly List<WeaponData> Tier1 =
    [
        new("Clé à molette rouillée",         1,  5, 12, 5),
        new("Couteau de cuisine spatial",      1,  6, 14, 7),
        new("Pistolet à eau sous pression",    1,  5,  8, 3,  WeaponEffect.Distraction, 60, "L'ennemi perd son tour"),
        new("Le Bâton de M. Patate",           1,  5, 15, 5),
        new("Tournevis acéré",                 1,  7, 13, 6),
        new("Fourchette tritanium",            1,  6, 11, 8,  WeaponEffect.Poison, 25, "Saignement mineur (2 tours)"),
        new("Bombe à odeur",                   1,  3,  6, 2,  WeaponEffect.Flee, 30, "L'ennemi fuit"),
        new("Sarbacane de fortune",            1,  5, 10, 5,  WeaponEffect.Poison, 40, "Poison léger (2 tours)"),
        new("Extincteur galactique",           1,  8, 15, 4,  WeaponEffect.Blind, 50, "Aveugle 1 tour"),
        new("Règle en métal",                  1,  5, 12, 5),
    ];

    public static readonly List<WeaponData> Tier2 =
    [
        new("Pistolet laser standard",         2, 15, 25, 8),
        new("Matraque électrique",             2, 18, 30, 7,  WeaponEffect.Stun, 20, "Stun 1 tour"),
        new("Le Persuadeur",                   2, 20, 32, 9),
        new("Fusil à pompe galactique",        2, 22, 35, 6,  WeaponEffect.None, 0, "", SelfDmgChance: 15, SelfDmgMax: 12),
        new("Lance-patates cosmique",          2, 15, 28, 10, WeaponEffect.Confusion, 35, "L'ennemi est confus"),
        new("Pistolet paralysant Dodo Express",2, 16, 24, 7,  WeaponEffect.Paralyze, 35, "Paralysé 1 tour"),
        new("Couteau lame chauffante",         2, 20, 33, 9,  WeaponEffect.Burn, 50, "Brûlure 2 tours"),
        new("Le Chuchoteur",                   2, 18, 30, 10, WeaponEffect.Silence, 100, "Furtif — pas de -rep"),
        new("Shurikens en alliage spatial",    2, 17, 28, 12),
        new("Arbalète à carreaux laser",       2, 20, 34, 9),
        new("Canon à sel",                     2, 14, 22, 6,  WeaponEffect.Blind, 40, "-20% précision ennemi"),
        new("Poing mécanique renforcé",        2, 18, 32, 8),
    ];

    public static readonly List<WeaponData> Tier3 =
    [
        new("Fusil de précision X-7",          3, 40, 65, 13),
        new("Canon à plasma compact",          3, 38, 60, 10, WeaponEffect.Burn, 60, "Brûlure 3 tours"),
        new("Le Démolisseur 3000",             3, 45, 70,  9),
        new("Lame vibrante",                   3, 42, 65, 12, WeaponEffect.ArmorPierce, 100, "Ignore 20% armure"),
        new("Fusil à impulsion magnétique",    3, 38, 62, 10, WeaponEffect.Stun, 25, "Désarme l'ennemi"),
        new("Le Négociateur",                  3, 45, 70,  8),
        new("Canon sonique",                   3, 35, 58,  9, WeaponEffect.Blind, 60, "Désorientation ennemi"),
        new("Fléchettes Venin de Verrath",     3, 30, 50, 10, WeaponEffect.Poison, 80, "Poison fort 4 tours"),
        new("Pistolet gravitationnel",         3, 35, 55,  9, WeaponEffect.Distraction, 50, "Repousse ou attire"),
        new("L'Argument Final",                3, 48, 70, 11),
        new("Lance-flammes compact",           3, 42, 65,  8, WeaponEffect.Burn, 70, "Brûlure sévère 3 tours",  SelfDmgChance: 20, SelfDmgMax: 18),
        new("Couteau monomoléculaire",         3, 45, 68, 14, WeaponEffect.ArmorPierce, 100, "Ignore 40% armure"),
        new("Marteau de briseur d'os",         3, 50, 70,  8, WeaponEffect.Stun, 30, "Fracture -20% stats"),
    ];

    public static readonly List<WeaponData> Tier4 =
    [
        new("Canon à antimatière",             4,  80, 140, 12),
        new("Fusil de sniper orbital",         4,  90, 150, 15),
        new("La Mauvaise Nouvelle",            4,  85, 145, 11),
        new("Désintégrateur ionique",          4,  80, 130, 12, WeaponEffect.Stun, 40, "Détruit équipement ennemi"),
        new("Gatling à plasma Bonne Nuit",     4,  70, 120, 10, WeaponEffect.None, 0, "",                      SelfDmgChance: 20, SelfDmgMax: 20),
        new("Le Psychologue",                  4,  75, 125, 11, WeaponEffect.Flee, 40, "Panique — ennemi fuit"),
        new("Canon à trou noir miniaturisé",   4, 100, 150, 12, WeaponEffect.None, 0, "",                      SelfDmgChance: 15, SelfDmgMax: 35),
        new("Lame de plasma Sourire d'Adieu",  4,  85, 140, 13, WeaponEffect.ArmorPierce, 100, "Ignore 50% armure"),
        new("Le Syndicaliste",                 4,  75, 130, 10),
        new("Turbocanon à impulsion répétée",  4,  80, 135, 11),
    ];

    public static readonly List<WeaponData> Tier5 =
    [
        new("L'Extinction",                    5, 180, 300, 15),
        new("Canon à singularité",             5, 200, 300, 14, WeaponEffect.None, 0, "",                           SelfDmgChance: 25, SelfDmgMax: 50),
        new("Lame du Vide",                    5, 160, 280, 18, WeaponEffect.ArmorPierce, 100, "Ignore toute armure"),
        new("Dernière Parole",                 5, 300, 300, 100,WeaponEffect.None, 0, "",                           SelfDmgChance: 30, SelfDmgMax: 60),
        new("Le Sceptre de Raphazarus",        5, 150, 300, 20, WeaponEffect.Random, 100, "Effet aléatoire",         SelfDmgChance: 33, SelfDmgMax: 40),
        new("L'Œil de l'Emporium",            5, 200, 300, 14),
        new("Le Testament",                    5, 180, 290, 16),
        new("Lame de Kharos",                  5, 160, 280, 17, WeaponEffect.Poison, 100, "Vole 30% des dégâts en PV"),
        new("L'Insistance",                    5, 150, 250, 13, WeaponEffect.None, 0, "",                            SelfDmgChance: 15, SelfDmgMax: 25),
        new("Bombe à paradoxe",                5,   0, 500, 50, WeaponEffect.None, 0, "",                            SelfDmgChance: 50, SelfDmgMax: 150),
    ];

    public static WeaponData RollForStation(string station)
    {
        var tier = station switch
        {
            "Emporium Requiem" or "La Citadelle Écarlate" or "Fort Ossian"
                or "Le Nid des Faucons" or "La Couronne d'Eos" => Rng.Next(2) == 0 ? 4 : 3,

            "Arc Ouest Apocalypse" or "Scotty Golden North" or "La Forge Noire"
                or "Le Purgatoire" or "Fort Kharos" => 3,

            "Nexus Aldara" or "La Citadelle" or "Avant-Poste Kalem"
                or "Terminus Sud" or "Fort Ossian" => 2,

            _ => Rng.Next(2) == 0 ? 1 : 2,
        };
        return RollForTier(tier);
    }

    public static WeaponData RollForTier(int tier)
    {
        var pool = tier switch { 1 => Tier1, 2 => Tier2, 3 => Tier3, 4 => Tier4, _ => Tier5 };
        return pool[Rng.Next(pool.Count)];
    }

    // Retourne le modificateur de dégâts selon l'affinité de classe (+1.0 = normal, 0.7 = malus)
    public static float AffinityModifier(WeaponData weapon, PlayerClass cls) => cls.Name switch
    {
        "Seigneur de guerre" => 1.0f,
        "Vétéran"            => weapon.Tier >= 3 ? 1.1f : 1.0f,
        "Contrebandier"      => weapon.Tier <= 3 ? 1.1f : 0.8f,
        "Médecin"            => weapon.Tier <= 2 ? 1.0f : 0.75f,
        "Mécanicien"         => weapon.Name.Contains("clé") || weapon.Name.Contains("outil") ? 1.1f : weapon.Tier >= 3 ? 0.8f : 1.0f,
        "Hackeur"            => weapon.Effect == WeaponEffect.Stun || weapon.Effect == WeaponEffect.Paralyze ? 1.2f : weapon.Tier >= 4 ? 0.8f : 1.0f,
        "Explorateur"        => weapon.Tier is 2 or 3 ? 1.05f : 0.9f,
        "Vagabond"           => 0.7f,
        "Ferrailleur"        => weapon.Tier == 1 ? 1.0f : 0.75f,
        "Endetté"            => weapon.Tier <= 2 ? 1.0f : 0.8f,
        _                    => 1.0f,
    };
}
