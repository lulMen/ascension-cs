using Ascension.Models;

namespace Ascension.Data;

public static class Fighters
{
    public static Character Kael => new Character(
        Id: Guid.NewGuid().ToString(),
        Name: "Kael",
        BirthClass: "Adventurer",
        CurrentClass: null,
        Tier: 0,
        Level: 1,
        ResetUsed: false,
        IsPlayerControlled: false,
        Attributes: new Attributes(
            Strength: 55,
            Agility: 50,
            Vitality: 50,
            Intelligence: 20,
            Willpower: 30
        ),
        Resources: new Resources(
            CurrentHp: 0,
            CurrentStamina: 0,
            CurrentMp: 0,
            Defending: false,
            DefendedLastTurn: false,
            HasActed: false,
            IsWaiting: false
        )
    );

    public static Character Veyra => new Character(
        Id: Guid.NewGuid().ToString(),
        Name: "Veyra",
        BirthClass: "Adventurer",
        CurrentClass: null,
        Tier: 0,
        Level: 1,
        ResetUsed: false,
        IsPlayerControlled: false,
        Attributes: new Attributes(
            Strength: 30,
            Agility: 70,
            Vitality: 35,
            Intelligence: 60,
            Willpower: 55
        ),
        Resources: new Resources(
            CurrentHp: 0,
            CurrentStamina: 0,
            CurrentMp: 0,
            Defending: false,
            DefendedLastTurn: false,
            HasActed: false,
            IsWaiting: false
        )
    );
}