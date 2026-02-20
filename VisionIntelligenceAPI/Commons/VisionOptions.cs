using System.ComponentModel.DataAnnotations;

namespace VisionIntelligenceAPI.Commons
{
    public sealed class VisionOptions
    {
        [Required]
        public string Endpoint { get; init; } = default!;
        [Required]
        public string ApiKey { get; init; } = default!;
    }
}
