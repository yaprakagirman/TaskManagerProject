using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users");

        entity.HasKey(user => user.Id);

        entity.Property(user => user.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(user => user.LastName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        entity.Property(user => user.Role)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(UserRole.User);

        entity.Property(user => user.Expertises)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(UserExpertise.None);

        entity.HasIndex(user => user.Email)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        entity.HasQueryFilter(user => !user.IsDeleted);

        entity.Property(user => user.CreatedDate)
            .IsRequired();

        entity.HasMany(user => user.CreatedTasks)
            .WithOne(task => task.CreatedByUser)
            .HasForeignKey(task => task.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
