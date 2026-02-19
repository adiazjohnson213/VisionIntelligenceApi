using Azure.AI.Vision.ImageAnalysis;
using VisionIntelligenceAPI.Clients;
using VisionIntelligenceAPI.Mappers;
using VisionIntelligenceAPI.Models.Dtos.Requests;
using VisionIntelligenceAPI.Models.Dtos.Responses;
using VisionIntelligenceAPI.Models.Enums;

namespace VisionIntelligenceAPI.Services
{
    public class VisionAnalysisService(ImageAnalysisClient _client, VisionRestClient _rest)
    {
        private const long MaxFileSizeBytes = 20L * 1024 * 1024; // 20 MB (límite común para Image Analysis 4.0)
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        public async Task<VisionAnalyzeResponseDto> AnalyzeUrlWithSdkAsync(
            string correlationId, AnalyzeUrlRequestDto request, CancellationToken cancellationToken)
        {
            var features = FeatureMapper.ToVisualFeatures(request.Requirements);

            var result = await _client.AnalyzeAsync(
                new Uri(request.Url), features, new ImageAnalysisOptions(), cancellationToken);

            var captionDto = (request.Requirements.Contains(Requirement.Caption) && result.Value.Caption is not null)
                                    ? new CaptionDto(result.Value.Caption.Text, result.Value.Caption.Confidence)
                                    : null;

            var readDto = request.Requirements.Contains(Requirement.Read)
                            ? new List<ReadLineDto>()
                            : null;

            var objectsDto = request.Requirements.Contains(Requirement.Objects)
                                ? new List<ObjectDto>()
                                : null;

            return new VisionAnalyzeResponseDto(
                            CorrelationId: correlationId,
                            Caption: captionDto,
                            Read: readDto,
                            Objects: objectsDto
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

            var result = await _client.AnalyzeAsync(imageData, features, new ImageAnalysisOptions(), cancellationToken);

            var captionDto = (requirements.Contains(Requirement.Caption) && result.Value.Caption is not null)
                         ? new CaptionDto(result.Value.Caption.Text, result.Value.Caption.Confidence)
                         : null;

            var readDto = requirements.Contains(Requirement.Read)
                          ? new List<ReadLineDto>()
                          : null;
            var objectsDto = requirements.Contains(Requirement.Objects)
                         ? new List<ObjectDto>()
                         : null;

            return new VisionAnalyzeResponseDto(
                            CorrelationId: correlationId,
                            Caption: captionDto,
                            Read: readDto,
                            Objects: objectsDto
                        );
        }

        public async Task<VisionAnalyzeResponseDto> AnalyzeUrlWithRestAsync(
        string correlationId,
        AnalyzeUrlRequestDto request,
        CancellationToken ct)
        {
            var featuresCsv = FeatureMapper.ToRestFeaturesCsv(request.Requirements);
            using var json = await _rest.AnalyzeUrlAsync(featuresCsv, request.Url, ct);

            // Parsing defensivo: cada bloque puede NO venir si no lo pediste
            CaptionDto? caption = null;
            IReadOnlyList<ReadLineDto>? read = null;
            IReadOnlyList<ObjectDto>? objects = null;

            var root = json.RootElement;

            // Caption
            if (request.Requirements.Contains(Requirement.Caption))
            {
                // si el feature fue solicitado pero falta el bloque, devolvemos null? NO: solicitado => debe existir,
                // PERO para robustez mantenemos: si falta => caption null (y tú lo detectas en tests)
                if (root.TryGetProperty("captionResult", out var captionResult)
                    && captionResult.TryGetProperty("text", out var textEl)
                    && captionResult.TryGetProperty("confidence", out var confEl))
                {
                    caption = new CaptionDto(textEl.GetString() ?? "", confEl.GetDouble());
                }
                else
                {
                    // solicitado pero no vino -> lo dejamos null (defensive)
                    caption = null;
                }
            }

            // Read (en este paso dejamos mapeo a [] si solicitado; el mapeo completo lo haces en Paso 6)
            if (request.Requirements.Contains(Requirement.Read))
            {
                read = new List<ReadLineDto>(); // placeholder correcto según contrato (solicitado => [] si sin hallazgos)
            }

            // Objects (igual que Read por ahora)
            if (request.Requirements.Contains(Requirement.Objects))
            {
                objects = new List<ObjectDto>();
            }

            return new VisionAnalyzeResponseDto(
                CorrelationId: correlationId,
                Caption: caption,
                Read: read,
                Objects: objects
            );
        }
    }
}
