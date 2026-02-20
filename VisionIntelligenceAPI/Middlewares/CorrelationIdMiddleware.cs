using System.Diagnostics;

namespace VisionIntelligenceAPI.Observability
{
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            // 1) Use incoming correlation id if present; otherwise use TraceIdentifier
            var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
            var correlationId = string.IsNullOrWhiteSpace(incoming)
                ? context.TraceIdentifier
                : incoming.Trim();

            // 2) Ensure it is accessible downstream
            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            // Optional: tag Activity for distributed tracing
            Activity.Current?.SetTag("correlationId", correlationId);

            await _next(context);
        }
    }
}
