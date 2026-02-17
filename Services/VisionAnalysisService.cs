using Azure.AI.Vision.ImageAnalysis;
using VisionIntelligenceAPI.Models.Dtos.Requests;
using VisionIntelligenceAPI.Models.Dtos.Responses;
using VisionIntelligenceAPI.Models.Enums;

namespace VisionIntelligenceAPI.Services
{
    public class VisionAnalysisService(ImageAnalysisClient _client)
    {
        public async Task<VisionAnalyzeResponseDto> AnalyzeUrlWithSdkAsync(string correlationId, AnalyzeUrlRequestDto request, CancellationToken cancellationToken) 
        {
            var features = MapToVisualFeatures(request.Requirements);

            var result = await _client.AnalyzeAsync(new Uri(request.Url), features, new ImageAnalysisOptions(), cancellationToken);

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

        private VisualFeatures MapToVisualFeatures(Requirement[] requirements)
        {
            VisualFeatures visualFeatures = 0;

            foreach (var requirement in requirements)
            {
                visualFeatures |= requirement switch
                {
                    Requirement.Caption => VisualFeatures.Caption,
                    Requirement.Read => VisualFeatures.Read,
                    Requirement.Objects => VisualFeatures.Objects,
                    _ => 0
                };
                
            }

            return visualFeatures;
        }
    }
}
