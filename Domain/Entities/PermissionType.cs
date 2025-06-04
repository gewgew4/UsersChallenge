namespace Domain.Entities;

public class PermissionType
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    // Navigation props
    public ICollection<Permission> Permissions { get; set; } = [];
}
