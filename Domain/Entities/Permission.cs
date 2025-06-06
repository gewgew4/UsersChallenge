namespace Domain.Entities;

public class Permission : BaseEntity<Permission, int>
{
    public string EmployeeForename { get; set; } = string.Empty;
    public string EmployeeSurname { get; set; } = string.Empty;
    public int PermissionTypeId { get; set; }
    public DateTime PermissionDate { get; set; }

    // Navigation props
    public PermissionType PermissionType { get; set; }
}