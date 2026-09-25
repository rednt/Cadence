
namespace Cadence.Core.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? AreaId { get; set; }
        public Area? Area { get; set; }
        public string? BlockLabel { get; set; }
        public TimeOnly? DueAt { get; set; }
        public TaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }

        public TaskItem() { }
        public TaskItem(int id, string title, int? areaId, TimeOnly? dueAt, TaskStatus status = TaskStatus.Pending, TaskPriority priority = TaskPriority.Normal)
        {
            Id = id;
            Title = title;
            AreaId = areaId;
            DueAt = dueAt;
            Status = status;
            Priority = priority;
        }
    }
}