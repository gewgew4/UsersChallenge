using MediatR;

namespace Application.Commands;

public class ModifyPermissionCommand : IRequest
{
    public int Id { get; set; }
    public string EmployeeForename { get; set; } = string.Empty;
    public string EmployeeSurname { get; set; } = string.Empty;
    public int PermissionTypeId { get; set; }
    public DateTime PermissionDate { get; set; }
}
