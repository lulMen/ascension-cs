using Spectre.Console;
using Ascension.Models;
using Ascension.Combat;

namespace Ascension.UI;

public static class CharacterCreation
{
    private const int POOL = 15;
    private const int MIN_STAT = 1;
    private const int MAX_STAT = 11; // base 1 + max 10
    private const int STAT_COUNT = 5;

    private static readonly string[] StatNames =
        { "Strength", "Agility", "Vitality", "Intelligence", "Willpower" };

    public static Character Create()
    {
        // ── Name Input ────────────────────────────────────────
        AnsiConsole.Clear();
        ShowTitle();

        string name = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Enter your name:[/]")
                .Validate(n => n.Trim().Length >= 2
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Name must be at least 2 characters.[/]"))
        );

        // ── Stat Allocation ───────────────────────────────────
        int[] stats = { 1, 1, 1, 1, 1 };
        int remaining = POOL;
        int selected = 0;

        while (true)
        {
            AnsiConsole.Clear();
            ShowTitle();
            ShowAllocation(stats, remaining, selected, name);

            var key = Console.ReadKey(intercept: true).Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + STAT_COUNT) % STAT_COUNT;
                    break;

                case ConsoleKey.DownArrow:
                    selected = (selected + 1) % STAT_COUNT;
                    break;

                case ConsoleKey.RightArrow:
                    if (remaining > 0 && stats[selected] < MAX_STAT)
                    {
                        stats[selected]++;
                        remaining--;
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (stats[selected] > MIN_STAT)
                    {
                        stats[selected]--;
                        remaining++;
                    }
                    break;

                case ConsoleKey.Enter:
                    if (remaining == 0)
                        goto Done;
                    else
                        AnsiConsole.MarkupLine("[red]  Spend all points before confirming.[/]");
                    break;
            }
        }

    Done:
        return BuildCharacter(name, stats);
    }

    // ── Display ───────────────────────────────────────────────
    private static void ShowTitle()
    {
        AnsiConsole.Write(new Rule("[bold yellow]CHARACTER CREATION[/]")
            .RuleStyle("yellow")
            .Centered());
        AnsiConsole.WriteLine();
    }

    private static void ShowAllocation(int[] stats, int remaining, int selected, string name)
    {
        // Points remaining
        string pointsColor = remaining == 0 ? "green" : "yellow";
        AnsiConsole.MarkupLine($"  [grey]Name:[/] [white]{name}[/]");
        AnsiConsole.MarkupLine($"  [grey]Points remaining:[/] [{pointsColor}]{remaining}[/]");
        AnsiConsole.WriteLine();

        // Stat rows
        for (int i = 0; i < STAT_COUNT; i++)
        {
            bool isSelected = i == selected;
            string cursor = isSelected ? "[bold yellow]►[/]" : " ";
            string statName = StatNames[i].PadRight(13);
            string canAdd = remaining > 0 && stats[i] < MAX_STAT ? "[grey]►[/]" : "[dim]►[/]";
            string canSub = stats[i] > MIN_STAT ? "[grey]◄[/]" : "[dim]◄[/]";
            string val = isSelected
                ? $"[bold white]{stats[i],2}[/]"
                : $"[white]{stats[i],2}[/]";

            AnsiConsole.MarkupLine($"  {cursor} {statName} {canSub} {val} {canAdd}");
        }

        AnsiConsole.WriteLine();

        // Live derived stat preview
        var attributes = new Attributes(
            stats[0], stats[1], stats[2], stats[3], stats[4]
        );
        var derived = CombatCalculator.CalculateDerivedStats(attributes);

        AnsiConsole.MarkupLine(
            $"  [grey]HP[/] [green]{derived.MaxHp}[/]  " +
            $"[grey]MP[/] [blue]{derived.MaxMp}[/]  " +
            $"[grey]SP[/] [cyan]{derived.MaxStamina}[/]"
        );
        AnsiConsole.MarkupLine(
            $"  [grey]DMG[/] [red]{derived.PhysicalDamage}[/]  " +
            $"[grey]DEF[/] [white]{derived.PhysicalDefense}[/]  " +
            $"[grey]SPD[/] [yellow]{derived.Initiative}[/]"
        );

        AnsiConsole.WriteLine();
        string confirmHint = remaining == 0
            ? "[green]Press Enter to confirm[/]"
            : "[dim]Spend all points to confirm[/]";
        AnsiConsole.MarkupLine($"  {confirmHint}");
    }

    // ── Build Character ───────────────────────────────────────
    private static Character BuildCharacter(string name, int[] stats)
    {
        var attributes = new Attributes(
            Strength: stats[0],
            Agility: stats[1],
            Vitality: stats[2],
            Intelligence: stats[3],
            Willpower: stats[4]
        );

        var derived = CombatCalculator.CalculateDerivedStats(attributes);

        return new Character(
            Id: Guid.NewGuid().ToString(),
            Name: name,
            BirthClass: "Adventurer",
            CurrentClass: null,
            Tier: 0,
            Level: 1,
            Xp: 0,
           StatPointsAvailable: 0,
            ResetUsed: false,
            IsPlayerControlled: true,
            Attributes: attributes,
            Resources: new Resources(
                CurrentHp: derived.MaxHp,
                CurrentStamina: derived.MaxStamina,
                CurrentMp: derived.MaxMp,
                Defending: false,
                DefendedLastTurn: false,
                HasActed: false,
                IsWaiting: false
            )
        );
    }
}