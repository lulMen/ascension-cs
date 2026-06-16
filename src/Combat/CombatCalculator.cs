using Ascension.Models;

namespace Ascension.Combat;

public static class CombatCalculator
{
    // ── Constants ────────────────────────────────────────────
    private const float BASIC_ATTACK_ACCURACY = 0.25f;
    private const float DEFENSE_K = 30f;
    private const float DEFEND_REACTION = 1.5f;
    private const float BLOCK_K = 30f;
    private const float PARTIAL_FACTOR = 0.5f;
    private const float PRIMARY_WEIGHT = 0.65f;

    // ── Helpers ──────────────────────────────────────────────
    private static float Blend(int primary, int secondary) =>
        primary * PRIMARY_WEIGHT + secondary * (1f - PRIMARY_WEIGHT);
    private static int Scale(float attribute, float c) =>
        (int)(c * MathF.Sqrt(attribute));

    // ── Derived Stats ─────────────────────────────────────────
    public static DerivedStats CalculateDerivedStats(Attributes a) =>
        new DerivedStats(
            MaxHp: Scale(a.Vitality, 5f),
            MaxStamina: Scale(a.Vitality, 3f),
            MaxMp: Scale(a.Intelligence, 3f),
            PhysicalDamage: Scale(a.Strength, 2f),
            MagicalDamage: Scale(a.Intelligence, 2f),
            PhysicalDefense: Scale(a.Vitality, 1.5f),
            MagicalDefense: Scale(a.Willpower, 1.5f),
            Initiative: Scale(a.Agility, 2f),
            Evasion: Scale(a.Agility, 2f),
            Accuracy: Scale(a.Agility, 2f),
            BlockSpeed: Scale(Blend(a.Agility, a.Strength), 2f),
            BlockPower: Scale(Blend(a.Vitality, a.Strength), 2f)
        );

    // ── Turn Order ────────────────────────────────────────────
    public static List<Character> DetermineTurnOrder(List<Character> combatants) =>
        combatants
            .Where(c => c.Resources.CurrentHp > 0 && !c.Resources.HasActed)
            .OrderByDescending(c => CalculateDerivedStats(c.Attributes).Initiative)
            .ToList();

    // ── Attack Resolution ─────────────────────────────────────
    public static AttackResult ResolveAttack(
        Character attacker,
        Character defender,
        AttackType type,
        float modifiers,
        float roll)
    {
        var atkStats = CalculateDerivedStats(attacker.Attributes);
        var defStats = CalculateDerivedStats(defender.Attributes);

        float hitChance = (float)atkStats.Accuracy / (atkStats.Accuracy + defStats.Evasion)
                          + BASIC_ATTACK_ACCURACY;
        bool hit = roll < hitChance;

        if (!hit)
            return new AttackResult(false, 0, hitChance, attacker.Id, defender.Id);

        int rawDamage = type == AttackType.Physical
            ? atkStats.PhysicalDamage
            : atkStats.MagicalDamage;

        int defense = type == AttackType.Physical
            ? defStats.PhysicalDefense
            : defStats.MagicalDefense;

        float reduction = DEFENSE_K / (DEFENSE_K + defense);
        int damage = (int)(rawDamage * modifiers * reduction);

        return new AttackResult(true, damage, hitChance, attacker.Id, defender.Id);
    }

    // ── Block Resolution ──────────────────────────────────────
    public static BlockResult ResolveBlock(Character attacker, Character defender)
    {
        var atkStats = CalculateDerivedStats(attacker.Attributes);
        var defStats = CalculateDerivedStats(defender.Attributes);

        bool fullBlock = defStats.BlockSpeed * DEFEND_REACTION >= atkStats.Initiative;

        return fullBlock
            ? new BlockResult(BlockTier.Full, 1f)
            : new BlockResult(BlockTier.Partial, PARTIAL_FACTOR);
    }

    // ── Win Condition ─────────────────────────────────────────
    public static string? CheckWinCondition(List<Character> sideA, List<Character> sideB)
    {
        bool sideADead = sideA.All(c => c.Resources.CurrentHp <= 0);
        bool sideBDead = sideB.All(c => c.Resources.CurrentHp <= 0);

        if (sideADead && sideBDead) return "draw";
        if (sideADead) return "sideB";
        if (sideBDead) return "sideA";

        return null;
    }
}