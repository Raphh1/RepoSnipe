namespace VoidTrader;

enum FactionId { None, Faucons, Emporium, Gardiens, Culte }

static class Factions
{
    // Nom, description courte, bonus d'appartenance
    public static readonly Dictionary<FactionId, (string Name, string Desc, string Bonus)> Info = new()
    {
        [FactionId.Faucons]  = ("Les Faucons Noirs",   "Pirates organisés. Efficaces. Impitoyables.",             "Pirates neutres en voyage, réduction sur armes T3+"),
        [FactionId.Emporium] = ("L'Emporium",           "Réseau marchand puissant. Argent avant tout.",            "-15% achat, +15% vente, accès deals exclusifs"),
        [FactionId.Gardiens] = ("Les Gardiens Écarlates","Ordre ancien. Moralité rigide. Réputation massive.",      "+50 rep à chaque acte héroïque, stations sûres gratuites"),
        [FactionId.Culte]    = ("Le Culte du Vide",     "Secte spatiale mystérieuse. Pouvoir étrange. Dangereux.", "Événements WTF amplifiés, accès lieux interdits"),
    };
}
