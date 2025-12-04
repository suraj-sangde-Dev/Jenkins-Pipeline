using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using TeleICU.API.Services.Interface;
using static TeleICU.API.Helpers.AppException;

namespace TeleICU.API.Helpers
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogService _logService;

        public ExceptionMiddleware(RequestDelegate next, ILogService logService)
        {
            _next = next;
            _logService = logService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // 
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // log entry
            var logRequest = new LogRequest
            {
                logType = (int?)LogType.Error,
                endpoint = $"{context.Request.Method}: {context.Request.Path}",
                message = ex.Message,
                innerException = ex.InnerException?.ToString()
            };

            await _logService.Create(logRequest);

            // structured JSON error response
            var errorResponse = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = ex.Message,
                Details = null
            };

            var json = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(json);
        }
    }

}
