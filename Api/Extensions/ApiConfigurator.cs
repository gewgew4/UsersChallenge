using Api.Middleware;
using Infrastructure.Data;
using Scalar.AspNetCore;

namespace Api.Extensions;

public static class ApiLayerConfigurator
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>()
            .AddElasticsearch(configuration.GetSection("ElasticSearch:Uri").Value ?? "http://localhost:9200")
            .AddKafka(options =>
            {
                options.BootstrapServers = configuration.GetSection("Kafka:BootstrapServers").Value ?? "localhost:9092";
            })
            .AddSqlServer(configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseApiConfiguration(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<LoggingMiddleware>();

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseAuthorization();

        app.UseHealthChecks("/health");

        return app;
    }

    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            endpoints.MapOpenApi();
            endpoints.MapScalarApiReference();
        }

        endpoints.MapControllers();

        return endpoints;
    }
}