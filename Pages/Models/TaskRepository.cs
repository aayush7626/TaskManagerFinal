using Dapper;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using TaskManagementSystem.Models;
using System.ComponentModel;
using OfficeOpenXml;


public class TaskRepository : ITaskRepository
{
    private readonly ILogger<TaskRepository> _logger;
    private readonly string _connectionString;

    public TaskRepository(ILogger<TaskRepository> logger,IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    private IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<IEnumerable<TaskItem>> GetAllTasks(string search, int page, int pageSize)
    {
        try
        {
            using var connection = CreateConnection();

            string searchCondition = string.IsNullOrEmpty(search) ? "%" : $"%{search}%";

            string sql = @"
        SELECT * FROM Tasks
        WHERE Title LIKE @Search OR Status LIKE @Search
        ORDER BY Id DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            return await connection.QueryAsync<TaskItem>(sql, new
            {
                Search = searchCondition, // Use the search condition
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetAllTasks");
            throw;
        }
    }


    // Get total count of tasks (for pagination)
    public async Task<int> GetTotalTasks(string search)
    {
        using var connection = CreateConnection();
        string sql = "SELECT COUNT(*) FROM Tasks WHERE Title LIKE @Search OR Description LIKE @Search;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Search = $"%{search}%" });
    }

    // Get a single task by ID
    public async Task<TaskItem> GetTaskById(int id)
    {
        using var connection = CreateConnection();
        string sql = "SELECT * FROM Tasks WHERE Id = @Id;";
        return await connection.QueryFirstOrDefaultAsync<TaskItem>(sql, new { Id = id });
    }

    // Add a new task and return the inserted ID
    public async Task<int> AddTask(TaskItem taskItem)
    {
        using var connection = CreateConnection();
        string sql = @"
        INSERT INTO Tasks (Title, Description, Status, DueDate, CreatedAt)
        VALUES (@Title, @Description, @Status, @DueDate, @CreatedAt);
        SELECT SCOPE_IDENTITY();";
        taskItem.CreatedAt = DateTime.UtcNow;

        return await connection.ExecuteScalarAsync<int>(sql, taskItem);
    }



    // Update an existing task and return affected rows
    public async Task<int> UpdateTask(TaskItem taskItem)
    {
        using var connection = CreateConnection();
        string sql = @"
        UPDATE Tasks
        SET Title = @Title, Description = @Description, Status = @Status, DueDate = @DueDate, @CreatedAt= @CreatedAt
        WHERE Id = @Id;";

        return await connection.ExecuteAsync(sql, taskItem);
    }


    // Delete a task and return affected rows
    public async Task<int> DeleteTask(int id)
    {
        using var connection = CreateConnection();
        string sql = "DELETE FROM Tasks WHERE Id = @Id;";
        return await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<TaskItem>> GetAllTasksWithoutSearch(int page = 1, int pageSize = 10)
    {
        using var connection = CreateConnection();

        string sql = @"
    SELECT * FROM Tasks
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        return await connection.QueryAsync<TaskItem>(sql, new
        {
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });
    }

    public async Task<TaskStatusReport> GetTaskStatusReport()
    {
        try
        {
            using var connection = CreateConnection();

            
            string countQuery = @"
        SELECT 
            COUNT(*) AS TotalTasks,
            SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingTasks,
            SUM(CASE WHEN Status = 'In Progress' THEN 1 ELSE 0 END) AS InProgressTasks,
            SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedTasks
        FROM Tasks";

            var countResult = await connection.QueryFirstOrDefaultAsync(countQuery);

             
            string taskQuery = @"SELECT * FROM Tasks ORDER BY Status";
            var tasks = (await connection.QueryAsync<TaskItem>(taskQuery)).ToList();

            var report = new TaskStatusReport
            {
                TotalTasks = countResult.TotalTasks,
                PendingTasks = countResult.PendingTasks,
                InProgressTasks = countResult.InProgressTasks,
                CompletedTasks = countResult.CompletedTasks,
                CompletedPercentage = countResult.TotalTasks > 0 ? ((double)countResult.CompletedTasks / countResult.TotalTasks) * 100 : 0,
                Tasks = tasks  
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating task status report");
            throw;
        }
    }

    public byte[] GenerateExcel(TaskStatusReport report)
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        using (var package = new ExcelPackage())
        {
           
            var summaryWorksheet = package.Workbook.Worksheets.Add("Task Status Report");

             
            summaryWorksheet.Cells[1, 1].Value = "Total Tasks";
            summaryWorksheet.Cells[1, 2].Value = "Pending Tasks";
            summaryWorksheet.Cells[1, 3].Value = "In Progress Tasks";
            summaryWorksheet.Cells[1, 4].Value = "Completed Tasks";
            summaryWorksheet.Cells[1, 5].Value = "Completed Percentage";

             
            summaryWorksheet.Cells[2, 1].Value = report.TotalTasks;
            summaryWorksheet.Cells[2, 2].Value = report.PendingTasks;
            summaryWorksheet.Cells[2, 3].Value = report.InProgressTasks;
            summaryWorksheet.Cells[2, 4].Value = report.CompletedTasks;
            summaryWorksheet.Cells[2, 5].Value = report.CompletedPercentage;

            
            var detailsWorksheet = package.Workbook.Worksheets.Add("Task Details");

             
            detailsWorksheet.Cells[1, 1].Value = "Task Title";
            detailsWorksheet.Cells[1, 2].Value = "Description";
            detailsWorksheet.Cells[1, 3].Value = "Due Date";
            detailsWorksheet.Cells[1, 4].Value = "Status";

            // Add task details data
            int row = 2;
            foreach (var task in report.Tasks)
            {
                detailsWorksheet.Cells[row, 1].Value = task.Title;
                detailsWorksheet.Cells[row, 2].Value = task.Description;
                detailsWorksheet.Cells[row, 3].Value = task.DueDate.ToString("yyyy-MM-dd");
                detailsWorksheet.Cells[row, 4].Value = task.Status;
                row++;
            }

            // Return the byte array representing the Excel file
            return package.GetAsByteArray();
        }
    }



}
