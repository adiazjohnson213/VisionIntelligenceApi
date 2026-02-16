using Microsoft.AspNetCore.Mvc;
using VisionIntelligenceAPI.Models.Dtos.Requests;
using VisionIntelligenceAPI.Models.Dtos.Responses;
using VisionIntelligenceAPI.Observability;

namespace VisionIntelligenceAPI.Controllers
{
    [ApiController]
    [Route("api/v1/vision")]
    public class VisionAnalysisController : Controller
    {
        [HttpPost("image-analysis:analyze-url")]
        public ActionResult<VisionAnalyzeResponseDto> AnalyzeUrl([FromBody] AnalyzeUrlRequestDto request)
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

        [HttpPost("image-analysis:analyze-file")]
        [Consumes("multipart/form-data")]
        public ActionResult<VisionAnalyzeResponseDto> AnalyzeFile(
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
