using Microsoft.Data.Sqlite;

namespace Ascension.Data.Database;

public static class DbInitializer
{
    public static void Initialize()
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(DbConfig.DbPath)!);

        using var connection = new SqliteConnection(DbConfig.ConnectionString);
        connection.Open();

        CreateTables(connection);
        SeedCategories(connection);
        SeedEnemies(connection);
    }

    // ── Schema ────────────────────────────────────────────────
    private static void CreateTables(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Id   TEXT PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS EnemyTemplates (
                Id       TEXT PRIMARY KEY,
                Name     TEXT NOT NULL,
                FloorMin INTEGER NOT NULL,
                FloorMax INTEGER NOT NULL,
                StrFlag  INTEGER NOT NULL DEFAULT 0,
                AgiFlag  INTEGER NOT NULL DEFAULT 0,
                VitFlag  INTEGER NOT NULL DEFAULT 0,
                IntFlag  INTEGER NOT NULL DEFAULT 0,
                WilFlag  INTEGER NOT NULL DEFAULT 0,
                IsElite  INTEGER NOT NULL DEFAULT 0,
                IsBoss   INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS EnemyCategories (
                EnemyId    TEXT NOT NULL,
                CategoryId TEXT NOT NULL,
                PRIMARY KEY (EnemyId, CategoryId),
                FOREIGN KEY (EnemyId)    REFERENCES EnemyTemplates(Id),
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    // ── Seed Data ─────────────────────────────────────────────
    private static void SeedCategories(SqliteConnection connection)
    {
        var categories = new[]
        {
            ("vermin",    "Vermin"),
            ("beast",     "Beast"),
            ("undead",    "Undead"),
            ("spirit",    "Spirit"),
            ("elemental", "Elemental"),
            ("divine",    "Divine"),
            ("cursed",    "Cursed")
        };

        foreach (var (id, name) in categories)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Categories (Id, Name)
                VALUES ($id, $name);
            ";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$name", name);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedEnemies(SqliteConnection connection)
    {
        InsertEnemy(connection,
            id: "dustfang-rat",
            name: "Dustfang Rat",
            floorMin: 1,
            floorMax: 10,
            strFlag: 3,
            agiFlag: 2,
            vitFlag: 3,
            intFlag: 1,
            wilFlag: 0,
            isElite: false,
            isBoss: false,
            categories: new[] { "vermin" }
        );
    }

    // ── Helper ────────────────────────────────────────────────
    private static void InsertEnemy(
        SqliteConnection connection,
        string id, string name,
        int floorMin, int floorMax,
        int strFlag, int agiFlag,
        int vitFlag, int intFlag, int wilFlag,
        bool isElite, bool isBoss,
        string[] categories)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO EnemyTemplates
                (Id, Name, FloorMin, FloorMax,
                 StrFlag, AgiFlag, VitFlag, IntFlag, WilFlag,
                 IsElite, IsBoss)
            VALUES
                ($id, $name, $floorMin, $floorMax,
                 $strFlag, $agiFlag, $vitFlag, $intFlag, $wilFlag,
                 $isElite, $isBoss);
        ";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$floorMin", floorMin);
        cmd.Parameters.AddWithValue("$floorMax", floorMax);
        cmd.Parameters.AddWithValue("$strFlag", strFlag);
        cmd.Parameters.AddWithValue("$agiFlag", agiFlag);
        cmd.Parameters.AddWithValue("$vitFlag", vitFlag);
        cmd.Parameters.AddWithValue("$intFlag", intFlag);
        cmd.Parameters.AddWithValue("$wilFlag", wilFlag);
        cmd.Parameters.AddWithValue("$isElite", isElite ? 1 : 0);
        cmd.Parameters.AddWithValue("$isBoss", isBoss ? 1 : 0);
        cmd.ExecuteNonQuery();

        foreach (var catId in categories)
        {
            var catCmd = connection.CreateCommand();
            catCmd.CommandText = @"
                INSERT OR IGNORE INTO EnemyCategories (EnemyId, CategoryId)
                VALUES ($enemyId, $categoryId);
            ";
            catCmd.Parameters.AddWithValue("$enemyId", id);
            catCmd.Parameters.AddWithValue("$categoryId", catId);
            catCmd.ExecuteNonQuery();
        }
    }
}