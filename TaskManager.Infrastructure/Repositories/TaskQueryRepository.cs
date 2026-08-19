using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Common;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public sealed class TaskQueryRepository : ITaskQueryRepository
{
    private readonly AppDbContext _context;

    public TaskQueryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<TaskResponse>> GetPagedAsync(
        TaskQueryRequest request,
        int? assignedUserId = null,
        bool deletedOnly = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TaskItem> query = _context.Tasks.AsNoTracking();
        if (deletedOnly)
        {
            query = query.IgnoreQueryFilters().Where(task => task.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(task =>
                EF.Functions.ILike(task.Title, pattern) ||
                (task.Description != null && EF.Functions.ILike(task.Description, pattern)));
        }

        if (request.Status.HasValue)
            query = query.Where(task => task.Status == request.Status.Value);
        if (request.Priority.HasValue)
            query = query.Where(task => task.Priority == request.Priority.Value);
        if (request.ProjectId.HasValue)
            query = query.Where(task => task.ProjectId == request.ProjectId.Value);
        if (request.TagId.HasValue)
            query = query.Where(task => task.TaskTags.Any(tag => tag.TagId == request.TagId.Value));

        var effectiveAssignedUserId = assignedUserId ?? request.AssignedUserId;
        if (effectiveAssignedUserId.HasValue)
            query = query.Where(task => task.TaskAssignments.Any(assignment => assignment.AssignedUserId == effectiveAssignedUserId.Value));

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplyOrdering(query, request.SortBy, request.SortDirection);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(task => new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                CreatedDate = task.CreatedDate,
                DueDate = task.DueDate,
                CreatedByUserId = task.CreatedByUserId,
                ProjectId = task.ProjectId,
                ParentTaskId = task.ParentTaskId
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponse>
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            CurrentPage = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task RestoreAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == taskId, cancellationToken)
            ?? throw new NotFoundException("Task was not found.");

        if (!task.IsDeleted)
            throw new ConflictException("Task is already active.");

        if (task.ProjectId.HasValue && !await _context.Projects.AnyAsync(project => project.Id == task.ProjectId.Value, cancellationToken))
            throw new BadRequestException("Task cannot be restored because its project is deleted or missing.");
        if (task.ParentTaskId.HasValue && !await _context.Tasks.AnyAsync(parent => parent.Id == task.ParentTaskId.Value, cancellationToken))
            throw new BadRequestException("Task cannot be restored because its parent task is deleted or missing.");
        if (!await _context.Users.AnyAsync(user => user.Id == task.CreatedByUserId, cancellationToken))
            throw new BadRequestException("Task cannot be restored because its creator is deleted or missing.");

        task.IsDeleted = false;
        task.DeletedDate = null;
        task.DeleterId = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<TaskItem> ApplyOrdering(IQueryable<TaskItem> query, string sortBy, string direction)
    {
        var ascending = direction.Equals("asc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "id" => ascending ? query.OrderBy(task => task.Id) : query.OrderByDescending(task => task.Id),
            "title" => ascending ? query.OrderBy(task => task.Title).ThenBy(task => task.Id) : query.OrderByDescending(task => task.Title).ThenBy(task => task.Id),
            "status" => ascending ? query.OrderBy(task => task.Status).ThenBy(task => task.Id) : query.OrderByDescending(task => task.Status).ThenBy(task => task.Id),
            "priority" => ascending ? query.OrderBy(task => task.Priority).ThenBy(task => task.Id) : query.OrderByDescending(task => task.Priority).ThenBy(task => task.Id),
            "duedate" => ascending ? query.OrderBy(task => task.DueDate).ThenBy(task => task.Id) : query.OrderByDescending(task => task.DueDate).ThenBy(task => task.Id),
            _ => ascending ? query.OrderBy(task => task.CreatedDate).ThenBy(task => task.Id) : query.OrderByDescending(task => task.CreatedDate).ThenBy(task => task.Id)
        };
    }
}
