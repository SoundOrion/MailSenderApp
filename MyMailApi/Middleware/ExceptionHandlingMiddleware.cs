using System.Text.Json;
using FluentValidation;
using MyMailApi.Contracts;

namespace MyMailApi.Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "バリデーションエラー");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray());

            var response = new ErrorResponse
            {
                Error = "Validation failed.",
                ValidationErrors = errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "フォーマットエラー");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Error = $"Invalid format: {ex.Message}"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "業務エラー");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Error = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未処理例外");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Error = "Internal server error."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}