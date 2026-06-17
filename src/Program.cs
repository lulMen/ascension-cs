using Spectre.Console;

using Ascension.Combat;
using Ascension.Data;
using Ascension.Models;
using Ascension.UI;

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

bool ShouldDefend(Character actor, Character target)
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

var kael = InitFighter(Fighters.Kael);
var veyra = InitFighter(Fighters.Veyra);

var combat = new CombatManager(
    sideA: new List<Character> { kael },
    sideB: new List<Character> { veyra }
);

var rng = new Random();

// ── Header ────────────────────────────────────────────────
CombatDisplay.ShowHeader();
CombatDisplay.ShowFighters(combat.SideA.First(), combat.SideB.First());

// ── Combat Loop ───────────────────────────────────────────
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

// ── Winner ────────────────────────────────────────────────
var winner = combat.CheckWin() == "sideA"
    ? combat.SideA.First().Name
    : combat.SideB.First().Name;

CombatDisplay.ShowWinner(winner);