using Spectre.Console;

namespace VoidTrader;

static class ChoiceMenu
{
    // Nombre de choix visibles à la fois (scrolling au-delà)
    const int PageSize = 10;

    public static Choice? Present(Situation situation, GameState state)
    {
        var choices = situation.GetChoices(state);
        if (choices.Count == 0) return null;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{situation.Color}]>> {situation.Description}[/]");
        AnsiConsole.WriteLine();

        int selected  = 0;
        int scrollTop = 0;   // premier index visible

        Render(choices, selected, scrollTop, situation.Color);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selected > 0)
                    {
                        selected--;
                        if (selected < scrollTop) scrollTop--;
                        Redraw(choices, selected, scrollTop, situation.Color);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (selected < choices.Count - 1)
                    {
                        selected++;
                        if (selected >= scrollTop + PageSize) scrollTop++;
                        Redraw(choices, selected, scrollTop, situation.Color);
                    }
                    break;

                case ConsoleKey.Enter:
                    AnsiConsole.WriteLine();
                    AnsiConsole.WriteLine();
                    return choices[selected];

                case ConsoleKey.Escape:
                    // Cherche un choix "retour/annuler/quitter"
                    var back = choices.FirstOrDefault(c =>
                        c.Label.Contains('←') || c.Label.Contains("Quitter") ||
                        c.Label.Contains("Annuler") || c.Label.Contains("Repartir"));
                    if (back != null) { AnsiConsole.WriteLine(); return back; }
                    break;
            }
        }
    }

    public static void Resolve(Situation situation, GameState state)
    {
        var choice = Present(situation, state);
        if (choice is null) return;
        choice.Effect?.Invoke(state);
    }

    // ── RENDU ─────────────────────────────────────────────────────────────

    static int RenderedLines;

    static void Render(List<Choice> choices, int selected, int scrollTop, Color titleColor)
    {
        var hasCategories = choices.Any(c => c.Category != null);
        var lines         = 0;

        int visibleEnd = Math.Min(scrollTop + PageSize, choices.Count);

        if (scrollTop > 0)
        {
            AnsiConsole.MarkupLine($"  [grey dim]... {scrollTop} choix au-dessus (↑)[/]");
            lines++;
        }

        string? lastCat = null;
        for (int i = scrollTop; i < visibleEnd; i++)
        {
            var c   = choices[i];
            var sel = i == selected;

            if (hasCategories && c.Category != lastCat)
            {
                if (c.Category != null)
                {
                    AnsiConsole.MarkupLine($"  [grey dim]── {c.Category.ToUpper()} ──[/]");
                    lines++;
                }
                lastCat = c.Category;
            }

            var marker = sel ? $"[{titleColor}]>[/] " : "  ";
            var label  = sel ? $"[bold]{c.Label}[/]" : $"[grey]{c.Label}[/]";
            AnsiConsole.MarkupLine($"  {marker}{label}");
            lines++;
        }

        if (visibleEnd < choices.Count)
        {
            AnsiConsole.MarkupLine($"  [grey dim]... {choices.Count - visibleEnd} choix en-dessous (↓)[/]");
            lines++;
        }

        RenderedLines = lines;
    }

    static void Redraw(List<Choice> choices, int selected, int scrollTop, Color titleColor)
    {
        // Remonter exactement le nombre de lignes imprimées
        ErasePreviousRender();
        Render(choices, selected, scrollTop, titleColor);
    }

    static void ErasePreviousRender()
    {
        for (int i = 0; i < RenderedLines; i++)
        {
            Console.Write("\x1b[2K"); // efface la ligne courante
            Console.Write("\x1b[1A"); // remonte d'une ligne
        }
        Console.Write("\x1b[2K"); // efface la ligne du curseur (la dernière)
        Console.Write("\r");
    }
}
