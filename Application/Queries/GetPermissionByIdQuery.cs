using Application.Dtos;
using MediatR;

namespace Application.Queries;

public class GetPermissionByIdQuery(int id) : IRequest<PermissionDto>
{
    public int Id { get; set; } = id;
}