using Azure.AI.Vision.ImageAnalysis;
using VisionIntelligenceAPI.Models.Enums;

namespace VisionIntelligenceAPI.Mappers
{
    public static class FeatureMapper
    {
        private static readonly Dictionary<Requirement, string> RequirementToFeatureMap = new()
        {
            { Requirement.Caption, "caption" },
            { Requirement.Read, "read" },
            { Requirement.Objects, "objects" }
        };

        public static VisualFeatures ToVisualFeatures(Requirement[] requirements)
        {
            VisualFeatures visualFeatures = VisualFeatures.None;

            foreach (var requirement in requirements.Distinct())
            {

                visualFeatures |= requirement switch
                {
                    Requirement.Caption => VisualFeatures.Caption,
                    Requirement.Read => VisualFeatures.Read,
                    Requirement.Objects => VisualFeatures.Objects,
                    _ => VisualFeatures.None
                };
            }

            return visualFeatures;
        }

        public static string ToRestFeaturesCsv(Requirement[] requirements)
        {
            var features = requirements
                .Distinct()
                .Where(RequirementToFeatureMap.ContainsKey)
                .Select(r => RequirementToFeatureMap[r]);

            return string.Join(",", features.Where(t => !string.IsNullOrWhiteSpace(t)));
        }
    }
}
