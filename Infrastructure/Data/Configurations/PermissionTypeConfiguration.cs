using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PermissionTypeConfiguration : IEntityTypeConfiguration<PermissionType>
{
    public void Configure(EntityTypeBuilder<PermissionType> builder)
    {
        builder.ToTable("PermissionTypes");
        builder.HasKey(pt => pt.Id);

        // Properties
        builder.Property(pt => pt.Id)
            .ValueGeneratedOnAdd();

        builder.Property(pt => pt.Description)
               .IsRequired()
               .HasMaxLength(100);

        // Index for performance
        builder.HasIndex(pt => pt.Description);
    }
}
