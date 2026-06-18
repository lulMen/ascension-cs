namespace Ascension.Core;

public static class TowerConfig
{
    // ── Tower Structure ───────────────────────────────────────
    public const int TowerFloors = 100;
    public const int LevelCap = 200;
    public const int EliteEveryNFloors = 5;
    public const int BossEveryNFloors = 10;

    // ── Mob Scaling ───────────────────────────────────────────
    public const double MobScaleBase = 20.0;
    public const double MobScalePow = 1.3;
    public const double MobScaleFactor = 1.82;
    public const float EliteMinMult = 1.3f;
    public const float EliteMaxMult = 1.5f;
    public const float BossMinMult = 1.8f;
    public const float BossMaxMult = 2.3f;

    // ── XP ────────────────────────────────────────────────────
    public const int XpPerLevelMultiplier = 150;
    public const int XpMobMultiplier = 10;
    public const int XpEliteMultiplier = 15;
    public const int XpBossMultiplier = 25;
    public const float DeathXpLossFraction = 0.20f;

    // ── Stat Points Per Level ─────────────────────────────────
    public const int StatPointsStandard = 3;
    public const int StatPointsEvery5 = 3;   // + equipment tier unlock
    public const int StatPointsEvery10 = 3;   // + flat +5 all attributes
    public const int FlatBonusEvery10 = 5;   // flat to ALL attributes
    public const int StatPointsJobChange = 10;
    public const int StatPointsLevel200 = 15;  // + flat +10 all attributes
    public const int FlatBonusLevel200 = 10;  // flat to ALL attributes

    // ── Tier Job Change Levels ────────────────────────────────
    public const int Tier1Level = 20;
    public const int Tier2Level = 50;
    public const int Tier3Level = 90;
    public const int Tier4Level = 140;

    // ── Regression Hard Floors ────────────────────────────────
    public const int MinLevelTier0 = 1;
    public const int MinLevelTier1 = 20;
    public const int MinLevelTier2 = 50;
    public const int MinLevelTier3 = 90;
    public const int MinLevelTier4 = 140;

    // ── Equipment Tier Unlock Levels ─────────────────────────
    public const int GearCommon = 1;
    public const int GearIron = 5;
    public const int GearSilver = 10;
    public const int GearGold = 20;
    public const int GearDarkGold = 35;
    public const int GearEpic = 50;
    public const int GearLegendary = 90;
    public const int GearDivine = 140;
    public const int GearMythic = 200;

    // ── Helper Methods ────────────────────────────────────────
    public static bool IsEliteFloor(int floor) =>
        floor % EliteEveryNFloors == 0 && !IsBossFloor(floor);

    public static bool IsBossFloor(int floor) =>
        floor % BossEveryNFloors == 0;

    public static bool IsCheckpointFloor(int floor) =>
        IsBossFloor(floor);

    public static bool IsJobChangeLevel(int level) =>
        level == Tier1Level || level == Tier2Level ||
        level == Tier3Level || level == Tier4Level;

    public static int XpToNextLevel(int currentLevel) =>
        currentLevel * XpPerLevelMultiplier;

    public static int MinLevelForTier(int tier) => tier switch
    {
        0 => MinLevelTier0,
        1 => MinLevelTier1,
        2 => MinLevelTier2,
        3 => MinLevelTier3,
        4 => MinLevelTier4,
        _ => MinLevelTier0
    };

    public static double MobStatPool(int level) =>
        MobScaleBase + Math.Pow(level - 1, MobScalePow) * MobScaleFactor;
}