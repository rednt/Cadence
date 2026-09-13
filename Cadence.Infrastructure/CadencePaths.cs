namespace Cadence.Infrastructure
{
    /// <summary>
    /// Single source of truth for runtime data locations. Both CLI and Worker
    /// resolve through here, so the .exe works from any folder — no source-tree
    /// walk-up, no divergent databases.
    /// </summary>
    public static class CadencePaths
    {
        public static string GetDataDirectory() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cadence");

        public static string GetDbPath() => Path.Combine(GetDataDirectory(), "cadence.db");

        public static string GetPidPath() => Path.Combine(GetDataDirectory(), "worker.pid");

        public static string GetRoutinePath() => Path.Combine(GetDataDirectory(), "routine.json");

        /// <summary>
        /// Best-effort lookup of the pre-refactor solution-root database, for one-time
        /// migration. Returns null when not found (e.g. published .exe far from source).
        /// </summary>
        public static string? FindLegacyDbPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 10; i++)
            {
                if (dir is null) break;
                if (File.Exists(Path.Combine(dir.FullName, "Cadence.sln")))
                {
                    var legacy = Path.Combine(dir.FullName, "CadenceDB", "cadence.db");
                    return File.Exists(legacy) ? legacy : null;
                }
                dir = dir.Parent;
            }
            return null;
        }

        /// <summary>
        /// Copies the legacy solution-root DB into the data directory once, when the
        /// new location is empty. Best-effort: never throws — worst case the caller
        /// creates a fresh database via EnsureCreated().
        /// </summary>
        public static void EnsureDatabaseMigrated()
        {
            try
            {
                var target = GetDbPath();
                if (File.Exists(target)) return;
                var legacy = FindLegacyDbPath();
                if (legacy is null) return;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(legacy, target);
            }
            catch
            {
                // Migration is a convenience, not a requirement.
            }
        }
    }
}
