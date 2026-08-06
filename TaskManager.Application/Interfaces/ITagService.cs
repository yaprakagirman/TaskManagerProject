using TaskManager.Application.DTOs.Tags;

namespace TaskManager.Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagResponse>> GetAllAsync();

    Task<TagResponse> GetByIdAsync(int id);

    Task<TagResponse> CreateAsync(TagRequest request);

    Task UpdateAsync(
        int id,
        TagRequest request);

    Task DeleteAsync(int id);
}
