using Application.Dtos;
using Application.Interfaces;
using AutoMapper;
using Common.Constants;
using Common.Dtos;
using Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public class GetPermissionByIdQueryHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IKafkaProducer kafkaProducer,
    ILogger<GetPermissionByIdQueryHandler> logger) : IRequestHandler<GetPermissionByIdQuery, PermissionDto>
{
    public async Task<PermissionDto> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing GetPermissionById query for ID: {PermissionId}", request.Id);

        try
        {
            var permission = await unitOfWork.PermissionRepository.GetById(
                request.Id,
                tracking: false,
                "PermissionType");

            if (permission == null)
            {
                logger.LogWarning("Permission not found with ID: {PermissionId}", request.Id);
                throw new ValidationException($"Permission with ID {request.Id} not found");
            }

            var permissionDto = mapper.Map<PermissionDto>(permission);

            logger.LogInformation("Retrieved permission from database: {PermissionId}", request.Id);

            var kafkaMessage = new KafkaMessageDto
            {
                Id = Guid.NewGuid(),
                NameOperation = OperationNames.Get
            };

            var kafkaResult = await kafkaProducer.ProduceAsync(kafkaMessage);

            if (kafkaResult)
            {
                logger.LogInformation("Kafka message sent successfully for GetPermissionById operation: {PermissionId}", request.Id);
            }
            else
            {
                logger.LogWarning("Failed to send Kafka message for GetPermissionById operation: {PermissionId}", request.Id);
            }

            return permissionDto;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GetPermissionById query for ID: {PermissionId}", request.Id);
            throw;
        }
    }
}