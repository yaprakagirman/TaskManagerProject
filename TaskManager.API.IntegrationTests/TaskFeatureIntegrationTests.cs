using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.DTOs.Common;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.API.IntegrationTests;

public sealed class TaskFeatureIntegrationTests : IClassFixture<TaskManagerApiFactory>
{
    private readonly TaskManagerApiFactory _factory;

    public TaskFeatureIntegrationTests(TaskManagerApiFactory factory)
    {
        _factory = factory;
    }

    // ───────────────────── Pagination / Filtering / Search / Sort ─────────────────────

    [Fact]
    public async Task Listing_PaginatesFiltersSearchesAndSortsInPostgreSql()
    {
        await ResetAndSeedAsync();
        using var client = CreateClient(1, "User");

        var page = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>(
            "/api/tasks?page=1&pageSize=2&sortBy=title&sortDirection=asc");
        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasNextPage);
        Assert.False(page.HasPreviousPage);
        Assert.Equal(["Alpha backend", "Beta frontend"], page.Items.Select(item => item.Title));

        var filtered = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>(
            "/api/tasks?status=InProgress&priority=High");
        Assert.Single(filtered!.Items);
        Assert.Equal("Beta frontend", filtered.Items[0].Title);

        var searched = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>(
            "/api/tasks?search=POSTGRES");
        Assert.Single(searched!.Items);
        Assert.Equal("Gamma API", searched.Items[0].Title);
    }

    // ───────────────────── /api/tasks/my-tasks JWT extraction ─────────────────────

    [Fact]
    public async Task MyTasks_UsesAuthenticatedClaimAndReturnsOnlyAssignments()
    {
        await ResetAndSeedAsync();
        using var client = CreateClient(1, "User");
        var result = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks/my-tasks");
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Contains(item.Id, new[] { 1, 2 }));
    }

    // ───────────────────── Assignment Removal (Admin-only) ─────────────────────

    [Fact]
    public async Task AssignmentRemoval_Anonymous_Returns401Unauthorized()
    {
        await ResetAndSeedAsync();
        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/tasks/1/assignments/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AssignmentRemoval_AuthenticatedNonAdmin_Returns403Forbidden()
    {
        await ResetAndSeedAsync();
        using var client = CreateClient(1, "User");
        var response = await client.DeleteAsync("/api/tasks/1/assignments/1");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignmentRemoval_Admin_SucceedsThenReturnsNotFoundForMissingAssignment()
    {
        await ResetAndSeedAsync();
        using var client = CreateClient(4, "Admin");
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/tasks/1/assignments/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/tasks/1/assignments/1")).StatusCode);
    }

    // ───────────────────── Assignment Transfer (Admin-only) ─────────────────────

    [Fact]
    public async Task AssignmentTransfer_Anonymous_Returns401Unauthorized()
    {
        await ResetAndSeedAsync();
        using var client = _factory.CreateClient();
        var response = await client.PatchAsJsonAsync(
            "/api/tasks/1/assignments/transfer",
            new TransferTaskAssignmentRequest { CurrentAssignedUserId = 1, NewAssignedUserId = 2 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AssignmentTransfer_AuthenticatedNonAdmin_Returns403Forbidden()
    {
        await ResetAndSeedAsync();
        using var client = CreateClient(1, "User");
        var response = await client.PatchAsJsonAsync(
            "/api/tasks/1/assignments/transfer",
            new TransferTaskAssignmentRequest { CurrentAssignedUserId = 1, NewAssignedUserId = 2 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignmentTransfer_Admin_SucceedsAtomically()
    {
        await ResetAndSeedAsync();
        using var client = CreateClient(4, "Admin");
        var response = await client.PatchAsJsonAsync(
            "/api/tasks/1/assignments/transfer",
            new TransferTaskAssignmentRequest { CurrentAssignedUserId = 1, NewAssignedUserId = 2 });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.TaskAssignments.AnyAsync(item => item.TaskItemId == 1 && item.AssignedUserId == 1));
        Assert.True(await db.TaskAssignments.AnyAsync(item => item.TaskItemId == 1 && item.AssignedUserId == 2));
    }

    [Fact]
    public async Task AssignmentTransfer_Admin_RejectsUserWithoutRequiredExpertiseAndKeepsCurrentAssignment()
    {
        await ResetAndSeedAsync();
        using var client = CreateClient(4, "Admin");
        var response = await client.PatchAsJsonAsync(
            "/api/tasks/1/assignments/transfer",
            new TransferTaskAssignmentRequest { CurrentAssignedUserId = 1, NewAssignedUserId = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.TaskAssignments.AnyAsync(item => item.TaskItemId == 1 && item.AssignedUserId == 1));
        Assert.False(await db.TaskAssignments.AnyAsync(item => item.TaskItemId == 1 && item.AssignedUserId == 3));
    }

    // ───────────────────── Deleted Task Administration (Admin-only) ─────────────────────

    [Fact]
    public async Task DeletedTaskAdministration_IsAdminOnlyAndRestoreMakesTaskActive()
    {
        await ResetAndSeedAsync();
        using var userClient = CreateClient(1, "User");
        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.GetAsync("/api/tasks/deleted")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.PatchAsync("/api/tasks/4/restore", null)).StatusCode);

        using var adminClient = CreateClient(4, "Admin");
        var deleted = await adminClient.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks/deleted");
        Assert.Single(deleted!.Items);
        Assert.Equal(4, deleted.Items[0].Id);
        Assert.Equal(HttpStatusCode.NoContent, (await adminClient.PatchAsync("/api/tasks/4/restore", null)).StatusCode);

        var active = await adminClient.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks?search=Deleted");
        Assert.Single(active!.Items);
        using var scope = _factory.Services.CreateScope();
        var restored = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Tasks.SingleAsync(task => task.Id == 4);
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DeletedDate);
        Assert.Null(restored.DeleterId);
    }

    // ───────────────────── Health Check ─────────────────────

    [Fact]
    public async Task Health_ReturnsHealthyWhenPostgreSqlIsAvailable()
    {
        await ResetAndSeedAsync();
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }

    // ───────────────────── Helpers ─────────────────────

    private HttpClient CreateClient(int userId, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(userId, role));
        return client;
    }

    private string CreateToken(int userId, string role)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_factory.TokenKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            "TaskManagerApi",
            "TaskManagerClient",
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task ResetAndSeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.TaskAssignments.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.TaskTags.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Tasks.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Tags.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Projects.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Users.IgnoreQueryFilters().ExecuteDeleteAsync();

        db.Users.AddRange(
            new User { Id = 1, FirstName = "Current", LastName = "User", Email = "current@test.local", PasswordHash = "x", Expertises = UserExpertise.Backend },
            new User { Id = 2, FirstName = "Backend", LastName = "User", Email = "backend@test.local", PasswordHash = "x", Expertises = UserExpertise.Backend },
            new User { Id = 3, FirstName = "Frontend", LastName = "User", Email = "frontend@test.local", PasswordHash = "x", Expertises = UserExpertise.Frontend },
            new User { Id = 4, FirstName = "Admin", LastName = "User", Email = "admin@test.local", PasswordHash = "x", Role = UserRole.Admin });
        db.Projects.Add(new Project { Id = 1, Name = "Integration", CreatedDate = DateTime.UtcNow });
        db.Tasks.AddRange(
            new TaskItem { Id = 1, Title = "Alpha backend", Description = "service", Status = TaskItemStatus.Pending, Priority = TaskPriority.Medium, ProjectId = 1, CreatedByUserId = 1, CreatedDate = DateTime.UtcNow.AddDays(-3) },
            new TaskItem { Id = 2, Title = "Beta frontend", Description = "page", Status = TaskItemStatus.InProgress, Priority = TaskPriority.High, ProjectId = 1, CreatedByUserId = 1, CreatedDate = DateTime.UtcNow.AddDays(-2) },
            new TaskItem { Id = 3, Title = "Gamma API", Description = "PostgreSQL search", Status = TaskItemStatus.Completed, Priority = TaskPriority.Low, ProjectId = 1, CreatedByUserId = 1, CreatedDate = DateTime.UtcNow.AddDays(-1) },
            new TaskItem { Id = 4, Title = "Deleted task", Status = TaskItemStatus.Pending, Priority = TaskPriority.Low, ProjectId = 1, CreatedByUserId = 1, CreatedDate = DateTime.UtcNow.AddDays(-4), IsDeleted = true, DeletedDate = DateTime.UtcNow, DeleterId = 4 });
        db.Tags.Add(new Tag { Id = 1, Name = "backend-integration", RequiredExpertise = UserExpertise.Backend, CreatedDate = DateTime.UtcNow });
        await db.SaveChangesAsync();
        db.TaskTags.Add(new TaskTag { Id = 1, TaskItemId = 1, TagId = 1 });
        db.TaskAssignments.AddRange(
            new TaskAssignment { TaskItemId = 1, AssignedUserId = 1 },
            new TaskAssignment { TaskItemId = 2, AssignedUserId = 1 });
        await db.SaveChangesAsync();
    }
}
