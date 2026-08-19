using AutoMapper;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.DTOs.Users;
using TaskManager.Domain.Entities;
using TaskManager.Application.DTOs.Tags;

namespace TaskManager.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserResponse>();
        CreateMap<User, EligibleUserResponse>();
        CreateMap<CreateUserRequest, User>(MemberList.Source);
        CreateMap<UpdateUserRequest, User>(MemberList.Source);

        CreateMap<TaskItem, TaskResponse>();
        CreateMap<CreateTaskRequest, TaskItem>(MemberList.Source)
            .ForMember(
                task => task.CreatedByUserId,
                options => options.Ignore());

        CreateMap<UpdateTaskRequest, TaskItem>(MemberList.Source)
            .ForMember(
                task => task.CreatedByUserId,
                options => options.Ignore());

        CreateMap<CreateProjectRequest, Project>(MemberList.Source);
        CreateMap<UpdateProjectRequest, Project>(MemberList.Source);
        CreateMap<Project, ProjectResponse>();

        CreateMap<TagRequest, Tag>(MemberList.Source);
        CreateMap<Tag, TagResponse>();
    }
}
