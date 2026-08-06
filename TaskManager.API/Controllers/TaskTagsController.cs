using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;
using FluentValidation;
namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/tasktags")]
[Authorize]
public class TaskTagsController : BaseApiController
{
    private readonly ITaskTagService _taskTagService;
    private readonly IValidator<TagTasksRequest> _tagTasksValidator;
    private readonly IValidator<UpdateTaskTagRequest>
        _updateTaskTagValidator;

    public TaskTagsController(
        ITaskTagService taskTagService,
        IValidator<TagTasksRequest> tagTasksValidator,
        IValidator<UpdateTaskTagRequest> updateTaskTagValidator)
    {
        _taskTagService = taskTagService;
        _tagTasksValidator = tagTasksValidator;
        _updateTaskTagValidator = updateTaskTagValidator;
    }
    [HttpGet]
    [ProducesResponseType(
        typeof(IEnumerable<TaskTagResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskTagResponse>>> GetAll(
        [FromQuery] int? taskId = null,
        [FromQuery] int? tagId = null)
    {
        var taskTags = await _taskTagService.GetAllAsync(
            taskId,
            tagId);

        return Ok(taskTags);
    }

    [HttpPost("tags/{tagId:int}/tasks")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AddTasks(
    int tagId,
    [FromBody] TagTasksRequest request)
    {
        return ValidateAndExecuteNoContent(
            request,
            _tagTasksValidator,
            () => _taskTagService.AddTasksToTagAsync(
                tagId,
                request));
    }

    [HttpDelete("{taskTagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(int taskTagId)
    {
        await _taskTagService.RemoveAsync(taskTagId);

        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var taskTag = await _taskTagService.GetByIdAsync(id);

        return Ok(taskTag);
    }

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(
    int id,
    [FromBody] UpdateTaskTagRequest request)
    {
        return ValidateAndExecute(
            request,
            _updateTaskTagValidator,
            async () =>
            {
                var updatedTaskTag =
                    await _taskTagService.UpdateAsync(id, request);

                return Ok(updatedTaskTag);
            });
    }
}
