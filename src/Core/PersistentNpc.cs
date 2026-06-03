namespace VoidTrader;

/// <summary>
/// Représente un NPC qui se souvient de ses interactions avec le joueur.
/// C'est LA fondation du système de conséquences.
/// </summary>
record PersistentNpc(
    string Id,                      // Identifiant unique
    string Name,                    // Nom affiché
    string Station,                 // Station d'origine
    int FirstMetDay = 0,           // Jour de première rencontre
    int TimesMetCount = 0,         // Combien de fois rencontré
    int ReputationDelta = 0,       // Réputation joueur deltas cumulés vis-à-vis de ce NPC
    bool QuestRefused = false,     // A refusé une quête du joueur
    FactionId? OfferFaction = null,// Quelle faction il représente (nullable)
    bool IsAlly = false,           // Devient allié après certaines actions
    bool IsEnemy = false,          // Devient ennemi après certaines actions
    List<string>? PriorChoices = null  // Tags de choix précédents ("Showed Mercy", "Betrayed", etc)
)
{
    // Calcule comment cet NPC réagit au joueur
    public NpcReaction GetReaction(GameState state, int currentDay)
    {
        // Premier contact = neutre
        if (TimesMetCount == 0) return NpcReaction.Neutral;
        
        // Ennemi = hostile
        if (IsEnemy) return NpcReaction.Hostile;
        
        // Allié = positif
        if (IsAlly) return NpcReaction.Ally;
        
        // Si quête refusée = méfiant
        if (QuestRefused) return NpcReaction.Wary;
        
        // Réputation delta positive = favorable
        if (ReputationDelta > 10) return NpcReaction.Friendly;
        if (ReputationDelta > 5) return NpcReaction.Warm;
        
        // Réputation delta négative = froid
        if (ReputationDelta < -10) return NpcReaction.Cold;
        if (ReputationDelta < -5) return NpcReaction.Distant;
        
        // Par défaut, selon nombre de rencontres
        return TimesMetCount > 2 ? NpcReaction.Familiar : NpcReaction.Neutral;
    }

    // Retourne une phrase d'accueil selon la réaction
    public string GetGreeting(NpcReaction reaction) => reaction switch
    {
        NpcReaction.Neutral => $"{Name} t'observe. Pas de réaction particulière.",
        NpcReaction.Familiar => $"{Name} hoche la tête en te voyant. 'Toi encore.'",
        NpcReaction.Warm => $"{Name} te sourit. 'Content de te revoir.'",
        NpcReaction.Friendly => $"{Name} s'éclaire. 'Ah, c'est toi ! Je m'en souvenais bien.'",
        NpcReaction.Ally => $"{Name} t'accueille chaleureusement. 'Mon allié. C'est du renfort.'",
        NpcReaction.Distant => $"{Name} te regarde de loin, l'air peu intéressé.",
        NpcReaction.Cold => $"{Name} te lance un regard glacial. 'Toi.'",
        NpcReaction.Wary => $"{Name} hésite. 'Je ne suis pas sûr que ce soit une bonne idée, toi et moi.'",
        NpcReaction.Hostile => $"{Name}'s eyes narrow dangerously. 'Je croyais t'avoir dit de plus revenir.'",
        _ => $"{Name} te fixe d'un air indéchiffrable.",
    };

    // Enregistre une nouvelle rencontre et met à jour l'historique
    public PersistentNpc RecordMeeting(int reputationDelta = 0, string? choiceTag = null)
    {
        var newChoices = new List<string>(PriorChoices ?? []);
        if (choiceTag != null) newChoices.Add(choiceTag);
        
        return this with
        {
            TimesMetCount = TimesMetCount + 1,
            ReputationDelta = ReputationDelta + reputationDelta,
            PriorChoices = newChoices
        };
    }

    // Marque comme allié
    public PersistentNpc BecomeAlly() => this with { IsAlly = true, IsEnemy = false };

    // Marque comme ennemi
    public PersistentNpc BecomeEnemy() => this with { IsEnemy = true, IsAlly = false };

    // Marque quête refusée
    public PersistentNpc RefusedQuest() => this with { QuestRefused = true };
}

/// <summary>
/// Comment un NPC réagit au joueur.
/// </summary>
enum NpcReaction
{
    Neutral,    // Première rencontre, pas d'histoire
    Familiar,   // Reconnaît le joueur, indifférent
    Warm,       // Positif + petite histoire
    Friendly,   // Positif + bonne histoire
    Ally,       // Équipier / Allié de confiance
    Distant,    // Négatif léger
    Cold,       // Négatif modéré
    Wary,       // Méfiant (quête refusée)
    Hostile,    // Très négatif, considère comme menace
}
