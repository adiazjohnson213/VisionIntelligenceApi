namespace VisionIntelligenceAPI.Models.Dtos.Responses
{
    public sealed record ReadWordDto(
        string Text,
        double Confidence,
        IReadOnlyList<PointDto> BoundingPolygon
    );
}
