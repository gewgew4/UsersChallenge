using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class PermissionTypeRepository(ApplicationDbContext context) : GenericRepository<PermissionType, int>(context), IPermissionTypeRepository;