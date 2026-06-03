namespace VoidTrader;

/// <summary>
/// État d'une station — elle change selon les actions du joueur.
/// Cela rend chaque revisit unique et dépendant des choix passés.
/// </summary>
record StationState(
    string Name,
    int LastVisitDay = 0,
    BossState BossStatus = BossState.Active,
    FactionId? ControlledBy = null,        // Qui contrôle la station maintenant
    int ReputationWithStation = 0,         // Réputation locale (indépendant du global)
    List<string>? EventsHappened = null,  // "Boss_Defeated", "Raid_By_Faucons", "Liberated", etc.
    bool IsLocked = false,                // Zone fermée ? (par faction, par boss, etc.)
    string? LockReason = null,            // Pourquoi ? "Controlled by Faucons", "Boss Too Powerful", etc.
    string? CurrentGovernor = null       // PNJ qui gouverne la station maintenant
)
{
    // Retourne true si la station a changé depuis la dernière visite
    public bool HasChanged(int daysSinceVisit) => daysSinceVisit > 3;

    // Description du changement pour le joueur
    public string DescribeChanges() => (EventsHappened?.Count ?? 0) switch
    {
        0 => "La station semble inchangée.",
        1 => $"Quelque chose s'est passé : {EventsHappened![0]}.",
        _ => $"Plusieurs choses ont changé ici. Ambiance tendue.",
    };

    // Ajoute un événement à l'historique
    public StationState RecordEvent(string eventTag)
    {
        var newEvents = new List<string>(EventsHappened ?? []);
        newEvents.Add(eventTag);
        return this with { EventsHappened = newEvents };
    }

    // Marque la station comme contrôlée par une faction
    public StationState Conquer(FactionId faction)
        => this with 
        { 
            ControlledBy = faction,
            IsLocked = false,
            LockReason = null,
            EventsHappened = new(EventsHappened ?? []) { "Conquered" }
        };

    // Verrouille une station (inaccessible)
    public StationState Lock(string reason)
        => this with { IsLocked = true, LockReason = reason };

    // Déverrouille
    public StationState Unlock()
        => this with { IsLocked = false, LockReason = null };

    // Met à jour le statut du boss
    public StationState SetBossStatus(BossState status)
        => this with { BossStatus = status };
}

/// <summary>
/// Gestionnaire global pour l'état dynamique des stations.
/// </summary>
static class StationStates
{
    private static readonly Dictionary<string, StationState> _states = new();

    // Initialise une station si elle n'existe pas
    public static StationState GetOrInit(string stationName)
    {
        if (!_states.TryGetValue(stationName, out var state))
        {
            state = new StationState(stationName);
            _states[stationName] = state;
        }
        return state;
    }

    // Met à jour l'état d'une station
    public static void Update(StationState newState)
        => _states[newState.Name] = newState;

    // Vérifie si un joueur peut entrer dans une station
    public static bool CanEnter(string stationName, GameState playerState)
    {
        var state = GetOrInit(stationName);
        
        // Verrouillée ? Vérifier si le joueur a accès
        if (state.IsLocked)
        {
            // Si contrôlée par une faction et le joueur en est membre, accès ok
            if (state.ControlledBy.HasValue && playerState.Faction == state.ControlledBy)
                return true;
            
            // Sinon, bloqué
            return false;
        }

        return true;
    }

    // Retourne un message sur l'accès/la situation
    public static string GetAccessMessage(string stationName, GameState playerState)
    {
        var state = GetOrInit(stationName);
        
        if (!state.IsLocked)
            return state.ControlledBy.HasValue 
                ? $"{stationName} — Contrôlée par {Factions.Info[state.ControlledBy.Value].Name}."
                : $"{stationName} — Station libre.";

        return state.LockReason ?? "Cette station est inaccessible pour le moment.";
    }

    // Change qui contrôle une station
    public static void SetController(string stationName, FactionId faction)
    {
        var state = GetOrInit(stationName);
        Update(state.Conquer(faction));
    }

    // Verrouille une station
    public static void LockStation(string stationName, string reason)
    {
        var state = GetOrInit(stationName);
        Update(state.Lock(reason));
    }
}
