using Spectre.Console;

namespace VoidTrader;

record Choice(
    string Label,
    Action<GameState>? Effect    = null,
    Func<GameState, bool>? Condition = null,
    string? Category = null
);

class Situation
{
    public string Description { get; }
    public Color Color        { get; }
    private readonly List<Choice> _choices;

    public Situation(string description, List<Choice> choices, Color? color = null)
    {
        Description = description;
        Color       = color ?? Spectre.Console.Color.White;
        _choices    = choices;
    }

    // Retourne uniquement les choix disponibles selon l'état du jeu
    public List<Choice> GetChoices(GameState state) =>
        _choices.Where(c => c.Condition is null || c.Condition(state)).ToList();
}
