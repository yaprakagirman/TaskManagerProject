namespace TaskManager.Application.Common;

public static class ValidationMessages
{
    public static class User
    {
        public const string FirstNameRequired = "First name is required.";
        public const string FirstNameMaxLength = "First name cannot exceed 100 characters.";

        public const string LastNameRequired = "Last name is required.";
        public const string LastNameMaxLength = "Last name cannot exceed 100 characters.";

        public const string EmailRequired = "Email is required.";
        public const string EmailInvalid = "Email format is not valid.";
        public const string EmailMaxLength = "Email cannot exceed 200 characters.";

        public const string UserIdGreaterThanZero =
            "User ID must be greater than zero.";

        public const string ExpertisesRequired =
            "Expertises list cannot be null.";

        public const string ExpertisesUnique =
            "Expertise values must be unique.";

        public const string NoneExpertiseCannotBeCombined =
            "None cannot be combined with another expertise.";

        public const string ExpertiseInvalid =
            "Each expertise must be Backend, Frontend, QA, or DevOps.";
    }

    public static class Auth
    {
        public const string PasswordRequired = "Password is required.";
        public const string PasswordMinLength = "Password must be at least 6 characters.";
    }

    public static class Task
    {
        public const string TitleRequired = "Title is required.";
        public const string TitleMaxLength = "Title cannot exceed 200 characters.";

        public const string DescriptionMaxLength = "Description cannot exceed 1000 characters.";

        public const string AssignedUserIdGreaterThanZero = "AssignedUserId must be greater than 0.";
        public const string ProjectIdGreaterThanZero = "ProjectId must be greater than 0.";
        public const string ParentTaskIdGreaterThanZero = "ParentTaskId must be greater than 0.";

        public const string DueDateCannotBePast = "Due date cannot be in the past.";
        public const string TaskStatusInvalid = "Task status is not valid.";
        public const string PriorityInvalid = "Task priority is not valid.";
    }

    public static class Project
    {
        public const string NameRequired =
            "Project name is required.";

        public const string NameMaxLength =
            "Project name cannot exceed 200 characters.";

        public const string DescriptionMaxLength =
            "Project description cannot exceed 1000 characters.";
    }
    public static class Tag
    {
        public const string NameRequired =
            "Tag name is required.";

        public const string NameMaxLength =
            "Tag name cannot exceed 100 characters.";

        public const string TaskIdsRequired =
            "At least one task ID is required.";

        public const string TaskIdsUnique =
            "Task IDs must be unique.";

        public const string TaskIdGreaterThanZero =
            "Task IDs must be greater than zero.";

        public const string TagIdGreaterThanZero =
            "Tag ID must be greater than zero.";

        public const string RequiredExpertiseInvalid =
            "Required expertise must be None, Backend, Frontend, QA, DevOps, or null.";
    }
}
