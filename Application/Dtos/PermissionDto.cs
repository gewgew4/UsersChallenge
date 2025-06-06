namespace Application.Dtos;

public class PermissionDto
{
    public int Id { get; set; }
    public string EmployeeForename { get; set; } = string.Empty;
    public string EmployeeSurname { get; set; } = string.Empty;
    public int PermissionTypeId { get; set; }
    public DateTime PermissionDate { get; set; }
    public PermissionTypeDto PermissionType { get; set; } = new();
}