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
            [FromServices] VisionAnalysisService service, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                                ?? HttpContext.TraceIdentifier;

            if (request.Requirements == null || request.Requirements.Length == 0)
                return BadRequest(new ApiErrorDto("InvalidRequest", "Requirements must contain at least one item.", correlationId));

            var engine = request.Engine ?? EngineMode.Sdk;

            var response = engine switch
            {
                EngineMode.Sdk => await service.AnalyzeUrlWithSdkAsync(correlationId, request, cancellationToken),
                EngineMode.Rest => await service.AnalyzeUrlWithRestAsync(correlationId, request, cancellationToken),
                _ => throw new InvalidOperationException("Unsupported engine")
            };

            return Ok(response);
        }

        [HttpPost("image-analysis:analyze-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AnalyzeFile([FromForm] IFormFile file, [FromForm] string requirements,
            [FromForm] string? engine, [FromServices] VisionAnalysisService service, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                                ?? HttpContext.TraceIdentifier;

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
                return BadRequest(new ApiErrorDto("InvalidRequest", $"Invalid requirements: {ex.Message}", correlationId));
            }

            if (reqs.Length == 0)
                return BadRequest(new ApiErrorDto("InvalidRequest", "Requirements must contain at least one item.", correlationId));

            try
            {
                var response = await service.AnalyzeFileWithSdkAsync(correlationId, file, reqs, cancellationToken);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiErrorDto("InvalidRequest", ex.Message, correlationId));
            }
        }
    }
}
