namespace Cadence.Core.Models
{
    public class Cycle
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CycleThemeId { get; set; }
        public CycleTheme? CycleTheme { get; set; }
        public ICollection<Area> Areas { get; set; } = new List<Area>();
        public TimeSpan TargetLength { get; set; }
        public CyclePhase Phase { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? PausedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public TimeSpan? ActualDuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}