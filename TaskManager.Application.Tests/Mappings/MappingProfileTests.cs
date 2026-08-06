using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManager.Application.Mappings;

namespace TaskManager.Application.Tests.Mappings;

public class MappingProfileTests
{
    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        var configuration = new MapperConfiguration(
            config => config.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
    }
}
