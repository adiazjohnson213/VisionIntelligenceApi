using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using VisionIntelligenceAPI.Models.Dtos.Errors;
using VisionIntelligenceAPI.Models.Dtos.Requests;
using VisionIntelligenceAPI.Models.Enums;
using VisionIntelligenceAPI.Observability;
using VisionIntelligenceAPI.Services;

namespace VisionIntelligenceAPI.Controllers
{
    [ApiController]
    [Route("api/v1/vision")]
    public class VisionAnalysisController : Controller
    {
        [HttpPost("image-analysis:analyze-url")]
        public async Task<IActionResult> AnalyzeUrl([FromBody] AnalyzeUrlRequestDto request,
            [FromServices] VisionAnalysisService service, [FromServices] ILogger<VisionAnalysisController> logger,
            CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                                ?? HttpContext.TraceIdentifier;

            if (request.Requirements == null || request.Requirements.Length == 0)
                return BadRequest(new ApiErrorDto("InvalidRequest", "Requirements must contain at least one item.", correlationId));

            var engine = request.Engine ?? EngineMode.Sdk;

            var sw = Stopwatch.StartNew();

            using var activity = VisionTelemetry.ActivitySource.StartActivity("VisionAnalyzeUrl", ActivityKind.Server);
            activity?.SetTag("correlationId", correlationId);
            activity?.SetTag("engine", engine.ToString());
            activity?.SetTag("requirements", string.Join(",", request.Requirements ?? Array.Empty<Requirement>()));


            try
            {
                VisionTelemetry.RequestsTotal.Add(1,
                    new KeyValuePair<string, object?>("engine", engine.ToString()));
                var response = engine switch
                {
                    EngineMode.Sdk => await service.AnalyzeUrlWithSdkAsync(correlationId, request, cancellationToken),
                    EngineMode.Rest => await service.AnalyzeUrlWithRestAsync(correlationId, request, cancellationToken),
                    _ => throw new InvalidOperationException("Unsupported engine")
                };


                sw.Stop();
                VisionTelemetry.RequestDurationMs.Record(sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("engine", engine.ToString()),
                    new KeyValuePair<string, object?>("status", 200));

                logger.LogInformation(
                    "AnalyzeUrl OK {CorrelationId} {Engine} {Requirements} {DurationMs}",
                    correlationId, engine, request.Requirements, sw.Elapsed.TotalMilliseconds);

                return Ok(response);

            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                sw.Stop();
                VisionTelemetry.Throttles429Total.Add(1,
                    new KeyValuePair<string, object?>("engine", engine.ToString()));

                VisionTelemetry.RequestDurationMs.Record(sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("engine", engine.ToString()),
                    new KeyValuePair<string, object?>("status", 429));

                logger.LogWarning(ex,
                    "AnalyzeUrl throttled {CorrelationId} {Engine} {DurationMs}",
                    correlationId, engine, sw.Elapsed.TotalMilliseconds);

                return BadRequest(new ApiErrorDto("InvalidRequest", ex.Message, correlationId));
            }
        }

        [HttpPost("image-analysis:analyze-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AnalyzeFile([FromForm] IFormFile file, [FromForm] string requirements,
            [FromForm] string? engine, [FromServices] VisionAnalysisService service, [FromServices] ILogger<VisionAnalysisController> logger,
            CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                                ?? HttpContext.TraceIdentifier;

            var sw = Stopwatch.StartNew();

            using var activity = VisionTelemetry.ActivitySource.StartActivity("VisionAnalyzeFile", ActivityKind.Server);
            activity?.SetTag("correlationId", correlationId);
            activity?.SetTag("engine", engine ?? "Sdk");
            activity?.SetTag("file.contentType", file?.ContentType);
            activity?.SetTag("file.size", file?.Length);

            if (!string.IsNullOrWhiteSpace(engine) && !engine.Equals("Sdk", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new ApiErrorDto("InvalidRequest", "This step supports only Engine=Sdk.", correlationId));

            if (string.IsNullOrWhiteSpace(requirements))
                return BadRequest(new ApiErrorDto("InvalidRequest", "Requirements must contain at least one item.", correlationId));


            Requirement[] reqs;
            try
            {
                var opts = new JsonSerializerOptions();
                opts.Converters.Add(new JsonStringEnumConverter());
                reqs = JsonSerializer.Deserialize<Requirement[]>(requirements, opts)
                       ?? Array.Empty<Requirement>();
            }
            catch (Exception ex)
            {
                sw.Stop();
                VisionTelemetry.RequestDurationMs.Record(sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("endpoint", "analyze-file"),
                    new KeyValuePair<string, object?>("status", 400));

                logger.LogWarning(ex,
                    "AnalyzeFile invalid requirements {CorrelationId} {Requirements} {DurationMs}",
                    correlationId, requirements, sw.Elapsed.TotalMilliseconds);

                return BadRequest(new ApiErrorDto("InvalidRequest", $"Invalid requirements: {ex.Message}", correlationId));

            }

            if (reqs.Length == 0)
                return BadRequest(new ApiErrorDto("InvalidRequest", "Requirements must contain at least one item.", correlationId));

            activity?.SetTag("requirements", string.Join(",", reqs.Select(r => r.ToString())));

            try
            {
                VisionTelemetry.RequestsTotal.Add(1,
                    new KeyValuePair<string, object?>("endpoint", "analyze-file"),
                    new KeyValuePair<string, object?>("engine", "Sdk"));

                var response = await service.AnalyzeFileWithSdkAsync(correlationId, file, reqs, cancellationToken);

                sw.Stop();
                VisionTelemetry.RequestDurationMs.Record(sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("endpoint", "analyze-file"),
                    new KeyValuePair<string, object?>("engine", "Sdk"),
                    new KeyValuePair<string, object?>("status", 200));

                logger.LogInformation(
                    "AnalyzeFile OK {CorrelationId} {Engine} {Requirements} {ContentType} {Size} {DurationMs}",
                    correlationId, "Sdk", reqs, file.ContentType, file.Length, sw.Elapsed.TotalMilliseconds);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                sw.Stop();
                VisionTelemetry.RequestDurationMs.Record(sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("endpoint", "analyze-file"),
                    new KeyValuePair<string, object?>("engine", "Sdk"),
                    new KeyValuePair<string, object?>("status", 400));

                logger.LogWarning(ex,
                    "AnalyzeFile bad request {CorrelationId} {DurationMs}",
                    correlationId, sw.Elapsed.TotalMilliseconds);

                return BadRequest(new ApiErrorDto("InvalidRequest", ex.Message, correlationId));
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                sw.Stop();
                VisionTelemetry.Throttles429Total.Add(1,
                    new KeyValuePair<string, object?>("endpoint", "analyze-file"),
                    new KeyValuePair<string, object?>("engine", "Sdk"));

                VisionTelemetry.RequestDurationMs.Record(sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("endpoint", "analyze-file"),
                    new KeyValuePair<string, object?>("engine", "Sdk"),
                    new KeyValuePair<string, object?>("status", 429));

                logger.LogWarning(ex,
                    "AnalyzeFile throttled {CorrelationId} {DurationMs}",
                    correlationId, sw.Elapsed.TotalMilliseconds);

                return BadRequest(new ApiErrorDto("InvalidRequest", ex.Message, correlationId));
            }
        }
    }
}
