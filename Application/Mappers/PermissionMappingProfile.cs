using Application.Commands;
using Application.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers;

public class PermissionMappingProfile : Profile
{
    public PermissionMappingProfile()
    {
        CreateMap<Permission, PermissionDto>();
        CreateMap<PermissionType, PermissionTypeDto>();

        CreateMap<RequestPermissionCommand, Permission>();
        CreateMap<ModifyPermissionCommand, Permission>();
    }
}