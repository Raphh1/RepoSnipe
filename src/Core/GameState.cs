namespace VoidTrader;

class GameState
{
    public PlayerClass Class { get; }

    // Ressources
    public int Credits  { get; set; }
    public int Fuel     { get; set; }
    public int MaxFuel  => Class.MaxFuel + BonusMaxFuel;
    public int Day      { get; set; } = 1;

    // Horloge — un jour passe au bout de Clock.ActionsPerDay actions « de terrain »
    public int ActionsToday { get; set; } = 0;

    // Profondeur d'exploration dans la zone courante (reset à chaque voyage).
    // Plus elle est haute, plus les monstres sont puissants.
    public int ZoneDepth { get; set; } = 0;

    // PV vaisseau
    public int ShipHp    { get; set; }
    public int ShipMaxHp { get; set; } = 100;

    // PV joueur (à pied dans les stations/planètes)
    public int PlayerHp    { get; set; }
    public int PlayerMaxHp { get; set; } = 100;

    // Réputation
    public int Reputation { get; set; } = 0;
    public Dictionary<string, int> FactionReputation { get; } = new();

    // Localisation
    public string CurrentStation { get; set; }

    // Cargaison
    public Inventory Cargo { get; } = new();

    // Armes
    public List<WeaponData> Weapons         { get; } = new();
    public WeaponData?       EquippedWeapon  { get; set; }

    // Armures
    public List<ArmorData>  Armors          { get; } = new();
    public ArmorData?        EquippedArmor   { get; set; }

    // Stamina (combat)
    public int Stamina    { get; set; }
    public int MaxStamina => Class.Name switch
    {
        "Seigneur de guerre" => 150,
        "Vétéran"            => 120,
        "Contrebandier"      => 100,
        "Vagabond"           => 60,
        "Ferrailleur"        => 70,
        _                    => 100,
    };

    // Faction
    public FactionId Faction          { get; set; } = FactionId.None;
    public bool      IsDoubleAgent    { get; set; } = false;
    public string?   SecondFaction    { get; set; }

    // Addiction (drogues/alcool)
    public int AddictionLevel    { get; set; } = 0;  // 0 = aucune, 1-3 = légère, 4-6 = modérée, 7+ = sévère
    public int AddictionDaysSinceDose { get; set; } = 0;

    public string AddictionLabel => AddictionLevel switch
    {
        0       => "",
        <= 3    => "[yellow]Accoutumance légère[/]",
        <= 6    => "[orange1]Dépendance modérée[/]",
        _       => "[red bold]Dépendance sévère[/]",
    };

    public int AddictionDailyCost => AddictionLevel switch
    {
        0    => 0,
        <= 3 => 80,
        <= 6 => 200,
        _    => 450,
    };

    // Suivi objectifs
    public HashSet<string>  CompletedObjectives  { get; } = new();
    public HashSet<string>  VisitedStations      { get; } = new();
    public HashSet<string>  NpcsMet              { get; } = new();
    public int               InterrogationsSurvived { get; set; } = 0;
    public int               PrisonEscapes          { get; set; } = 0;
    public int               BossesDefeated         { get; set; } = 0;
    public int               FactionMissions        { get; set; } = 0;

    // État du joueur
    public bool IsImprisoned { get; set; } = false;
    public bool IsInCombat   { get; set; } = false;
    public bool IsDead       { get; set; } = false;   // mort réelle → fin de run
    public string? DeathCause { get; set; } = null;

    // ──────────────────────────────────────────────────────────────────────
    // SYSTÈME DE CONSÉQUENCES NARRATIVES
    // ──────────────────────────────────────────────────────────────────────

    // NPCs persistants — qui se souviennent du joueur
    // Dictionary<NpcId, NpcMemory>
    public Dictionary<string, object> PersistentNpcs { get; } = new();  // Sera typé PersistentNpc à runtime

    // Choix moraux et alignement
    public enum MoralAlignment { Neutral = 0, Merciful = 1, Ruthless = 2, Lawful = 4, Criminal = 8 }
    public MoralAlignment Alignment { get; set; } = MoralAlignment.Neutral;

    // Tags de choix historiques — pour les callbacks narratifs
    public HashSet<string> ChoiceTags { get; } = new();  // "Spared_Fugitive", "Betrayed_X", "Sided_With_Faucons", etc.

    // Faction relationship — plus granulaire que "Faction"
    public Dictionary<FactionId, int> FactionStanding { get; set; }

