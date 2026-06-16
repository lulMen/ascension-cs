namespace Ascension.Models;

public record AttackResult(
    bool Hit,
    int Damage,
    float HitChance,
    string AttackerId,
    string DefenderId,
    string? StatusEffect = null
);