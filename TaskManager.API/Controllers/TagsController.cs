using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : BaseApiController
{
    private readonly ITagService _tagService;
    private readonly IValidator<TagRequest> _tagValidator;

    public TagsController(
    ITagService tagService,
    IValidator<TagRequest> tagValidator)
    {
        _tagService = tagService;
        _tagValidator = tagValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagResponse>>> GetAll()
    {
        var tags = await _tagService.GetAllAsync();

        return Ok(tags);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TagResponse>> GetById(int id)
    {
        var tag = await _tagService.GetByIdAsync(id);

        return Ok(tag);
    }

    [HttpPost]
    [ProducesResponseType(
    typeof(TagResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<TagResponse>> Create(
    [FromBody] TagRequest request)
    {
        return ValidateAndExecute<TagRequest, TagResponse>(
            request,
            _tagValidator,
            async () =>
            {
                var createdTag =
                    await _tagService.CreateAsync(request);

                return Ok(createdTag);
            });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Update(
    int id,
    [FromBody] TagRequest request)
    {
        return ValidateAndExecuteNoContent(
            request,
            _tagValidator,
            () => _tagService.UpdateAsync(
                id,
                request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _tagService.DeleteAsync(id);

        return NoContent();
    }
}
