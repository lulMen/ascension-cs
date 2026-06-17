using Ascension.Models;

namespace Ascension.Data.Enemies;

public static class Vermin
{
    public static Character DustfangRat => new Character(
        Id: Guid.NewGuid().ToString(),
        Name: "Dustfang Rat",
        BirthClass: "Monster",
        CurrentClass: "Vermin",
        Tier: 0,
        ResetUsed: false,
        Attributes: new Attributes(
            Strength: 5,
            Agility: 6,
            Vitality: 5,
            Intelligence: 2,
            Willpower: 2
        ),
        Resources: new Resources(
            CurrentHp: 0,
            CurrentStamina: 0,
            CurrentMp: 0,
            Defending: false,
            DefendedLastTurn: false,
            HasActed: false
        )
    );
}