    // NPCs typés
    public Dictionary<string, PersistentNpc> KnownNpcs { get; } = new();

    // Stalker system
    public int     StalkerLevel       { get; set; } = 0;
    public string? StalkerName        { get; set; }
    public string? StalkerObsession   { get; set; }
    public int     StalkerActionsLeft { get; set; } = 0;

    // Arcs narratifs
    public HashSet<string>       ActiveArcs    { get; } = new();
    public HashSet<string>       CompletedArcs { get; } = new();
    public Dictionary<string,int> ArcProgress  { get; } = new();

    // Secrets débloqués
    public HashSet<string> UnlockedSecrets { get; } = new();

    // États des stations — mémoire de ce qui s'est passé dans chaque station
    public Dictionary<string, StationState> StationStates { get; } = new();

    public StationState GetStationState(string name)
    {
        if (!StationStates.TryGetValue(name, out var s))
        {
            s = new StationState(name);
            StationStates[name] = s;
        }
        return s;
    }

    public void UpdateStationState(string name, StationState newState)
        => StationStates[name] = newState;

    // Boss de stations vaincus
    public HashSet<string> StationBossesBeaten { get; } = new();

    // Nexus / Seigneur de l'Espace
    public HashSet<string> RalliedStations    { get; } = new();
    public int             StationPiecesRallied { get; set; } = 0;

    // Titre de chef de faction
    public bool IsFactionLeader { get; set; } = false;

    // Traits spéciaux
    public bool IsCannibalistic { get; set; } = false;

    // Améliorations vaisseau (atelier)
    public int BonusMaxFuel   { get; set; } = 0;
    public int BonusNavRange  { get; set; } = 0;

    // Rivaux et alliés durables
    public HashSet<string> Rivals { get; } = new();
    public HashSet<string> Allies { get; } = new();

    // Boss state — track si beaten, vengeful, corrupted, etc.
    public Dictionary<string, object> StationBossStates { get; } = new();  // Will be typed BossState at runtime

    // Système de quêtes
    public List<Quest>      ActiveQuests      { get; } = new();
    public HashSet<string>  CompletedQuestIds { get; } = new();

    // Quêtes refusées — pour conséquences
    public HashSet<string> RefusedQuestIds { get; } = new();

    // Arcs narratifs échoués définitivement cette run
    public HashSet<string> FailedArcs { get; } = new();

    // Stations "conquered" — marquées par faction du joueur
    public Dictionary<string, FactionId> ConqueredStations { get; } = new();

    // Morts précédentes — pour les rumeurs post-mortem
    public List<object> PriorDeaths { get; } = new();  // Will be typed PriorDeath at runtime

    public GameState()
    {
        Class          = PlayerClass.Random();
        Credits        = Class.StartingCredits;
        Fuel           = Class.MaxFuel;
        ShipHp         = ShipMaxHp;
        PlayerHp       = PlayerMaxHp;
        Stamina        = MaxStamina;
        CurrentStation = Class.StartingStation;

        // Initialiser FactionStanding ici au lieu d'inline
        FactionStanding = new Dictionary<FactionId, int>
        {
            { FactionId.Faucons, 0 },
            { FactionId.Emporium, 0 },
            { FactionId.Gardiens, 0 },
            { FactionId.Culte, 0 },
        };
    }

    // Applique un gain de réputation avec bonus Gardiens
    public void AddReputation(int amount)
    {
        if (Faction == FactionId.Gardiens && amount > 0)
            amount = (int)(amount * 1.5f);
        Reputation += amount;
    }

    public string FactionLabel => Faction == FactionId.None ? "" : $" [{Factions.Info[Faction].Name}]";

    public string ReputationLabel => Reputation switch
    {
        >= 1000 => "Légende",
        >= 500  => "Respecté",
        >= 100  => "Connu",
        >= 0    => "Inconnu",
        >= -500 => "Criminel notoire",
        _       => "Ennemi public"
    };
}

class Inventory
{
    private readonly Dictionary<string, int> _items = new();

    public void Add(string item, int qty) =>
        _items[item] = _items.GetValueOrDefault(item) + qty;

    public bool Remove(string item, int qty)
    {
        if (_items.GetValueOrDefault(item) < qty) return false;
        _items[item] -= qty;
        if (_items[item] == 0) _items.Remove(item);
        return true;
    }

    public int Get(string item) => _items.GetValueOrDefault(item);

    public IReadOnlyDictionary<string, int> All => _items;
}
