namespace VoidTrader;

enum ArmorEffect { None, Thorns, Regen, StaminaBoost, Stealth, Immunity }

record ArmorData(
    string Name,
    int    Tier,
    int    Defense,        // % de réduction des dégâts reçus
    int    HpBonus,        // PV joueur max supplémentaires (permanents quand équipée)
    ArmorEffect Effect      = ArmorEffect.None,
    int         EffectValue = 0,
    string      Description = "",
    int         SellValue   = 0
);

static class ArmorPool
{
    private static readonly Random Rng = new();

    public static readonly List<ArmorData> Tier1 =
    [
        new("Veste en cuir renforcé",      1,  8,  5,  Description: "Vieille, usée, encore solide.",                SellValue: 200),
        new("Combinaison de travail",       1,  6, 10,  Description: "Faite pour les chantiers, pas pour les combats. Ça dépanne.", SellValue: 180),
        new("Gilet de protection basique", 1, 10,  0,  Description: "Standard. Sans surprise. Efficace.",           SellValue: 250),
        new("Armure de scavenger",         1,  7,  8,  Description: "Assemblée avec des pièces récupérées. Ça tient.", SellValue: 150),
        new("Manteau blindé",              1,  9,  5,  Description: "Long, lourd, mais les balles le traversent moins vite.", SellValue: 220),
    ];

    public static readonly List<ArmorData> Tier2 =
    [
        new("Gilet balistique",            2, 18, 15,  Description: "Conçu pour les projectiles. Efficace sur les balles.",       SellValue: 500),
        new("Exosquelette léger",          2, 15, 20,  ArmorEffect.StaminaBoost, 10, "Stamina +10/tour",                          SellValue: 600),
        new("Armure composite légère",     2, 20, 10,  Description: "Bon compromis poids/protection.",                            SellValue: 550),
        new("Combinaison tactique",        2, 16, 18,  Description: "Utilisée par les mercenaires de second rang.",                SellValue: 480),
        new("Veste à plaques",             2, 19, 12,  Description: "Plaques métalliques cousues dans du cuir épais.",             SellValue: 520),
        new("Armure furtive",              2, 12, 15,  ArmorEffect.Stealth, 1, "Réduit les pertes de réputation en combat",        SellValue: 650),
    ];

    public static readonly List<ArmorData> Tier3 =
    [
        new("Armure militaire standard",   3, 28, 25,  Description: "Issue des surplus militaires. Fiable, lourde.",              SellValue: 1200),
        new("Scaphandre renforcé",         3, 25, 35,  Description: "Conçu pour le vide spatial mais utile en combat.",           SellValue: 1100),
        new("Armure à épines",             3, 22, 20,  ArmorEffect.Thorns, 15, "Renvoie 15% des dégâts reçus à l'attaquant",      SellValue: 1400),
        new("Plastron régénérant",         3, 24, 30,  ArmorEffect.Regen, 5, "Régénère 5 PV joueur par tour en combat",           SellValue: 1500),
        new("Armure de pirate",            3, 26, 28,  Description: "Lourde, abîmée, et ça se voit. Efficace quand même.",        SellValue: 1000),
    ];

    public static readonly List<ArmorData> Tier4 =
    [
        new("Armure militaire lourde",     4, 38, 40,  Description: "Réservée aux élites. Chaque coup amorti.",                   SellValue: 2500),
        new("Exosquelette de combat",      4, 35, 45,  ArmorEffect.StaminaBoost, 20, "Stamina +20/tour",                          SellValue: 3000),
        new("Armure à épines avancée",     4, 32, 35,  ArmorEffect.Thorns, 25, "Renvoie 25% des dégâts reçus",                    SellValue: 3200),
        new("Carapace régénérante",        4, 36, 40,  ArmorEffect.Regen, 10, "Régénère 10 PV joueur par tour",                   SellValue: 3500),
        new("Armure de l'Emporium",        4, 40, 30,  Description: "Fabriquée exclusivement pour les gardes de l'Emporium.",     SellValue: 2800),
    ];

    public static readonly List<ArmorData> Tier5 =
    [
        new("Exo-armure légendaire",       5, 50, 60,  Description: "Le summum de la protection connue. Impénétrable ou presque.", SellValue: 6000),
        new("Armure du Vide",              5, 45, 70,  ArmorEffect.Immunity, 1, "Immunité à un coup fatal par combat",             SellValue: 7000),
        new("Carapace de Kharos",          5, 48, 55,  ArmorEffect.Thorns, 40, "Renvoie 40% des dégâts reçus",                    SellValue: 7500),
        new("Armure Régénératrice Suprême",5, 42, 65,  ArmorEffect.Regen, 20, "Régénère 20 PV joueur par tour",                   SellValue: 8000),
        new("L'Intouchable",               5, 55, 50,  Description: "Personne ne sait comment elle a été fabriquée.",              SellValue: 9000),
    ];

    public static ArmorData RollForStation(string station)
    {
        var tier = station switch
        {
            "Emporium Requiem" or "La Citadelle Écarlate" or "Fort Ossian"
                or "Le Nid des Faucons" or "La Couronne d'Eos" => Rng.Next(2) == 0 ? 4 : 3,
            "Arc Ouest Apocalypse" or "La Forge Noire" or "Le Purgatoire"
                or "Fort Kharos" or "La Forge des Damnés" => 3,
            "Nexus Aldara" or "La Citadelle" or "Avant-Poste Kalem"
                or "Fort Ossian" or "Station Terminus Noir" => 2,
            _ => Rng.Next(2) == 0 ? 1 : 2,
        };
        return RollForTier(tier);
    }

    public static ArmorData RollForTier(int tier)
    {
        var pool = tier switch { 1 => Tier1, 2 => Tier2, 3 => Tier3, 4 => Tier4, _ => Tier5 };
        return pool[Rng.Next(pool.Count)];
    }
}
