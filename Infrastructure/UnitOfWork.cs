using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;

namespace Infrastructure;


public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private IPermissionRepository? _permissionRepository;
    private IPermissionTypeRepository? _permissionTypeRepository;

    public IPermissionRepository PermissionRepository =>
        _permissionRepository ??= new PermissionRepository(context);

    public IPermissionTypeRepository PermissionTypeRepository =>
        _permissionTypeRepository ??= new PermissionTypeRepository(context);

    public async Task<int> SaveAsync()
    {
        return await context.SaveChangesAsync();
    }

    public void Dispose() => context.Dispose();
}
