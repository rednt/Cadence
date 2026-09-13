using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cadence.Core.Models;
namespace Cadence.Infrastructure.Routines
{
    public sealed class JsonRoutineLoader
    {
        private sealed class RoutineFileDto
        {
            public string? Profile { get; set; }
            public List<BlockDto>? Blocks { get; set; }
        }
        private sealed class BlockDto
        {
            public string? Label { get; set; }
            public BlockRole? Role { get; set; }
            public string? Time { get; set; }
        }

        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter<BlockRole>(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
        };

        public IReadOnlyList<Block> Parse(string json)
        {
            var dto = JsonSerializer.Deserialize<RoutineFileDto>(json, _options) ?? throw new InvalidOperationException("Failed to deserialize routine file.");
            if (dto.Blocks is null || dto.Blocks.Count == 0)
            {
                throw new InvalidOperationException("Routine file contains no blocks.");
            }

            var blocks = dto.Blocks.Select(b => new Block(
            TimeOnly.Parse(b.Time ?? throw new InvalidOperationException("Block time is missing."), CultureInfo.InvariantCulture),
            b.Label ?? string.Empty,
            b.Role ?? BlockRole.Unspecified)).ToList();

            var duplicate = blocks.GroupBy(b => b.StartTime).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"Duplicate block start time '{duplicate.Key:HH:mm}' - start times must be unique.");
            }

            return blocks;
        }
        public IReadOnlyList<Block> Load(string path) => Parse(File.ReadAllText(path));

        /// <summary>
        /// Resolve order: user-editable %LOCALAPPDATA%\Cadence\routine.json first,
        /// embedded default second, loose file next to the .exe last (dev/manual drop).
        /// Seeds the user copy from the embedded default on first run (best-effort).
        /// Restart the worker after editing the routine — no hot-reload in v1.
        /// </summary>
        public IReadOnlyList<Block> LoadDefault()
        {
            var userPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Cadence", "routine.json");
            if (File.Exists(userPath))
                return Load(userPath);

            var embedded = GetEmbeddedJson();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(userPath)!);
                File.WriteAllText(userPath, embedded);
            }
            catch
            {
                // Seeding is a convenience; embedded content still loads below.
            }
            try
            {
                return Parse(embedded);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException("Embedded default routine is invalid.", ex);
            }
        }

        private static string GetEmbeddedJson()
        {
            var assembly = typeof(JsonRoutineLoader).Assembly;
            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("default.json", StringComparison.OrdinalIgnoreCase));
            if (name is not null)
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            // Dev fallback: loose file next to the .exe (pre-publish layout).
            var loose = Path.Combine(AppContext.BaseDirectory, "Routines", "default.json");
            if (File.Exists(loose))
                return File.ReadAllText(loose);
            throw new InvalidOperationException(
                "Default routine not found (no embedded resource, no Routines/default.json next to the .exe).");
        }

    }

}