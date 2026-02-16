using VisionIntelligenceAPI.Models.Enums;

namespace VisionIntelligenceAPI.Models.Dtos.Requests
{
    public sealed record AnalyzeFileRequestDto(
        Requirement[] Requirements,
        EngineMode? Engine = null
    );
}
