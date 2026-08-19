namespace TaskManager.Domain.Enums;

[Flags]
public enum UserExpertise
{
    None = 0,
    Backend = 1,
    Frontend = 2,
    QA = 4,
    DevOps = 8
}
