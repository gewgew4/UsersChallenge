using Application.Interfaces;
using AutoMapper;
using Common.Constants;
using Common.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public class RequestPermissionCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IElasticSearchService elasticSearchService,
    IKafkaProducer kafkaProducer,
    ILogger<RequestPermissionCommandHandler> logger) : IRequestHandler<RequestPermissionCommand, int>
{
    public async Task<int> Handle(RequestPermissionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing RequestPermission for employee: {Forename} {Surname}",
            request.EmployeeForename, request.EmployeeSurname);

        try
        {
            var permission = mapper.Map<Permission>(request);

            var createdPermission = await unitOfWork.PermissionRepository.Add(permission);
            await unitOfWork.SaveAsync();

            logger.LogInformation("Permission created in database with ID: {PermissionId}", createdPermission.Id);

            var permissionWithType = await unitOfWork.PermissionRepository.GetById(
                createdPermission.Id,
                tracking: false,
                "PermissionType");

            if (permissionWithType != null)
            {
                var permissionDto = mapper.Map<Application.Dtos.PermissionDto>(permissionWithType);
                await elasticSearchService.IndexPermissionAsync(permissionDto);

                logger.LogInformation("Permission indexed in ElasticSearch: {PermissionId}", createdPermission.Id);

                var kafkaMessage = new KafkaMessageDto
                {
                    Id = Guid.NewGuid(),
                    NameOperation = OperationNames.Request
                };

                var kafkaResult = await kafkaProducer.ProduceAsync(kafkaMessage);

                if (kafkaResult)
                {
                    logger.LogInformation("Kafka message sent successfully for permission: {PermissionId}", createdPermission.Id);
                }
                else
                {
                    logger.LogWarning("Failed to send Kafka message for permission: {PermissionId}", createdPermission.Id);
                }
            }
            else
            {
                logger.LogWarning("Could not retrieve created permission with PermissionType for ElasticSearch indexing");
            }

            return createdPermission.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing RequestPermission for employee: {Forename} {Surname}",
                request.EmployeeForename, request.EmployeeSurname);
            throw;
        }
    }
}