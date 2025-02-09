 
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Models;
using System.Collections.Generic;

namespace TaskManagementSystem.Controllers
{
    public class TaskController : Controller
    {
        private readonly ILogger<TaskController> _logger;
        private readonly ITaskRepository _taskRepository;

        public TaskController(ILogger<TaskController> logger,ITaskRepository taskRepository)
        {
            _logger = logger;
            _taskRepository = taskRepository;
        }

        //public async Task<IActionResult> Index()
        //{
        //    //    IEnumerable<Task> tasks;
        //    //    if (!string.IsNullOrEmpty(searchTerm))
        //    //    {
        //    //        tasks = _taskRepository.SearchTasks(searchTerm);
        //    //    }
        //    //    else
        //    //    {
        //    //        tasks = _taskRepository.GetAllTasks();
        //    //    }
        //    //    return View(tasks);
        //    _logger.LogInformation("Index action in TaskController was called.");
        //    IEnumerable<TaskItem> result= await this._taskRepository.GetAllTasksWithoutSearch();
        //    return View(result);
        //}
        [HttpGet]
        public IActionResult Create()
        {
            //IEnumerable<TaskItem> result = await this._taskRepository.GetAllTasksWithoutSearch();
            return View();
        }

        [HttpPost]
        public IActionResult Create([FromBody] TaskItem task)
        {
            if (ModelState.IsValid)
            {
                _taskRepository.AddTask(task);
                return Json(new { success = true, message = "Task created successfully!" });
            }
            return BadRequest("Invalid data provided.");
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _taskRepository.GetTaskById(id);

            if (task == null)
            {
                return NotFound();
            }

            return View(task); 
        }


        [HttpPost]
        public IActionResult Edit([FromBody] TaskItem task)
        {
            if (ModelState.IsValid)
            {
                _taskRepository.UpdateTask(task);
                return RedirectToAction("Index");
            }
            return View(task);
        }


        public IActionResult Delete(int id)
        {
            var task = _taskRepository.GetTaskById(id);
            if (task == null)
            {
                return NotFound();
            }
            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _taskRepository.DeleteTask(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index(string searchTerm, int page = 1, int pageSize = 10)
        {
            var tasks = await _taskRepository.GetAllTasks(searchTerm, page, pageSize);

            ViewData["SearchTerm"] = searchTerm;

            return View(tasks);
        }

        public async Task<IActionResult> TaskStatusReport()
        {
            var report = await _taskRepository.GetTaskStatusReport();
            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> ExportReport(string format)
        {
            var report = await _taskRepository.GetTaskStatusReport();

            if (format == "excel")
            {
                var excelContent = _taskRepository.GenerateExcel(report);
                return File(excelContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TaskStatusReport.xlsx");
            }

            return BadRequest();
        }

    }
}