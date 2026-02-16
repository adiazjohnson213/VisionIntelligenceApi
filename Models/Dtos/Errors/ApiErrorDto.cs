namespace VisionIntelligenceAPI.Models.Dtos.Errors
{
    public sealed record ApiErrorDto(
        string Code,
        string Message,
        string CorrelationId
    );
}
