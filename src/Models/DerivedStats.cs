namespace Ascension.Models;

public record DerivedStats(
    int MaxHp,
    int MaxStamina,
    int MaxMp,
    int PhysicalDamage,
    int MagicalDamage,
    int PhysicalDefense,
    int MagicalDefense,
    int Initiative,
    int Evasion,
    int Accuracy,
    int BlockSpeed,
    int BlockPower,
    int SpRegen,
    int MpRegen,
    int AttackSpCost,
    int BlockSpCost,
    int DodgeSpCost
);