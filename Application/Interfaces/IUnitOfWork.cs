namespace Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IPermissionRepository PermissionRepository { get; }
    IPermissionTypeRepository PermissionTypeRepository { get; }
    Task<int> SaveAsync();
}
