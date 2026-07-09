using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreshMart.Middleware
{
    /// <summary>
    /// Global exception handling middleware for consistent error responses
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Determine if the request is AJAX/API
            var isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                         context.Request.Path.Value?.StartsWith("/api") == true;

            if (isAjax)
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "An error occurred while processing your request",
                    timestamp = DateTime.UtcNow
                };

                switch (exception)
                {
                    case ArgumentNullException:
                    case ArgumentException:
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        break;
                    case UnauthorizedAccessException:
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        break;
                    case KeyNotFoundException:
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        break;
                    default:
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        break;
                }

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            else
            {
                // Normal Web Request -> friendly error page
                if (!context.Response.HasStarted)
                {
                    context.Response.Redirect("/Home/Error");
                }
            }
        }
    }
}
