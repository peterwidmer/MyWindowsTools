using Microsoft.AspNetCore.Mvc;
using TaskRunner.Host.Services;

namespace TaskRunner.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly TaskExecutionService _taskService;

    public TasksController(TaskExecutionService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Gets all available task types.
    /// </summary>
    [HttpGet("types")]
    public IActionResult GetTaskTypes()
    {
        var types = _taskService.GetAvailableTaskTypes();
        return Ok(types);
    }

    /// <summary>
    /// Gets all currently running tasks.
    /// </summary>
    [HttpGet("running")]
    public IActionResult GetRunningTasks()
    {
        var tasks = _taskService.GetRunningTasks();
        return Ok(tasks);
    }

    /// <summary>
    /// Starts a new background task.
    /// </summary>
    [HttpPost("start")]
    public IActionResult StartTask([FromBody] StartTaskRequest request)
    {
        try
        {
            var taskId = _taskService.StartTask(request.TaskType, request.Parameters ?? new());
            return Ok(new { TaskId = taskId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a running task.
    /// </summary>
    [HttpPost("{taskId}/cancel")]
    public IActionResult CancelTask(string taskId)
    {
        var cancelled = _taskService.CancelTask(taskId);
        if (cancelled)
        {
            return Ok(new { Message = "Task cancellation requested" });
        }
        return NotFound(new { Error = "Task not found or already completed" });
    }
}

public class StartTaskRequest
{
    public required string TaskType { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
}
