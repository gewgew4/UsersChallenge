namespace Domain.Entities;

public class PermissionType : BaseEntity<PermissionType, int>
{
    public string Description { get; set; } = string.Empty;

    // Navigation props
    public ICollection<Permission> Permissions { get; set; } = [];
}