using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p=> p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.EmployeeForename)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.EmployeeSurname)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.PermissionTypeId)
               .IsRequired();

        builder.Property(p => p.PermissionDate)
               .IsRequired()
               .HasColumnType("date");

        // Relationships
        builder.HasOne(p => p.PermissionType)
               .WithMany(pt => pt.Permissions)
               .HasForeignKey(p => p.PermissionTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
