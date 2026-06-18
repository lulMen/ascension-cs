using Spectre.Console;
using Ascension.Combat;
using Ascension.Data.Database;
using Ascension.Models;

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

            var player = CharacterCreation.Create();
            string result = RunCombat(player);
            ShowResult(result, player);
        }
    }

    // ── Combat Loop ───────────────────────────────────────────
    private static string RunCombat(Character player)
    {
        var enemy = MobFactory.GenerateForFloor(1);
        if (enemy == null)
        {
            AnsiConsole.MarkupLine("[red]No enemy found for this floor.[/]");
            return "sideA";
        }
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

                // re-fetch fresh state before acting
                actor = combat.SideA.Concat(combat.SideB)
                    .First(c => c.Id == actor.Id);

                var opponents = combat.SideA.Any(c => c.Id == actor.Id)
                    ? combat.SideB
                    : combat.SideA;

                var target = opponents.FirstOrDefault(c => c.Resources.CurrentHp > 0);
                if (target == null) break;

                if (actor.IsPlayerControlled)
                {
                    bool actionTaken = false;
                    while (!actionTaken)
                    {
                        var choice = CombatDisplay.ShowActionMenu(actor);
                        switch (choice)
                        {
                            case "Attack":
                                var freshActor = combat.SideA.Concat(combat.SideB)
                                    .First(c => c.Id == actor.Id);
                                var freshStats = CombatCalculator.CalculateDerivedStats(
                                    freshActor.Attributes, freshActor.Level
                                );
                                float modifier = freshActor.Resources.CurrentStamina >= freshStats.AttackSpCost
                                    ? 1f
                                    : 0.75f;
                                combat.ExecuteTurn(
                                    attacker: actor,
                                    target: target,
                                    type: AttackType.Physical,
                                    modifiers: modifier,
                                    roll: (float)rng.NextDouble()
                                );
                                actionTaken = true;
                                break;

                            case "Defend":
                                combat.SetDefending(actor);
                                actionTaken = true;
                                break;

                            case "Wait":
                                combat.Wait(actor);
                                actionTaken = true;
                                break;

                            default:
                                AnsiConsole.MarkupLine(
                                    "  [dim]Not available yet — coming soon.[/]");
                                break;
                        }
                    }
                }
                else
                {
                    if (ShouldWait(actor))
                        combat.Wait(actor);
                    else if (ShouldDefend(actor, target))
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
            }

            foreach (var entry in combat.Log.Skip(logCursor))
                CombatDisplay.ShowLogLine(entry);
            logCursor = combat.Log.Count;

            AnsiConsole.WriteLine();
            combat.NextRound();
            CombatDisplay.ShowFighters(combat.SideA.First(), combat.SideB.First());
        }

        // ?? "draw" ensures we never return null to ShowResult
        return combat.CheckWin() ?? "draw";
    }

    // ── Result Screen ─────────────────────────────────────────
    private static void ShowResult(string result, Character player)
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
            string newResult = RunCombat(newPlayer);
            ShowResult(newResult, newPlayer);
        }
    }

    // ── Helpers ───────────────────────────────────────────────
    private static bool ShouldDefend(Character actor, Character target)
    {
        var actorStats = CombatCalculator.CalculateDerivedStats(actor.Attributes, actor.Level);
        var targetStats = CombatCalculator.CalculateDerivedStats(target.Attributes, target.Level);

        if (actor.Resources.CurrentStamina < actorStats.BlockSpCost) return false;

        float hpPercent = (float)actor.Resources.CurrentHp / actorStats.MaxHp;
        float targetHpPercent = (float)target.Resources.CurrentHp / targetStats.MaxHp;

        if (targetHpPercent < 0.15f) return false;
        if (hpPercent < 0.30f && actor.Resources.DefendedLastTurn) return false;
        if (hpPercent < 0.30f && actorStats.Initiative < targetStats.Initiative) return true;
        if (hpPercent < 0.30f && actorStats.Initiative >= targetStats.Initiative) return false;
        return false;
    }

    private static bool ShouldWait(Character actor)
    {
        var stats = CombatCalculator.CalculateDerivedStats(actor.Attributes, actor.Level);
        float hpPercent = (float)actor.Resources.CurrentHp / stats.MaxHp;
        return actor.Resources.CurrentStamina == 0 && hpPercent > 0.40f;
    }
}