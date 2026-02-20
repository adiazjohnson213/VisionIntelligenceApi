namespace VisionIntelligenceAPI.Models.Dtos.Responses
{
    public sealed record ObjectDto(
        string Name,
        double Confidence,
        BoundingBoxDto BoundingBox
    );
}
