namespace VisionIntelligenceAPI.Models.Dtos.Responses
{
    public sealed record ReadLineDto(
        string Text,
        IReadOnlyList<PointDto> BoundingPolygon,
        IReadOnlyList<ReadWordDto> Words
    );
}
