using Application.Dtos;

namespace Application.Interfaces;

public interface IElasticSearchService
{
    Task IndexPermissionAsync(PermissionDto permission);
    Task<PermissionDto?> GetPermissionAsync(int id);
    Task<IEnumerable<PermissionDto>> SearchPermissionsAsync(string searchTerm);
    Task UpdatePermissionAsync(PermissionDto permission);
}
