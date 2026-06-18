namespace Ascension.Data.Database;

public static class DbConfig
{
    public static string DbPath
    {
        get
        {
            // Navigate from bin/Debug/net9.0/ up to repo root
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var repoRoot = Path.GetFullPath(
                Path.Combine(baseDir, "..", "..", "..", ".."));
            return Path.Combine(repoRoot, "data", "ascension.db");
        }
    }

    public static string ConnectionString => $"Data Source={DbPath}";
}