using System.Text.Json.Serialization;

namespace TaskManagementSystem.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime DueDate { get; set; }

        [JsonIgnore] 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
    }

}