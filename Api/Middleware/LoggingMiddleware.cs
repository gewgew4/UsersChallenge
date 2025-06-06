namespace Api.Middleware;

public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Request: {Method} {Url}", context.Request.Method, context.Request.Path);

        await next(context);

        logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
    }
}