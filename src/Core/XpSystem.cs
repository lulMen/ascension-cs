using Ascension.Models;

namespace Ascension.Core;

public record LevelUpEvent(
    int NewLevel,
    int FreePoints,
    int FlatBonus,
    bool IsJobChangeLevel
);

public static class XpSystem
{
    // ── Gain XP ───────────────────────────────────────────────
    public static (Character updated, List<LevelUpEvent> events) GainXp(
        Character character, int xpGained)
    {
        var events = new List<LevelUpEvent>();
        var current = character with { Xp = character.Xp + xpGained };

        while (current.Level < TowerConfig.LevelCap &&
               current.Xp >= TowerConfig.XpToNextLevel(current.Level))
        {
            current = current with
            {
                Xp = current.Xp - TowerConfig.XpToNextLevel(current.Level)
            };

            int newLevel = current.Level + 1;
            int freePoints = CalculateStatPointsForLevel(newLevel);
            int flatBonus = CalculateFlatBonusForLevel(newLevel);
            bool isJobChange = TowerConfig.IsJobChangeLevel(newLevel);

            var attrs = flatBonus > 0
                ? new Attributes(
                    current.Attributes.Strength + flatBonus,
                    current.Attributes.Agility + flatBonus,
                    current.Attributes.Vitality + flatBonus,
                    current.Attributes.Intelligence + flatBonus,
                    current.Attributes.Willpower + flatBonus)
                : current.Attributes;

            current = current with
            {
                Level = newLevel,
                Attributes = attrs,
                StatPointsAvailable = current.StatPointsAvailable + freePoints
            };

            events.Add(new LevelUpEvent(newLevel, freePoints, flatBonus, isJobChange));
        }

        return (current, events);
    }

    // ── Death Penalty ─────────────────────────────────────────
    public static Character ApplyDeath(Character character)
    {
        int totalXp = TotalXpAtLevel(character.Level) + character.Xp;
        int xpLost = (int)(totalXp * TowerConfig.DeathXpLossFraction);
        int remainingXp = Math.Max(0, totalXp - xpLost);
        int minLevel = TowerConfig.MinLevelForTier(character.Tier);
        int newLevel = character.Level;

        while (newLevel > minLevel &&
               remainingXp < TotalXpAtLevel(newLevel))
        {
            newLevel--;
        }

        int newXp = Math.Max(0, remainingXp - TotalXpAtLevel(newLevel));

        return character with { Level = newLevel, Xp = newXp };
    }

    // ── Stat Point Allocation ─────────────────────────────────
    public static Character SpendStatPoint(Character character, string attribute)
    {
        if (character.StatPointsAvailable <= 0) return character;

        var a = character.Attributes;
        Attributes updated;

        if (attribute == "Strength") updated = new Attributes(a.Strength + 1, a.Agility, a.Vitality, a.Intelligence, a.Willpower);
        else if (attribute == "Agility") updated = new Attributes(a.Strength, a.Agility + 1, a.Vitality, a.Intelligence, a.Willpower);
        else if (attribute == "Vitality") updated = new Attributes(a.Strength, a.Agility, a.Vitality + 1, a.Intelligence, a.Willpower);
        else if (attribute == "Intelligence") updated = new Attributes(a.Strength, a.Agility, a.Vitality, a.Intelligence + 1, a.Willpower);
        else if (attribute == "Willpower") updated = new Attributes(a.Strength, a.Agility, a.Vitality, a.Intelligence, a.Willpower + 1);
        else return character;

        return character with
        {
            Attributes = updated,
            StatPointsAvailable = character.StatPointsAvailable - 1
        };
    }

    // ── Equipment Tier ────────────────────────────────────────
    public static string GetEquipmentTier(int level)
    {
        if (level >= TowerConfig.GearMythic) return "Mythic";
        if (level >= TowerConfig.GearDivine) return "Divine";
        if (level >= TowerConfig.GearLegendary) return "Legendary";
        if (level >= TowerConfig.GearEpic) return "Epic";
        if (level >= TowerConfig.GearDarkGold) return "Dark Gold";
        if (level >= TowerConfig.GearGold) return "Gold";
        if (level >= TowerConfig.GearSilver) return "Silver";
        if (level >= TowerConfig.GearIron) return "Iron";
        return "Common";
    }

    // ── Helpers ───────────────────────────────────────────────
    public static int CalculateStatPointsForLevel(int level)
    {
        if (level == TowerConfig.LevelCap) return TowerConfig.StatPointsLevel200;
        if (TowerConfig.IsJobChangeLevel(level)) return TowerConfig.StatPointsJobChange;
        if (level % 10 == 0) return TowerConfig.StatPointsEvery10;
        if (level % 5 == 0) return TowerConfig.StatPointsEvery5;
        return TowerConfig.StatPointsStandard;
    }

    public static int CalculateFlatBonusForLevel(int level)
    {
        if (level == TowerConfig.LevelCap) return TowerConfig.FlatBonusLevel200;
        if (level % 10 == 0) return TowerConfig.FlatBonusEvery10;
        return 0;
    }

    public static int TotalXpAtLevel(int level)
    {
        int total = 0;
        for (int i = 1; i < level; i++)
            total += TowerConfig.XpToNextLevel(i);
        return total;
    }
}