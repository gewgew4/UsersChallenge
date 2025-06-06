using FluentValidation;

namespace Application.Commands;

public class RequestPermissionCommandValidator : AbstractValidator<RequestPermissionCommand>
{
    public RequestPermissionCommandValidator()
    {
        RuleFor(x => x.EmployeeForename)
            .NotEmpty().WithMessage("Employee forename is required")
            .MaximumLength(100).WithMessage("Employee forename cannot exceed 100 characters");

        RuleFor(x => x.EmployeeSurname)
            .NotEmpty().WithMessage("Employee surname is required")
            .MaximumLength(100).WithMessage("Employee surname cannot exceed 100 characters");

        RuleFor(x => x.PermissionTypeId)
            .GreaterThan(0).WithMessage("Permission type ID must be greater than 0");

        RuleFor(x => x.PermissionDate)
            .NotEmpty().WithMessage("Permission date is required")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Permission date cannot be in the past");
    }
}