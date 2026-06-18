namespace Ascension.Models;

public record Resources(
    int CurrentHp,
    int CurrentStamina,
    int CurrentMp,
    bool Defending,
    bool DefendedLastTurn,
    bool HasActed,
    bool IsWaiting
);