namespace VoidTrader;

/// <summary>
/// État d'un boss de station — pour voir comment il évolue après une rencontre.
/// </summary>
public enum BossState
{
    Active,      // Jamais combattu, dangereux
    Defeated,    // Vaincu une fois, peut revenir plus tard
    Corrupted,   // S'est allié à une faction, venge le joueur
    Allied,      // Le joueur a négocié une trêve
    Dead,        // Mort, un successeur peut prendre le relais
}

/// <summary>
/// Enregistrement d'une mort antérieure — pour les rumeurs post-mortem.
/// </summary>
record PriorDeath(
    int DayOfDeath,
    string Location,
    string Cause,              // "Murdered by X", "Starvation", "Boss Battle", etc.
    int Credits,
    int Reputation
);

/// <summary>
/// Options de conséquences de choix.
/// </summary>
public enum ChoiceConsequence
{
    MoralChoice,       // Mercy / Ruthlessness
    FactionChoice,     // Alliance with faction
    RivalryStart,      // Creating a rival / enemy
    AllyStart,         // Making an ally / friend
    SecretDiscovered,  // Learning hidden lore
    CriminalAct,       // Breaking law / morality
    HeroicAct,         // Heroic deed
}
