using Spectre.Console;
using Ascension.Combat;
using Ascension.Data.Enemies;
using Ascension.Models;
using Ascension.UI;

namespace Ascension.UI;

public static class GameLoop
{
    public static void Run()
    {
        while (true)
        {
            string choice = MainMenu.Show();

            if (choice == "Quit")
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[grey]Farewell, Adventurer.[/]");
                break;
            }

            // ── New Game ──────────────────────────────────────
            var player = CharacterCreation.Create();

            // ── Combat ────────────────────────────────────────
            string? result = RunCombat(player);

            // ── Result Screen ─────────────────────────────────
            ShowResult(result, player);
        }
    }

    // ── Combat Loop ───────────────────────────────────────────
    private static string? RunCombat(Character player)
    {
        var enemy = InitFighter(Vermin.DustfangRat);
        var combat = new CombatManager(
            sideA: new List<Character> { player },
            sideB: new List<Character> { enemy }
        );

        var rng = new Random();

        CombatDisplay.ShowHeader();
        CombatDisplay.ShowFighters(combat.SideA.First(), combat.SideB.First());

        int logCursor = 0;

        while (combat.CheckWin() == null)
        {
            CombatDisplay.ShowRound(combat.Round);

            while (!combat.IsRoundOver())
            {
                var actor = combat.GetNextActor();
                if (actor == null) break;

                var opponents = combat.SideA.Any(c => c.Id == actor.Id)
                    ? combat.SideB
                    : combat.SideA;

                var target = opponents.First(c => c.Resources.CurrentHp > 0);

                if (ShouldDefend(actor, target))
                    combat.SetDefending(actor);
                else
                    combat.ExecuteTurn(
                        attacker: actor,
                        target: target,
                        type: AttackType.Physical,
                        modifiers: 1f,
                        roll: (float)rng.NextDouble()
                    );
            }

            foreach (var entry in combat.Log.Skip(logCursor))
                CombatDisplay.ShowLogLine(entry);
            logCursor = combat.Log.Count;

            AnsiConsole.WriteLine();
            CombatDisplay.ShowFighters(combat.SideA.First(), combat.SideB.First());

            combat.NextRound();
        }

        return combat.CheckWin();
    }

    // ── Result Screen ─────────────────────────────────────────
    private static void ShowResult(string? result, Character player)
    {
        AnsiConsole.WriteLine();

        if (result == "sideA")
            CombatDisplay.ShowWinner(player.Name);
        else if (result == "sideB")
        {
            AnsiConsole.Write(new Panel(
                new Markup("[red]YOU HAVE FALLEN[/]"))
                .Border(BoxBorder.Double)
                .Padding(2, 1));
        }
        else
        {
            AnsiConsole.Write(new Panel(
                new Markup("[grey]DRAW — BOTH HAVE FALLEN[/]"))
                .Border(BoxBorder.Double)
                .Padding(2, 1));
        }

        AnsiConsole.WriteLine();
        var next = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .HighlightStyle(new Style(Color.Yellow))
                .AddChoices("Play Again", "Main Menu")
        );

        if (next == "Play Again")
        {
            var newPlayer = CharacterCreation.Create();
            RunCombat(newPlayer);
        }
    }

    // ── Helpers ───────────────────────────────────────────────
    private static Character InitFighter(Character fighter)
    {
        var stats = CombatCalculator.CalculateDerivedStats(fighter.Attributes);
        return fighter with
        {
            Resources = new Resources(
                CurrentHp: stats.MaxHp,
                CurrentStamina: stats.MaxStamina,
                CurrentMp: stats.MaxMp,
                Defending: false,
                DefendedLastTurn: false,
                HasActed: false
            )
        };
    }

    private static bool ShouldDefend(Character actor, Character target)
    {
        var actorStats = CombatCalculator.CalculateDerivedStats(actor.Attributes);
        var targetStats = CombatCalculator.CalculateDerivedStats(target.Attributes);

        float hpPercent = (float)actor.Resources.CurrentHp / actorStats.MaxHp;
        float targetHpPercent = (float)target.Resources.CurrentHp / targetStats.MaxHp;

        if (targetHpPercent < 0.15f) return false;
        if (hpPercent < 0.30f && actor.Resources.DefendedLastTurn) return false;
        if (hpPercent < 0.30f && actorStats.Initiative < targetStats.Initiative) return true;
        if (hpPercent < 0.30f && actorStats.Initiative >= targetStats.Initiative) return false;
        return false;
    }
}