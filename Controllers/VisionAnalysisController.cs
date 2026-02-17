using Microsoft.AspNetCore.Mvc;
using VisionIntelligenceAPI.Models.Dtos.Errors;
using VisionIntelligenceAPI.Models.Dtos.Requests;
using VisionIntelligenceAPI.Models.Dtos.Responses;
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
        public async Task<IActionResult> AnalyzeUrl([FromBody] AnalyzeUrlRequestDto request, [FromServices] VisionAnalysisService service, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                                ?? HttpContext.TraceIdentifier;

            var engine = request.Engine ?? EngineMode.Sdk;
            if (engine != EngineMode.Sdk)
                return BadRequest(new ApiErrorDto("InvalidRequest", "This step supports only Engine=Sdk.", correlationId));

            var response = await service.AnalyzeUrlWithSdkAsync(correlationId, request, cancellationToken);
            return Ok(response);
        }

        [HttpPost("image-analysis:analyze-file")]
        [Consumes("multipart/form-data")]
        public IActionResult AnalyzeFile(
        [FromForm] IFormFile file,
        [FromForm] string requirements,
        [FromForm] string? engine
    )
        {
            var correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                                ?? HttpContext.TraceIdentifier;

            return Ok(new VisionAnalyzeResponseDto(
                CorrelationId: correlationId,
                Caption: null,
                Read: null,
                Objects: null
            ));
        }
    }
}
