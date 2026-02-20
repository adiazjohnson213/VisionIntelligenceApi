using VisionIntelligenceAPI.Models.Dtos.Requests;
using VisionIntelligenceAPI.Models.Dtos.Responses;
using VisionIntelligenceAPI.Models.Enums;
using VisionIntelligenceAPI.Services;

namespace VisionIntelligenceAPI.Tests.TestDoubles
{
    public sealed class FakeVisionAnalysisService : VisionAnalysisService
    {

        public FakeVisionAnalysisService() : base(null!, null!, null!) { }

        public override Task<VisionAnalyzeResponseDto> AnalyzeUrlWithSdkAsync(
            string correlationId, AnalyzeUrlRequestDto request, CancellationToken ct)
        {
            var caption = request.Requirements.Contains(Requirement.Caption)
                ? new CaptionDto("fake caption", 0.99)
                : null;

            var read = request.Requirements.Contains(Requirement.Read)
                ? new List<ReadLineDto>()
                : null;

            var objects = request.Requirements.Contains(Requirement.Objects)
                ? new List<ObjectDto>()
                : null;

            return Task.FromResult(new VisionAnalyzeResponseDto(
                CorrelationId: correlationId,
                Caption: caption,
                Read: read,
                Objects: objects
            ));
        }
    }
}
