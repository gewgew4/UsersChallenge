using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class PermissionRepository(ApplicationDbContext context) : GenericRepository<Permission, int>(context), IPermissionRepository;
