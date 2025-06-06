using Application.Interfaces;
using AutoMapper;
using Common.Constants;
using Common.Dtos;
using Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public class ModifyPermissionCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IElasticSearchService elasticSearchService,
    IKafkaProducer kafkaProducer,
    ILogger<ModifyPermissionCommandHandler> logger) : IRequestHandler<ModifyPermissionCommand>
{
    public async Task Handle(ModifyPermissionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing ModifyPermission for ID: {PermissionId}", request.Id);

        try
        {
            var existingPermission = await unitOfWork.PermissionRepository.GetById(request.Id);

            if (existingPermission == null)
            {
                logger.LogWarning("Permission not found with ID: {PermissionId}", request.Id);
                throw new ValidationException($"Permission with ID {request.Id} not found");
            }

            existingPermission.EmployeeForename = request.EmployeeForename;
            existingPermission.EmployeeSurname = request.EmployeeSurname;
            existingPermission.PermissionTypeId = request.PermissionTypeId;
            existingPermission.PermissionDate = request.PermissionDate;

            await unitOfWork.PermissionRepository.Update(existingPermission);
            await unitOfWork.SaveAsync();

            logger.LogInformation("Permission updated in database: {PermissionId}", request.Id);

            var updatedPermissionWithType = await unitOfWork.PermissionRepository.GetById(
                request.Id,
                tracking: false,
                "PermissionType");

            if (updatedPermissionWithType != null)
            {
                var permissionDto = mapper.Map<Application.Dtos.PermissionDto>(updatedPermissionWithType);
                await elasticSearchService.UpdatePermissionAsync(permissionDto);

                logger.LogInformation("Permission updated in ElasticSearch: {PermissionId}", request.Id);

                var kafkaMessage = new KafkaMessageDto
                {
                    Id = Guid.NewGuid(),
                    NameOperation = OperationNames.Modify
                };

                var kafkaResult = await kafkaProducer.ProduceAsync(kafkaMessage);

                if (kafkaResult)
                {
                    logger.LogInformation("Kafka message sent successfully for modified permission: {PermissionId}", request.Id);
                }
                else
                {
                    logger.LogWarning("Failed to send Kafka message for modified permission: {PermissionId}", request.Id);
                }
            }
            else
            {
                logger.LogWarning("Could not retrieve updated permission with PermissionType for ElasticSearch update");
            }
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ModifyPermission for ID: {PermissionId}", request.Id);
            throw;
        }
    }
}