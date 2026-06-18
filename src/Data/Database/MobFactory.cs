using Microsoft.Data.Sqlite;
using Ascension.Models;
using Ascension.Combat;
using Ascension.Core;

namespace Ascension.Data.Database;

public record EnemyTemplate(
    string Id,
    string Name,
    int FloorMin,
    int FloorMax,
    int StrFlag,
    int AgiFlag,
    int VitFlag,
    int IntFlag,
    int WilFlag,
    bool IsElite,
    bool IsBoss
);

public static class MobFactory
{
    private static readonly Random _rng = new Random();

    // ── Public API ────────────────────────────────────────────
    public static Character? GenerateForFloor(int floor)
    {
        var template = LoadTemplate(floor);
        if (template == null) return null;

        float mult = GetMultiplier(template);
        int level = floor;
        int pool = (int)(TowerConfig.MobStatPool(level) * mult);

        var attributes = DistributeStats(pool, template);
        return BuildCharacter(template, attributes, level);
    }

    // ── Template Loading ──────────────────────────────────────
    private static EnemyTemplate? LoadTemplate(int floor)
    {
        using var connection = new SqliteConnection(DbConfig.ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Name, FloorMin, FloorMax,
                   StrFlag, AgiFlag, VitFlag, IntFlag, WilFlag,
                   IsElite, IsBoss
            FROM   EnemyTemplates
            WHERE  FloorMin <= $floor AND FloorMax >= $floor
            ORDER BY RANDOM()
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("$floor", floor);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new EnemyTemplate(
            Id: reader.GetString(0),
            Name: reader.GetString(1),
            FloorMin: reader.GetInt32(2),
            FloorMax: reader.GetInt32(3),
            StrFlag: reader.GetInt32(4),
            AgiFlag: reader.GetInt32(5),
            VitFlag: reader.GetInt32(6),
            IntFlag: reader.GetInt32(7),
            WilFlag: reader.GetInt32(8),
            IsElite: reader.GetInt32(9) == 1,
            IsBoss: reader.GetInt32(10) == 1
        );
    }

    // ── Stat Distribution ─────────────────────────────────────
    private static Attributes DistributeStats(int pool, EnemyTemplate t)
    {
        // Build flag pairs sorted highest to lowest, skip Flag 0
        var flags = new List<(string Attr, int Flag)>
        {
            ("STR", t.StrFlag),
            ("AGI", t.AgiFlag),
            ("VIT", t.VitFlag),
            ("INT", t.IntFlag),
            ("WIL", t.WilFlag)
        }
        .Where(f => f.Flag > 0)
        .OrderByDescending(f => f.Flag)
        .ToList();

        int str = 0, agi = 0, vit = 0, @int = 0, wil = 0;
        int remaining = pool;

        foreach (var (attr, flag) in flags)
        {
            float minPct = FlagMinPercent(flag);
            float maxPct = FlagMaxPercent(flag);
            float rolled = minPct + (float)_rng.NextDouble() * (maxPct - minPct);
            int amount = (int)Math.Ceiling(pool * rolled);
            amount = Math.Min(amount, remaining);

            switch (attr)
            {
                case "STR": str = amount; break;
                case "AGI": agi = amount; break;
                case "VIT": vit = amount; break;
                case "INT": @int = amount; break;
                case "WIL": wil = amount; break;
            }

            remaining -= amount;
        }

        // Distribute remainder across all five — rounded up
        if (remaining > 0)
        {
            int share = (int)Math.Ceiling((double)remaining / 5);
            str += share;
            agi += share;
            vit += share;
            @int += share;
            wil += share;
        }

        // Minimum 1 in each attribute
        return new Attributes(
            Strength: Math.Max(1, str),
            Agility: Math.Max(1, agi),
            Vitality: Math.Max(1, vit),
            Intelligence: Math.Max(1, @int),
            Willpower: Math.Max(1, wil)
        );
    }

    // ── Character Build ───────────────────────────────────────
    private static Character BuildCharacter(
        EnemyTemplate template, Attributes attributes, int level)
    {
        var stats = CombatCalculator.CalculateDerivedStats(attributes, level);

        return new Character(
            Id: Guid.NewGuid().ToString(),
            Name: template.Name,
            BirthClass: "Monster",
            CurrentClass: template.IsElite ? "Elite"
                              : template.IsBoss ? "Boss"
                              : "Standard",
            Tier: 0,
            Level: level,
            Xp: 0,
            StatPointsAvailable: 0,
            ResetUsed: false,
            IsPlayerControlled: false,
            Attributes: attributes,
            Resources: new Resources(
                CurrentHp: stats.MaxHp,
                CurrentStamina: stats.MaxStamina,
                CurrentMp: stats.MaxMp,
                Defending: false,
                DefendedLastTurn: false,
                HasActed: false,
                IsWaiting: false
            )
        );
    }

    // ── Helpers ───────────────────────────────────────────────
    private static float GetMultiplier(EnemyTemplate t)
    {
        if (t.IsBoss)
            return TowerConfig.BossMinMult +
                   (float)_rng.NextDouble() *
                   (TowerConfig.BossMaxMult - TowerConfig.BossMinMult);

        if (t.IsElite)
            return TowerConfig.EliteMinMult +
                   (float)_rng.NextDouble() *
                   (TowerConfig.EliteMaxMult - TowerConfig.EliteMinMult);

        return 1.0f;
    }

    private static float FlagMinPercent(int flag) => flag switch
    {
        1 => 0.05f,
        2 => 0.15f,
        3 => 0.25f,
        4 => 0.35f,
        5 => 0.45f,
        _ => 0f
    };

    private static float FlagMaxPercent(int flag) => flag switch
    {
        1 => 0.10f,
        2 => 0.20f,
        3 => 0.30f,
        4 => 0.40f,
        5 => 0.50f,
        _ => 0f
    };
}