using System.Text.Json.Serialization;

namespace TaskManagementSystem.Models
{
    public class TaskStatusReport
    {
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double CompletedPercentage { get; set; }
        public List<TaskItem> Tasks { get; set; } = new();
    }

}