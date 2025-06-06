using Api.Extensions;
using Application;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add services
        builder.Services.AddApiServices(builder.Configuration);
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);

        var app = builder.Build();

        // Initialize database and services
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();
                Log.Information("Database initialized successfully");

                var elasticService = services.GetRequiredService<IElasticSearchService>();
                await elasticService.EnsureIndexExistsAsync();
                Log.Information("ElasticSearch initialized successfully");

                var kafkaService = services.GetRequiredService<IKafkaProducer>();
                await kafkaService.EnsureTopicExistsAsync();
                Log.Information("KafkaProducer initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        app.UseApiConfiguration();

        app.MapApiEndpoints(app.Environment);

        try
        {
            Log.Information("Starting web application");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}