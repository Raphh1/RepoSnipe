namespace VoidTrader;

enum ClassTier { Bad, Balanced, Good }

record PlayerClass(
    string Name,
    string Description,
    ClassTier Tier,
    int StartingCredits,
    int MaxFuel,
    string StartingStation = "Port Méridien",
    bool CargoDegrades       = false,  // Junker: 30% chance to lose a cargo item on travel
    int  DailyDebt           = 0,      // Debtor: credits lost per day
    int  TravelCreditCost    = 0,      // Addict: credits lost per travel
    bool CursedEvents        = false,  // Cursed: positive events have 50% chance to fizzle
    int  BuyDiscountPercent  = 0,      // Smuggler: discount on purchases
    bool PiratesDoubled      = false,  // Smuggler: pirates appear twice as often
    bool CannotBuyWeapons    = false,  // Heir: can't buy Weapons
    int  PeriodicIncome      = 0,      // Heir: credits received every 5 days
    bool SeesPrices          = false,  // Hacker: sees prices at destination before traveling
    bool AutoKillsPirates    = false,  // Warlord: pirate events auto-resolve in your favor
    bool PeacefulBan         = false,  // Warlord: peaceful stations refuse to sell
    bool MedicBonus          = false,  // Medic: Medicines sell for 50% more
    bool NeutralEventsBoost  = false   // Explorer: neutral events more frequent
)
{
    public static readonly List<PlayerClass> All =
    [
        // --- Mauvaises ---
        new("Vagabond",   "Parti de rien. Finira de rien.",
            ClassTier.Bad,  StartingCredits: 500,  MaxFuel: 3,
            StartingStation: "La Carcasse"),

        new("Ferrailleur", "Ton vaisseau se désintègre en temps réel.",
            ClassTier.Bad,  StartingCredits: 800,  MaxFuel: 5,  CargoDegrades: true,
            StartingStation: "La Carcasse"),

        new("Endetté",    "Quelqu'un veut récupérer son argent. Chaque. Jour.",
            ClassTier.Bad,  StartingCredits: 1200, MaxFuel: 5,  DailyDebt: 50,
            StartingStation: "La Carcasse"),

        new("Accro",      "T'as des habitudes coûteuses. C'est pas négociable.",
            ClassTier.Bad,  StartingCredits: 1000, MaxFuel: 5,  TravelCreditCost: 100,
            StartingStation: "La Carcasse"),

        new("Maudit",     "L'univers ne t'aime tout simplement pas.",
            ClassTier.Bad,  StartingCredits: 1000, MaxFuel: 5,  CursedEvents: true,
            StartingStation: "La Carcasse"),

        // --- Équilibrées ---
        new("Marchand",   "Rien de spécial. Un travail honnête.",
            ClassTier.Balanced, StartingCredits: 1000, MaxFuel: 5,
            StartingStation: "Port Méridien"),

        new("Mécanicien", "Connaît les moteurs. Fauché comme les blés.",
            ClassTier.Balanced, StartingCredits: 600,  MaxFuel: 8,
            StartingStation: "Forge Alpha"),

        new("Explorateur","Tout vu. Chanceux dans les moments calmes.",
            ClassTier.Balanced, StartingCredits: 900,  MaxFuel: 6,  NeutralEventsBoost: true,
            StartingStation: "Port Méridien"),

        new("Médecin",    "Les gens ont besoin de toi. Surtout quand tu vends des Médicaments.",
            ClassTier.Balanced, StartingCredits: 900,  MaxFuel: 5,  MedicBonus: true,
            StartingStation: "Port Méridien"),

        // --- Bonnes ---
        new("Contrebandier", "Bons prix. Ennemis dangereux.",
            ClassTier.Good, StartingCredits: 1000, MaxFuel: 5,  BuyDiscountPercent: 20, PiratesDoubled: true,
            StartingStation: "Les Bas-Fonds de Vega"),

        new("Vétéran",    "Aguerri et bien financé.",
            ClassTier.Good, StartingCredits: 2000, MaxFuel: 6,
            StartingStation: "La Citadelle"),

        new("Héritier",   "Vieux argent. Éducation pacifiste.",
            ClassTier.Good, StartingCredits: 1000, MaxFuel: 5,  CannotBuyWeapons: true, PeriodicIncome: 300,
            StartingStation: "La Citadelle"),

        new("Hackeur",    "L'information, c'est la monnaie.",
            ClassTier.Good, StartingCredits: 900,  MaxFuel: 5,  SeesPrices: true,
            StartingStation: "La Citadelle"),

        new("Seigneur de guerre", "Craint partout. Bienvenu nulle part de paisible.",
            ClassTier.Good, StartingCredits: 1500, MaxFuel: 5,  AutoKillsPirates: true, PeacefulBan: true,
            StartingStation: "Fort Kharos"),
    ];

    public static PlayerClass Random()
    {
        var rng = new Random();
        return All[rng.Next(All.Count)];
    }
}
