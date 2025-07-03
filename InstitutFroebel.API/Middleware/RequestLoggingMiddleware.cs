using System.Diagnostics;

namespace InstitutFroebel.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            // Log request
            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} started",
                requestId,
                context.Request.Method,
                context.Request.Path);

            // Add request ID to response headers
            context.Response.Headers.Add("X-Request-ID", requestId);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                _logger.LogInformation(
                    "[{RequestId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}