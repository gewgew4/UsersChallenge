using Application.Dtos;
using Application.Interfaces;
using AutoMapper;
using Common.Constants;
using Common.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public class GetPermissionsQueryHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IKafkaProducer kafkaProducer,
    ILogger<GetPermissionsQueryHandler> logger) : IRequestHandler<GetPermissionsQuery, IEnumerable<PermissionDto>>
{
    public async Task<IEnumerable<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing GetPermissions query");

        try
        {
            var permissions = unitOfWork.PermissionRepository.GetAll(
                tracking: false,
                "PermissionType");

            var permissionDtos = mapper.Map<IEnumerable<PermissionDto>>(permissions);

            logger.LogInformation("Retrieved {Count} permissions from database", permissionDtos.Count());

            var kafkaMessage = new KafkaMessageDto
            {
                Id = Guid.NewGuid(),
                NameOperation = OperationNames.Get
            };

            var kafkaResult = await kafkaProducer.ProduceAsync(kafkaMessage);

            if (kafkaResult)
            {
                logger.LogInformation("Kafka message sent successfully for GetPermissions operation");
            }
            else
            {
                logger.LogWarning("Failed to send Kafka message for GetPermissions operation");
            }

            return permissionDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GetPermissions query");
            throw;
        }
    }
}