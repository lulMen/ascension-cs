using Ascension.Models;

namespace Ascension.Combat;

public static class CombatCalculator
{
    // ── Constants ────────────────────────────────────────────
    private const float BASIC_ATTACK_ACCURACY = 0.15f;
    private const float DEFENSE_K = 30f;
    private const int MIN_DAMAGE = 1;
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
}