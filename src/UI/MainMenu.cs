using Spectre.Console;

namespace Ascension.UI;

public static class MainMenu
{
    public static string Show()
    {
        AnsiConsole.Clear();
        ShowTitle();

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .HighlightStyle(new Style(Color.Yellow))
                .AddChoices("New Game", "Quit")
        );
    }

    private static void ShowTitle()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("ASCENSION")
            .Centered()
            .Color(Color.Yellow));

        AnsiConsole.Write(new Rule("[grey]A World of Myth and Legend[/]")
            .RuleStyle("grey")
            .Centered());

        AnsiConsole.WriteLine();
    }
}