using Ascension.Combat;
using Ascension.Data;
using Ascension.Models;

// ── Initialize Fighters ───────────────────────────────────
Character InitFighter(Character fighter)
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

var kael = InitFighter(Fighters.Kael);
var veyra = InitFighter(Fighters.Veyra);

bool ShouldDefend(Character actor, Character target)
{
    var actorStats = CombatCalculator.CalculateDerivedStats(actor.Attributes);
    var targetStats = CombatCalculator.CalculateDerivedStats(target.Attributes);

    float hpPercent = (float)actor.Resources.CurrentHp / actorStats.MaxHp;
    float targetHpPercent = (float)target.Resources.CurrentHp / targetStats.MaxHp;

    // Rule 1 - finish them
    if (targetHpPercent < 0.15f) return false;
    // Rule 2 - no turtling
    if (hpPercent < 0.30f && actorStats.Initiative < targetStats.Initiative) return true;
    // Rule 3 - survival
    if (hpPercent < 0.30f && actorStats.Initiative < targetStats.Initiative) return false;
    // Rule 4 - aggression
    if (hpPercent < 0.30f && actorStats.Initiative >= targetStats.Initiative) return true;
    // Rule 5 - default
    return false;
}

// ── Start Combat ──────────────────────────────────────────
var combat = new CombatManager(
    sideA: new List<Character> { kael },
    sideB: new List<Character> { veyra }
);

var rng = new Random();

Console.WriteLine("═══════════════════════════════════");
Console.WriteLine("          ASCENSION COMBAT         ");
Console.WriteLine("═══════════════════════════════════");

// ── Combat Loop ───────────────────────────────────────────
int logCursor = 0;

while (combat.CheckWin() == null)
{
    Console.WriteLine($"\n── Round {combat.Round} ──────────────────────");

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
        Console.WriteLine(entry);
    logCursor = combat.Log.Count;

    var kStatus = combat.SideA.First();
    var vStatus = combat.SideB.First();
    Console.WriteLine($"\n{kStatus.Name}: {kStatus.Resources.CurrentHp} HP");
    Console.WriteLine($"{vStatus.Name}: {vStatus.Resources.CurrentHp} HP");

    combat.NextRound();
}

var winner = combat.CheckWin() == "sideA"
    ? combat.SideA.First().Name
    : combat.SideB.First().Name;

Console.WriteLine("\n═══════════════════════════════════");
Console.WriteLine($"  WINNER: {winner.ToUpper()}");
Console.WriteLine("═══════════════════════════════════");