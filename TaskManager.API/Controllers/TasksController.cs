using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;

using System.Security.Claims;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : BaseApiController
{
    private readonly ITaskService _taskService;
    private readonly ITaskAssignmentService _taskAssignmentService;
    private readonly ITaskHierarchyService _taskHierarchyService;
    private readonly IValidator<CreateTaskRequest> _createTaskValidator;
    private readonly IValidator<UpdateTaskRequest> _updateTaskValidator;
    private readonly IValidator<AssignTaskRequest> _assignTaskValidator;
    private readonly IValidator<UpdateTaskStatusRequest> _updateTaskStatusValidator;

    public TasksController(
        ITaskService taskService,
        ITaskAssignmentService taskAssignmentService,
        ITaskHierarchyService taskHierarchyService,
        IValidator<CreateTaskRequest> createTaskValidator,
        IValidator<UpdateTaskRequest> updateTaskValidator,
        IValidator<AssignTaskRequest> assignTaskValidator,
        IValidator<UpdateTaskStatusRequest> updateTaskStatusValidator)
    {
        _taskService = taskService;
        _taskAssignmentService = taskAssignmentService;
        _taskHierarchyService = taskHierarchyService;
        _createTaskValidator = createTaskValidator;
        _updateTaskValidator = updateTaskValidator;
        _assignTaskValidator = assignTaskValidator;
        _updateTaskStatusValidator = updateTaskStatusValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> GetAll()
    {
        var tasks = await _taskService.GetAllTasksAsync();

        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDetailResponse>> GetById(
    int id)
    {
        var taskDetail = await _taskService
            .GetTaskDetailAsync(id);

        if (taskDetail is null)
        {
            return NotFound();
        }

        return Ok(taskDetail);
    }

    [HttpGet("{id:int}/tree")]
    public async Task<ActionResult<TaskTreeResponse>> GetTree(int id)
    {
        var result = await _taskHierarchyService.GetTaskTreeAsync(id);

        return Ok(result);
    }

    [HttpPost]
    public Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request)
    {
        return ValidateAndExecute<CreateTaskRequest, TaskResponse>(
            request,
            _createTaskValidator,
            async () =>
            {
                var createdByUserId = GetAuthenticatedUserId();
                var response = await _taskService.CreateTaskAsync(
                    request,
                    createdByUserId);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = response.Id },
                    response);
            });
    }

    private int GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            throw new UnauthorizedException(
                "Authenticated user ID claim is missing or invalid.");
        }

        return userId;
    }

    [HttpPut("{id}")]
    public Task<ActionResult<TaskResponse>> Update(int id, [FromBody] UpdateTaskRequest request)
    {
        return ValidateAndExecute<UpdateTaskRequest, TaskResponse>(
            request,
            _updateTaskValidator,
            async () =>
            {
                var response = await _taskService.UpdateTaskAsync(id, request);

                if (response is null)
                {
                    return NotFound();
                }

                return Ok(response);
            });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var isDeleted = await _taskService.DeleteTaskAsync(id);

        if (!isDeleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{taskId}/assign")]
    public Task<ActionResult<TaskResponse>> Assign(int taskId, [FromBody] AssignTaskRequest request)
    {
        return ValidateAndExecute<AssignTaskRequest, TaskResponse>(
            request,
            _assignTaskValidator,
            async () =>
            {
                var response = await _taskAssignmentService.AssignTaskAsync(taskId, request);

                if (response is null)
                {
                    return NotFound();
                }

                return Ok(response);
            });
    }

    [HttpPut("{taskId}/status")]
    public Task<ActionResult<TaskResponse>> UpdateStatus(
        int taskId,
        [FromBody] UpdateTaskStatusRequest request)
    {
        return ValidateAndExecute<UpdateTaskStatusRequest, TaskResponse>(
            request,
            _updateTaskStatusValidator,
            async () =>
            {
                var response = await _taskService.UpdateTaskStatusAsync(taskId, request);

                if (response is null)
                {
                    return NotFound();
                }

                return Ok(response);
            });
    }
}
