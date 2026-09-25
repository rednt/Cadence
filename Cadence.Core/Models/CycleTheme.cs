namespace Cadence.Core.Models
{
    public class CycleTheme
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public ICollection<int> DefaultAreaIds { get; set; } = new List<int>();
        public TimeSpan DefaultLength { get; set; }
        public bool IsBuiltIn { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Cycle> Cycles { get; set; } = new List<Cycle>();
    }
}