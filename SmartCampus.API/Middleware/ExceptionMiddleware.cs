using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartCampus.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
                _logger.LogError(ex, "Something went wrong: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            // Determine status code based on exception type
            int statusCode = (int)HttpStatusCode.InternalServerError;
            if (exception.Message.Contains("already exists") || exception.Message.Contains("duplicate"))
            {
                statusCode = (int)HttpStatusCode.BadRequest;
            }
            else if (exception.Message.Contains("not found") || exception.Message.Contains("does not exist"))
            {
                statusCode = (int)HttpStatusCode.NotFound;
            }
            
            context.Response.StatusCode = statusCode;

            // Get inner exception message if available (for Entity Framework errors)
            string detailedMessage = exception.Message;
            if (exception.InnerException != null)
            {
                detailedMessage += $" | Inner: {exception.InnerException.Message}";
            }

            var response = new
            {
                StatusCode = statusCode,
                Message = statusCode == (int)HttpStatusCode.BadRequest 
                    ? "Bad Request" 
                    : "Internal Server Error from the custom middleware.",
                Detailed = detailedMessage // In production, hide this!
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
