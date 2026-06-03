namespace VoidTrader;

// Gestion du temps. Un jour passe au bout de ActionsPerDay actions « de terrain »
// (explorer, traîner, chercher du travail, casino, dealer...). Le voyage compte
// pour un jour plein à lui seul.
static class Clock
{
    public const int ActionsPerDay = 3;

    // Consomme une action de terrain. Déclenche un nouveau jour si le quota est atteint.
    public static void SpendAction(GameState state)
    {
        if (state.PlayerHp <= 0 || state.ShipHp <= 0) return;
        state.ActionsToday++;
        if (state.ActionsToday >= ActionsPerDay)
        {
            state.ActionsToday = 0;
            AdvanceDay(state);
        }

        // Le stalker compte les actions du joueur — chaque action le rapproche
        if (state.StalkerLevel > 0 && state.StalkerActionsLeft > 0)
            state.StalkerActionsLeft--;
    }

    // Le voyage avance d'un jour plein et réinitialise le compteur d'actions.
    public static void NewDayFromTravel(GameState state)
    {
        state.ActionsToday = 0;
        AdvanceDay(state);
    }

    static void AdvanceDay(GameState state)
    {
        state.Day++;
        Events.ApplyDailyCosts(state);
    }
}
