namespace VisionIntelligenceAPI.Models.Dtos.Responses
{
    public sealed record VisionAnalyzeResponseDto(
        string CorrelationId,
        CaptionDto? Caption,
        IReadOnlyList<ReadLineDto>? Read,
        IReadOnlyList<ObjectDto>? Objects
    );
}
