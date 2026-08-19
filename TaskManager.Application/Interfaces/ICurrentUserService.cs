namespace TaskManager.Application.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    int? UserId { get; }
}
