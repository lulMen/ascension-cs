using Spectre.Console;
using Ascension.Models;
using Ascension.Combat;

namespace Ascension.UI;

public static class CombatDisplay
{
    // ── Colors ────────────────────────────────────────────────
    private const string ColorSideA = "bold yellow";
    private const string ColorSideB = "bold blue";
    private const string ColorHit = "red";
    private const string ColorMiss = "dim";
    private const string ColorBlock = "cyan";
    private const string ColorDefend = "blue";
    private const string ColorWinner = "bold green";

    // ── Header ────────────────────────────────────────────────
    public static void ShowHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold yellow]ASCENSION COMBAT[/]")
            .RuleStyle("yellow")
            .Centered());
        AnsiConsole.WriteLine();
    }

    // ── Fighter Panels ────────────────────────────────────────
    public static void ShowFighters(Character sideA, Character sideB)
    {
        var statsA = CombatCalculator.CalculateDerivedStats(sideA.Attributes, sideA.Level);
        var statsB = CombatCalculator.CalculateDerivedStats(sideB.Attributes, sideB.Level);

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(
            BuildFighterPanel(sideA, statsA, ColorSideA),
            BuildFighterPanel(sideB, statsB, ColorSideB)
        );

        AnsiConsole.Write(grid);
    }

    private static Panel BuildFighterPanel(Character c, DerivedStats stats, string nameColor)
    {
        string hpColor = HpColor(c.Resources.CurrentHp, stats.MaxHp);
        string hpBar = HpBar(c.Resources.CurrentHp, stats.MaxHp);
        string status = c.Resources.Defending ? " [blue][[DEFENDING]][/]" : "";

        string content;

        if (c.IsPlayerControlled)
        {
            string spColor = SpColor(c.Resources.CurrentStamina, stats.MaxStamina);
            string spBar = HpBar(c.Resources.CurrentStamina, stats.MaxStamina);

            content =
                $"[{nameColor}]{c.Name}[/]{status}\n" +
                $"HP  [{hpColor}]{hpBar} {c.Resources.CurrentHp}/{stats.MaxHp}[/]\n" +
                $"SP  [{spColor}]{spBar} {c.Resources.CurrentStamina}/{stats.MaxStamina}[/]\n" +
                $"MP  [blue]{c.Resources.CurrentMp}/{stats.MaxMp}[/]\n" +
                $"\n" +
                $"STR [white]{c.Attributes.Strength}[/]  " +
                $"AGI [white]{c.Attributes.Agility}[/]\n" +
                $"VIT [white]{c.Attributes.Vitality}[/]  " +
                $"INT [white]{c.Attributes.Intelligence}[/]  " +
                $"WIL [white]{c.Attributes.Willpower}[/]";
        }
        else
        {
            content =
                $"[{nameColor}]{c.Name}[/]{status}\n" +
                $"HP  [{hpColor}]{hpBar}[/]\n" +
                $"     {HpCondition(c.Resources.CurrentHp, stats.MaxHp)}\n" +
                $"\n" +
                $"[dim]??? ??  ???[/]\n" +
                $"[dim]??? ??  ???  ???[/]";
        }

        return new Panel(new Markup(content))
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);
    }

    // ── Round Header ──────────────────────────────────────────
    public static void ShowRound(int round)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[grey]Round {round}[/]")
            .RuleStyle("grey")
            .LeftJustified());
    }

    // ── Action Menu ───────────────────────────────────────────
    public static string ShowActionMenu(Character player)
    {
        var stats = CombatCalculator.CalculateDerivedStats(player.Attributes, player.Level);
        bool exhausted = player.Resources.CurrentStamina < stats.AttackSpCost;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [grey]Your turn —[/] [yellow]{player.Name}[/]");
        AnsiConsole.MarkupLine(
            $"  [grey]SP[/] [{SpColor(player.Resources.CurrentStamina, stats.MaxStamina)}]" +
            $"{player.Resources.CurrentStamina}/{stats.MaxStamina}[/]  " +
            $"[grey]ATK cost[/] [white]{stats.AttackSpCost}[/]  " +
            $"[grey]DEF cost[/] [white]{stats.BlockSpCost}[/]"
        );

        if (exhausted)
            AnsiConsole.MarkupLine("  [red]Exhausted — attacking at reduced power (0.75x)[/]");


        AnsiConsole.WriteLine();

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .HighlightStyle(new Style(Color.Yellow))
                .AddChoices("Attack", "Defend", "Wait", "Skills (soon)", "Items (soon)")
        );
    }

    // ── Log Lines ─────────────────────────────────────────────
    public static void ShowLogLine(string entry)
    {
        string styled = entry switch
        {
            var e when e.Contains("fully blocks") => $"[{ColorBlock}]{e}[/]",
            var e when e.Contains("partially blocks") => $"[{ColorBlock}]{e}[/]",
            var e when e.Contains("braces") => $"[{ColorDefend}]{e}[/]",
            var e when e.Contains("waits") => $"[dim]{e}[/]",
            var e when e.Contains("exhausted") => $"[red]{e}[/]",
            var e when e.Contains("miss") => $"[{ColorMiss}]{e}[/]",
            var e when e.Contains("hits") => $"[{ColorHit}]{e}[/]",
            _ => entry
        };

        AnsiConsole.MarkupLine($"  {styled}");
    }

    // ── Winner ────────────────────────────────────────────────
    public static void ShowWinner(string name)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(
            new Markup($"[{ColorWinner}]WINNER: {name.ToUpper()}[/]"))
            .Border(BoxBorder.Double)
            .Padding(2, 1));
    }

    // ── Helpers ───────────────────────────────────────────────
    private static string HpColor(int current, int max)
    {
        float pct = (float)current / max;
        return pct > 0.5f ? "green" : pct > 0.25f ? "yellow" : "red";
    }

    private static string SpColor(int current, int max)
    {
        float pct = (float)current / max;
        return pct > 0.5f ? "cyan" : pct > 0.25f ? "yellow" : "red";
    }

    private static string HpCondition(int current, int max)
    {
        float pct = (float)current / max;
        if (pct >= 1f) return "[green]Unscathed[/]";
        if (pct >= 0.75f) return "[green]Scratched[/]";
        if (pct >= 0.50f) return "[yellow]Bloodied[/]";
        if (pct >= 0.25f) return "[yellow]Wounded[/]";
        if (pct >= 0.10f) return "[red]Critical[/]";
        return "[bold red]Near Death[/]";
    }

    private static string HpBar(int current, int max, int width = 10)
    {
        int filled = (int)((float)current / max * width);
        filled = Math.Max(0, Math.Min(width, filled));
        return new string('█', filled) + new string('░', width - filled);
    }
}