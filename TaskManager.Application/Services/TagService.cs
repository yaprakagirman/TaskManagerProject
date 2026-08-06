using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Services;

public class TagService : ITagService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TagService> _logger;

    public TagService(
        IRepository<Tag> tagRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TagService> logger)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<TagResponse>> GetAllAsync()
    {
        var tags = await _tagRepository.GetAllAsync();

        _logger.LogInformation(
            "All tags listed. Count: {TagCount}",
            tags.Count);

        return _mapper.Map<List<TagResponse>>(tags);
    }

    public async Task<TagResponse> GetByIdAsync(int id)
    {
        var tag = await GetTagOrThrowAsync(id);

        _logger.LogInformation(
            "Tag retrieved successfully. TagId: {TagId}",
            id);

        return _mapper.Map<TagResponse>(tag);
    }

    public async Task<TagResponse> CreateAsync(TagRequest request)
    {
        var normalizedName = NormalizeName(request.Name);

        await EnsureNameIsAvailableAsync(normalizedName);

        var tag = _mapper.Map<Tag>(request);

        tag.Name = normalizedName;

        await _tagRepository.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Tag created successfully. TagId: {TagId}, TagName: {TagName}",
            tag.Id,
            tag.Name);

        return _mapper.Map<TagResponse>(tag);
    }

    public async Task UpdateAsync(
        int id,
        TagRequest request)
    {
        var tag = await GetTagOrThrowAsync(id);
        var normalizedName = NormalizeName(request.Name);

        await EnsureNameIsAvailableAsync(
            normalizedName,
            id);

        _mapper.Map(request, tag);

        tag.Name = normalizedName;

        _tagRepository.Update(tag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Tag updated successfully. TagId: {TagId}, TagName: {TagName}",
            tag.Id,
            tag.Name);
    }

    public async Task DeleteAsync(int id)
    {
        var tag = await GetTagOrThrowAsync(id);

        _tagRepository.Delete(tag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Tag deleted successfully. TagId: {TagId}",
            id);
    }

    private async Task<Tag> GetTagOrThrowAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);

        if (tag is not null)
        {
            return tag;
        }

        _logger.LogWarning(
            "Tag not found. TagId: {TagId}",
            id);

        throw new NotFoundException(
            "Tag was not found.");
    }

    private async Task EnsureNameIsAvailableAsync(
        string normalizedName,
        int? currentTagId = null)
    {
        var tagsWithSameName = await _tagRepository.FindAsync(
            tag => tag.Name == normalizedName
                && (!currentTagId.HasValue
                    || tag.Id != currentTagId.Value));

        if (!tagsWithSameName.Any())
        {
            return;
        }

        _logger.LogWarning(
            "Tag name is already in use. TagName: {TagName}",
            normalizedName);

        throw new ConflictException(
            "A tag with this name already exists.");
    }

    private static string NormalizeName(string name)
    {
        return name
            .Trim()
            .ToLowerInvariant();
    }
}
