using TaskManagementSystem.Models;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllTasks(string search, int page, int pageSize);
    Task<int> GetTotalTasks(string search);
    Task<TaskItem> GetTaskById(int id); 
    Task<int> AddTask(TaskItem taskItem);
    Task<int> UpdateTask(TaskItem taskItem); 
    Task<int> DeleteTask(int id);
    Task<IEnumerable<TaskItem>> GetAllTasksWithoutSearch(int page = 1, int pageSize = 10);
    Task<TaskStatusReport> GetTaskStatusReport();
    byte[] GenerateExcel(TaskStatusReport report);
   // byte[] GeneratePdf(TaskStatusReport report);
}
