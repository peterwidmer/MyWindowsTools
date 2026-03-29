using Microsoft.AspNetCore.Mvc;
using TaskRunner.Host.Services;

namespace TaskRunner.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly OperationExecutionService _operationService;

    public OperationsController(OperationExecutionService operationService)
    {
        _operationService = operationService;
    }

    /// <summary>
    /// Gets all available operation types.
    /// </summary>
    [HttpGet("types")]
    public IActionResult GetOperationTypes()
    {
        var types = _operationService.GetAvailableOperationTypes();
        return Ok(types);
    }

    /// <summary>
    /// Executes a synchronous operation.
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteOperation([FromBody] ExecuteOperationRequest request)
    {
        var result = await _operationService.ExecuteAsync(request.OperationType, request.Parameters ?? new());
        return Ok(result);
    }
}

public class ExecuteOperationRequest
{
    public required string OperationType { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
}
