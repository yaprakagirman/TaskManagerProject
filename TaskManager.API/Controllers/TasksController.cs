using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Common;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : BaseApiController
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITaskAssignmentService _taskAssignmentService;
    private readonly ITaskHierarchyService _taskHierarchyService;
    private readonly IValidator<CreateTaskRequest> _createTaskValidator;
    private readonly IValidator<UpdateTaskRequest> _updateTaskValidator;
    private readonly IValidator<AssignTaskRequest> _assignTaskValidator;
    private readonly IValidator<UpdateTaskStatusRequest> _updateTaskStatusValidator;
    private readonly IValidator<TaskQueryRequest> _taskQueryValidator;
    private readonly IValidator<TransferTaskAssignmentRequest> _transferValidator;

    public TasksController(
        ITaskService taskService,
        ICurrentUserService currentUserService,
        ITaskAssignmentService taskAssignmentService,
        ITaskHierarchyService taskHierarchyService,
        IValidator<CreateTaskRequest> createTaskValidator,
        IValidator<UpdateTaskRequest> updateTaskValidator,
        IValidator<AssignTaskRequest> assignTaskValidator,
        IValidator<UpdateTaskStatusRequest> updateTaskStatusValidator,
        IValidator<TaskQueryRequest> taskQueryValidator,
        IValidator<TransferTaskAssignmentRequest> transferValidator)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
        _taskAssignmentService = taskAssignmentService;
        _taskHierarchyService = taskHierarchyService;
        _createTaskValidator = createTaskValidator;
        _updateTaskValidator = updateTaskValidator;
        _assignTaskValidator = assignTaskValidator;
        _updateTaskStatusValidator = updateTaskStatusValidator;
        _taskQueryValidator = taskQueryValidator;
        _transferValidator = transferValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TaskResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<TaskResponse>>> GetAll(
        [FromQuery] TaskQueryRequest request,
        CancellationToken cancellationToken) =>
        ValidateAndExecute<TaskQueryRequest, PagedResponse<TaskResponse>>(
            request,
            _taskQueryValidator,
            async () => Ok(await _taskService.GetTasksAsync(request, cancellationToken)));

    [HttpGet("my-tasks")]
    [ProducesResponseType(typeof(PagedResponse<TaskResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<TaskResponse>>> GetMyTasks(
        [FromQuery] TaskQueryRequest request,
        CancellationToken cancellationToken) =>
        ValidateAndExecute<TaskQueryRequest, PagedResponse<TaskResponse>>(
            request,
            _taskQueryValidator,
            async () => Ok(await _taskService.GetMyTasksAsync(request, GetAuthenticatedUserId(), cancellationToken)));

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResponse<TaskResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<TaskResponse>>> GetDeleted(
        [FromQuery] TaskQueryRequest request,
        CancellationToken cancellationToken) =>
        ValidateAndExecute<TaskQueryRequest, PagedResponse<TaskResponse>>(
            request,
            _taskQueryValidator,
            async () => Ok(await _taskService.GetDeletedTasksAsync(request, cancellationToken)));

    [HttpPatch("{id:int}/restore")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        await _taskService.RestoreTaskAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDetailResponse>> GetById(int id)
    {
        var taskDetail = await _taskService.GetTaskDetailAsync(id);
        return taskDetail is null ? NotFound() : Ok(taskDetail);
    }

    [HttpGet("{id:int}/tree")]
    public async Task<ActionResult<TaskTreeResponse>> GetTree(int id) =>
        Ok(await _taskHierarchyService.GetTaskTreeAsync(id));

    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request) =>
        ValidateAndExecute<CreateTaskRequest, TaskResponse>(
            request,
            _createTaskValidator,
            async () => Ok(await _taskService.CreateTaskAsync(request, GetAuthenticatedUserId())));

    [HttpPut("{id:int}")]
    public Task<ActionResult<TaskResponse>> Update(int id, [FromBody] UpdateTaskRequest request) =>
        ValidateAndExecute<UpdateTaskRequest, TaskResponse>(
            request,
            _updateTaskValidator,
            async () =>
            {
                var response = await _taskService.UpdateTaskAsync(id, request);
                return response is null ? NotFound() : Ok(response);
            });

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await _taskService.DeleteTaskAsync(id) ? NoContent() : NotFound();

    [HttpPost("{taskId:int}/assign")]
    public Task<ActionResult<TaskResponse>> Assign(int taskId, [FromBody] AssignTaskRequest request) =>
        ValidateAndExecute<AssignTaskRequest, TaskResponse>(
            request,
            _assignTaskValidator,
            async () =>
            {
                var response = await _taskAssignmentService.AssignTaskAsync(taskId, request);
                return response is null ? NotFound() : Ok(response);
            });

    [HttpDelete("{taskId:int}/assignments/{userId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveAssignment(int taskId, int userId)
    {
        await _taskAssignmentService.RemoveAssignmentAsync(taskId, userId);
        return NoContent();
    }

    [HttpPatch("{taskId:int}/assignments/transfer")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> TransferAssignment(
        int taskId,
        [FromBody] TransferTaskAssignmentRequest request) =>
        ValidateAndExecuteNoContent(
            request,
            _transferValidator,
            () => _taskAssignmentService.TransferAssignmentAsync(taskId, request));

    [HttpGet("{taskId:int}/eligible-users")]
    public async Task<ActionResult<IEnumerable<EligibleUserResponse>>> GetEligibleUsers(int taskId) =>
        Ok(await _taskAssignmentService.GetEligibleUsersAsync(taskId));

    [HttpPut("{taskId:int}/status")]
    public Task<ActionResult<TaskResponse>> UpdateStatus(
        int taskId,
        [FromBody] UpdateTaskStatusRequest request) =>
        ValidateAndExecute<UpdateTaskStatusRequest, TaskResponse>(
            request,
            _updateTaskStatusValidator,
            async () =>
            {
                var response = await _taskService.UpdateTaskStatusAsync(taskId, request);
                return response is null ? NotFound() : Ok(response);
            });

    private int GetAuthenticatedUserId() =>
        _currentUserService.UserId
        ?? throw new UnauthorizedException("Authenticated user ID claim is missing or invalid.");
}
