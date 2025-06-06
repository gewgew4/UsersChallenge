using System.Text.Json;

namespace Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            // TODO: check later if needed - pending possible Result pattern implementation
            _ => new { message = "An error occurred while processing your request.", statusCode = 500 }
        };

        context.Response.StatusCode = response.statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}