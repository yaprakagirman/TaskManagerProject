using Microsoft.EntityFrameworkCore;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public class TaskDetailRepository : ITaskDetailRepository
{
    private readonly AppDbContext _context;

    public TaskDetailRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskDetailResponse?> GetByIdAsync(
        int taskId)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .Include(task => task.Project)
            .Include(task => task.CreatedByUser)
            .Include(task => task.TaskTags)
                .ThenInclude(taskTag => taskTag.Tag)
            .Include(task => task.TaskAssignments)
                .ThenInclude(assignment => assignment.AssignedUser)
            .FirstOrDefaultAsync(task => task.Id == taskId);

        if (task is null)
        {
            return null;
        }

        return MapToResponse(task);
    }

    private static TaskDetailResponse MapToResponse(
        TaskItem task)
    {
        return new TaskDetailResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            CreatedDate = task.CreatedDate,
            DueDate = task.DueDate,

            CreatedByUserId = task.CreatedByUserId,

            CreatedByUserFullName = CreateFullName(
                task.CreatedByUser.FirstName,
                task.CreatedByUser.LastName),

            ProjectId = task.ProjectId,
            ProjectName = task.Project?.Name,

            ParentTaskId = task.ParentTaskId,

            Tags = task.TaskTags
                .OrderBy(taskTag => taskTag.Tag.Name)
                .Select(taskTag => new TagResponse
                {
                    Id = taskTag.Tag.Id,
                    Name = taskTag.Tag.Name
                })
                .ToList(),

            Assignments = task.TaskAssignments
                .OrderBy(assignment => assignment.AssignedDate)
                .Select(assignment => new TaskAssignmentResponse
                {
                    AssignedUserId =
                        assignment.AssignedUserId,

                    AssignedUserFullName = CreateFullName(
                        assignment.AssignedUser.FirstName,
                        assignment.AssignedUser.LastName),

                    AssignedDate =
                        assignment.AssignedDate,

                    IsCompleted =
                        assignment.IsCompleted,

                    CompletedDate =
                        assignment.CompletedDate
                })
                .ToList()
        };
    }

    private static string CreateFullName(
        string firstName,
        string lastName)
    {
        return $"{firstName} {lastName}".Trim();
    }
}