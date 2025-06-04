using Application.Dtos;
using MediatR;

namespace Application.Queries;

public class GetPermissionsQuery : IRequest<IEnumerable<PermissionDto>>;
