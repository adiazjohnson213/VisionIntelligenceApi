using VisionIntelligenceAPI.Models.Enums;

namespace VisionIntelligenceAPI.Models.Dtos.Requests
{
    public sealed record AnalyzeUrlRequestDto(
    string Url,
    Requirement[] Requirements,
    EngineMode? Engine = null
);
}
