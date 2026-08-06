using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using FluentValidation;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : BaseApiController
{
    private readonly IProjectService _projectService;
    private readonly IValidator<CreateProjectRequest>
        _createProjectValidator;
    private readonly IValidator<UpdateProjectRequest>
        _updateProjectValidator;

    public ProjectsController(
        IProjectService projectService,
        IValidator<CreateProjectRequest> createProjectValidator,
        IValidator<UpdateProjectRequest> updateProjectValidator)
    {
        _projectService = projectService;
        _createProjectValidator = createProjectValidator;
        _updateProjectValidator = updateProjectValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetAll()
    {
        var projects = await _projectService.GetAllAsync();

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectResponse>> GetById(int id)
    {
        var project = await _projectService.GetByIdAsync(id);

        return Ok(project);
    }

    [HttpGet("{id}/tasks")]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> GetTasks(int id)
    {
        var tasks = await _projectService.GetTasksAsync(id);

        return Ok(tasks);
    }

    [HttpPost]
    public Task<ActionResult<ProjectResponse>> Create(
    [FromBody] CreateProjectRequest request)
    {
        return ValidateAndExecute<
            CreateProjectRequest,
            ProjectResponse>(
            request,
            _createProjectValidator,
            async () =>
            {
                var response =
                    await _projectService.CreateAsync(request);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = response.Id },
                    response);
            });
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<ProjectResponse>> Update(
    int id,
    [FromBody] UpdateProjectRequest request)
    {
        return ValidateAndExecute<
            UpdateProjectRequest,
            ProjectResponse>(
            request,
            _updateProjectValidator,
            async () =>
            {
                await _projectService.UpdateAsync(id, request);

                return Ok();
            });
    }
}
