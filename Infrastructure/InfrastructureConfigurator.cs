using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace Infrastructure;

public static class InfrastructureLayerConfigurator
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repository and Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionTypeRepository, PermissionTypeRepository>();

        // Configuration settings
        services.Configure<ElasticSearchSettings>(configuration.GetSection(ElasticSearchSettings.SectionName));
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));

        // ElasticSearch
        services.AddSingleton<IElasticClient>(provider =>
        {
            var settings = configuration.GetSection(ElasticSearchSettings.SectionName).Get<ElasticSearchSettings>() ??
                throw new InvalidOperationException($"Missing or invalid configuration section: {ElasticSearchSettings.SectionName}");

            var connectionSettings = new ConnectionSettings(new Uri(settings.Uri))
                .DefaultIndex(settings.IndexName)
                .EnableApiVersioningHeader(false)
                ;

            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                connectionSettings.BasicAuthentication(settings.Username, settings.Password);
            }

            return new ElasticClient(connectionSettings);
        });

        services.AddScoped<IElasticSearchService, ElasticSearchService>();

        // Kafka
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        return services;
    }
}