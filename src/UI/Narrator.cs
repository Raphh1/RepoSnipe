using Spectre.Console;

namespace VoidTrader;

static class Narrator
{
    // Effet machine à écrire — lettre par lettre
    public static void Say(string message, Color? color = null)
    {
        AnsiConsole.WriteLine();
        var c = color ?? Color.Grey;
        Console.Write($"\x1b[3m"); // italique ANSI
        foreach (var ch in message)
        {
            Console.Write(ch);
            Thread.Sleep(ch == '.' || ch == '!' || ch == '?' ? 60 : ch == ',' ? 40 : 18);
        }
        Console.Write($"\x1b[0m"); // reset
        Console.WriteLine();
        Thread.Sleep(200);
    }

    // Attend que le joueur appuie sur Entrée avant de continuer
    public static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey dim]— Appuie sur Entrée pour continuer —[/]");
        Console.ReadLine();
    }

    // Phrase + pause combinées
    public static void SayAndPause(string message, Color? color = null)
    {
        Say(message, color);
        Pause();
    }
}
