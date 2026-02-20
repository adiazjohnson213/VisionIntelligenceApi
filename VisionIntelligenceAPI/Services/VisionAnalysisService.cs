using Azure.AI.Vision.ImageAnalysis;
using VisionIntelligenceAPI.Clients;
using VisionIntelligenceAPI.Mappers;
using VisionIntelligenceAPI.Models.Dtos.Requests;
using VisionIntelligenceAPI.Models.Dtos.Responses;
using VisionIntelligenceAPI.Models.Enums;
using VisionIntelligenceAPI.Resilience;

namespace VisionIntelligenceAPI.Services
{
    public class VisionAnalysisService(ImageAnalysisClient _client, VisionRestClient _rest, ConcurrencyLimiter _limiter)
    {
        private const long MaxFileSizeBytes = 20L * 1024 * 1024;
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        public virtual async Task<VisionAnalyzeResponseDto> AnalyzeUrlWithSdkAsync(
            string correlationId, AnalyzeUrlRequestDto request, CancellationToken cancellationToken)
        {
            var features = FeatureMapper.ToVisualFeatures(request.Requirements);

            var result = await _limiter.ExecuteAsync(
                async cancellationToken2 => await _client.AnalyzeAsync(new Uri(request.Url), features, new ImageAnalysisOptions(), cancellationToken2),
                cancellationToken);


            return new VisionAnalyzeResponseDto(
                            CorrelationId: correlationId,
                            Caption: VisionResultMapper.MapCaptionSdk(result.Value, request.Requirements),
                            Read: VisionResultMapper.MapReadSdk(result.Value, request.Requirements),
                            Objects: VisionResultMapper.MapObjectsSdk(result.Value, request.Requirements)
                        );
        }

        public async Task<VisionAnalyzeResponseDto> AnalyzeFileWithSdkAsync(
            string correlationId, IFormFile file, Requirement[] requirements, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("File is required.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException($"File too large. Max {MaxFileSizeBytes} bytes.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException($"Unsupported content-type: {file.ContentType}");

            var features = FeatureMapper.ToVisualFeatures(requirements);

            await using var stream = file.OpenReadStream();
            var imageData = await BinaryData.FromStreamAsync(stream, cancellationToken);

            var result = await _limiter.ExecuteAsync(
                async cancellationToken2 => await _client.AnalyzeAsync(imageData, features, new ImageAnalysisOptions(), cancellationToken2),
                cancellationToken);

            return new VisionAnalyzeResponseDto(
                            CorrelationId: correlationId,
                            Caption: VisionResultMapper.MapCaptionSdk(result.Value, requirements),
                            Read: VisionResultMapper.MapReadSdk(result.Value, requirements),
                            Objects: VisionResultMapper.MapObjectsSdk(result.Value, requirements)
                        );
        }

        public async Task<VisionAnalyzeResponseDto> AnalyzeUrlWithRestAsync(
        string correlationId,
        AnalyzeUrlRequestDto request,
        CancellationToken cancellationToken)
        {
            var featuresCsv = FeatureMapper.ToRestFeaturesCsv(request.Requirements);
            using var json = await _limiter.ExecuteAsync(
                async cancellationToken2 => await _rest.AnalyzeUrlAsync(featuresCsv, request.Url, cancellationToken2),
                cancellationToken);

            var root = json.RootElement;

            return new VisionAnalyzeResponseDto(
                CorrelationId: correlationId,
                Caption: VisionResultMapper.MapCaptionRest(root, request.Requirements),
                Read: VisionResultMapper.MapReadRest(root, request.Requirements),
                Objects: VisionResultMapper.MapObjectsRest(root, request.Requirements)
            );
        }
    }
}
