using Application.Commands;
using Application.Dtos;
using Bogus;
using Domain.Entities;

namespace Tests.Helpers;

public static class TestDataBuilders
{
    public static Faker<Permission> PermissionFaker =>
        new Faker<Permission>()
            .RuleFor(p => p.Id, f => f.Random.Int(1, 1000))
            .RuleFor(p => p.EmployeeForename, f => f.Name.FirstName())
            .RuleFor(p => p.EmployeeSurname, f => f.Name.LastName())
            .RuleFor(p => p.PermissionTypeId, f => f.Random.Int(1, 4))
            .RuleFor(p => p.PermissionDate, f => f.Date.Future().Date)
            .RuleFor(p => p.PermissionType, f => PermissionTypeFaker.Generate());

    public static Faker<PermissionType> PermissionTypeFaker =>
        new Faker<PermissionType>()
            .RuleFor(pt => pt.Id, f => f.Random.Int(1, 4))
            .RuleFor(pt => pt.Description, f => f.PickRandom(
                "Type 1",
                "Type 2",
                "Type 3",
                "Type 4"));

    public static Faker<PermissionDto> PermissionDtoFaker =>
        new Faker<PermissionDto>()
            .RuleFor(p => p.Id, f => f.Random.Int(1, 1000))
            .RuleFor(p => p.EmployeeForename, f => f.Name.FirstName())
            .RuleFor(p => p.EmployeeSurname, f => f.Name.LastName())
            .RuleFor(p => p.PermissionTypeId, f => f.Random.Int(1, 4))
            .RuleFor(p => p.PermissionDate, f => f.Date.Future().Date)
            .RuleFor(p => p.PermissionType, f => PermissionTypeDtoFaker.Generate());

    public static Faker<PermissionTypeDto> PermissionTypeDtoFaker =>
        new Faker<PermissionTypeDto>()
            .RuleFor(pt => pt.Id, f => f.Random.Int(1, 4))
            .RuleFor(pt => pt.Description, f => f.PickRandom(
                "Type 1",
                "Type 2",
                "Type 3",
                "Type 4"));

    public static Faker<RequestPermissionCommand> RequestPermissionCommandFaker =>
        new Faker<RequestPermissionCommand>()
            .RuleFor(c => c.EmployeeForename, f => f.Name.FirstName())
            .RuleFor(c => c.EmployeeSurname, f => f.Name.LastName())
            .RuleFor(c => c.PermissionTypeId, f => f.Random.Int(1, 4))
            .RuleFor(c => c.PermissionDate, f => f.Date.Future().Date);

    public static Faker<ModifyPermissionCommand> ModifyPermissionCommandFaker =>
        new Faker<ModifyPermissionCommand>()
            .RuleFor(c => c.Id, f => f.Random.Int(1, 1000))
            .RuleFor(c => c.EmployeeForename, f => f.Name.FirstName())
            .RuleFor(c => c.EmployeeSurname, f => f.Name.LastName())
            .RuleFor(c => c.PermissionTypeId, f => f.Random.Int(1, 4))
            .RuleFor(c => c.PermissionDate, f => f.Date.Future().Date);

    public static RequestPermissionCommand CreateValidRequestCommand() =>
        RequestPermissionCommandFaker.Generate();

    public static ModifyPermissionCommand CreateValidModifyCommand() =>
        ModifyPermissionCommandFaker.Generate();

    public static Permission CreatePermissionWithType() =>
        PermissionFaker.Generate();

    public static IEnumerable<Permission> CreateMultiplePermissions(int count = 5) =>
        PermissionFaker.Generate(count);
}