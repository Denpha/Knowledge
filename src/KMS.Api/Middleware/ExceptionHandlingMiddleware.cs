using System.Net;
using System.Text.Json;

namespace KMS.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";

        object response;
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Handle specific exception types
        switch (exception)
        {
            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = exception.Message,
                    Details = _env.IsDevelopment() ? "Resource not found." : null,
                    StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
                };
                break;

            case ArgumentException:
            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = exception.Message,
                    Details = _env.IsDevelopment() ? "Invalid request." : null,
                    StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
                };
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = "Unauthorized access.",
                    Details = _env.IsDevelopment() ? exception.Message : null,
                    StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
                };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "An error occurred while processing your request.",
                    Details = _env.IsDevelopment() ? exception.Message : null,
                    StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
                };
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}