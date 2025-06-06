using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Infrastructure.Services;

public class ElasticSearchService(
    IElasticClient elasticClient,
    IOptions<ElasticSearchSettings> settings,
    ILogger<ElasticSearchService> logger) : IElasticSearchService
{
    private readonly ElasticSearchSettings _settings = settings.Value;

    public async Task IndexPermissionAsync(PermissionDto permission)
    {
        try
        {
            var response = await elasticClient.IndexDocumentAsync(permission);

            if (!response.IsValid)
            {
                logger.LogError("Failed to index permission {PermissionId}: {Error}",
                    permission.Id, response.OriginalException?.Message);
            }
            else
            {
                logger.LogInformation("Successfully indexed permission {PermissionId}", permission.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error indexing permission {PermissionId}", permission.Id);
        }
    }

    public async Task UpdatePermissionAsync(PermissionDto permission)
    {
        try
        {
            var response = await elasticClient.UpdateAsync<PermissionDto>(permission.Id,
                u => u.Doc(permission).Index(_settings.IndexName));

            if (!response.IsValid)
            {
                logger.LogError("Failed to update permission {PermissionId}: {Error}",
                    permission.Id, response.OriginalException?.Message);
            }
            else
            {
                logger.LogInformation("Successfully updated permission {PermissionId}", permission.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating permission {PermissionId}", permission.Id);
        }
    }

    public async Task<PermissionDto?> GetPermissionAsync(int id)
    {
        try
        {
            var response = await elasticClient.GetAsync<PermissionDto>(id, g => g.Index(_settings.IndexName));

            if (response.IsValid && response.Found)
            {
                return response.Source;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting permission {PermissionId} from Elasticsearch", id);
            return null;
        }
    }

    public async Task<IEnumerable<PermissionDto>> SearchPermissionsAsync(string searchTerm)
    {
        try
        {
            var response = await elasticClient.SearchAsync<PermissionDto>(s => s
                .Index(_settings.IndexName)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(p => p.EmployeeForename)
                            .Field(p => p.EmployeeSurname)
                            .Field(p => p.PermissionType.Description))
                        .Query(searchTerm))));

            if (response.IsValid)
            {
                return response.Documents;
            }

            logger.LogError("Failed to search permissions: {Error}", response.OriginalException?.Message);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching permissions with term: {SearchTerm}", searchTerm);
            return [];
        }
    }

    public async Task EnsureIndexExistsAsync()
    {
        var indexExists = await elasticClient.Indices.ExistsAsync(_settings.IndexName);

        if (!indexExists.Exists)
        {
            var createIndexResponse = await elasticClient.Indices.CreateAsync(_settings.IndexName, c => c
                .Map<PermissionDto>(m => m
                    .Properties(p => p
                        .Number(n => n.Name(f => f.Id))
                        .Text(t => t.Name(f => f.EmployeeForename).Analyzer("standard"))
                        .Text(t => t.Name(f => f.EmployeeSurname).Analyzer("standard"))
                        .Number(n => n.Name(f => f.PermissionTypeId))
                        .Date(d => d.Name(f => f.PermissionDate))
                        .Object<PermissionTypeDto>(o => o
                            .Name(f => f.PermissionType)
                            .Properties(pp => pp
                                .Number(n => n.Name(f => f.Id))
                                .Text(t => t.Name(f => f.Description).Analyzer("standard"))
                            )
                        )
                    )
                )
            );

            if (!createIndexResponse.IsValid)
            {
                logger.LogError("Failed to create ElasticSearch index: {Error}",
                    createIndexResponse.OriginalException?.Message);
            }
            else
            {
                logger.LogInformation("ElasticSearch index created successfully");
            }
        }
    }
}