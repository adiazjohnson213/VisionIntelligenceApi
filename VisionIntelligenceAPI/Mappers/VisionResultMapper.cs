using System.Text.Json;
using Azure.AI.Vision.ImageAnalysis;
using VisionIntelligenceAPI.Models.Dtos.Responses;
using VisionIntelligenceAPI.Models.Enums;

namespace VisionIntelligenceAPI.Mappers
{
    public static class VisionResultMapper
    {
        public static CaptionDto? MapCaptionSdk(ImageAnalysisResult result, IEnumerable<Requirement> requirements)
        {
            if (!requirements.Contains(Requirement.Caption))
            {
                return null;
            }
            var caption = result.Caption;
            if (caption is null)
            {
                return null;
            }
            return new CaptionDto(caption.Text, caption.Confidence);
        }

        public static IReadOnlyList<ReadLineDto>? MapReadSdk(ImageAnalysisResult result, IEnumerable<Requirement> requirements)
        {
            if (!requirements.Contains(Requirement.Read))
            {
                return null;
            }
            if (result.Read is null)
            {
                return null;
            }
            var lines = new List<ReadLineDto>();

            foreach (var block in result.Read.Blocks)
                foreach (var line in block.Lines)
                {
                    var words = line.Words.Select(w =>
                        new ReadWordDto(
                            w.Text,
                            w.Confidence,
                            w.BoundingPolygon.Select(p => new PointDto(p.X, p.Y)).ToList()
                        )
                    ).ToList();
                    lines.Add(new ReadLineDto(
                        line.Text,
                        line.BoundingPolygon.Select(p => new PointDto(p.X, p.Y)).ToList(),
                        words
                    ));
                }

            return lines;
        }

        public static IReadOnlyList<ObjectDto>? MapObjectsSdk(ImageAnalysisResult result, IEnumerable<Requirement> requirements)
        {
            if (!requirements.Contains(Requirement.Objects))
            {
                return null;
            }
            if (result.Objects is null)
            {
                return Array.Empty<ObjectDto>();
            }

            return result.Objects.Values.Select(o =>
            {
                var tag = o.Tags.FirstOrDefault();

                var box = o.BoundingBox;
                return new ObjectDto(
                    tag?.Name ?? "Unknown",
                    tag?.Confidence ?? 0,
                    new BoundingBoxDto(box.X, box.Y, box.Width, box.Height)
                );
            }).ToList();
        }

        public static CaptionDto? MapCaptionRest(JsonElement root, IEnumerable<Requirement> requirements)
        {
            if (!requirements.Contains(Requirement.Caption))
            {
                return null;
            }

            if (root.TryGetProperty("captionResult", out var caption)
                && caption.TryGetProperty("text", out var textEl)
                && caption.TryGetProperty("confidence", out var confEl))
            {
                return new CaptionDto(textEl.GetString() ?? string.Empty, confEl.GetSingle());
            }

            return null;
        }

        public static IReadOnlyList<ObjectDto>? MapObjectsRest(JsonElement root, IEnumerable<Requirement> requirements)
        {
            if (!requirements.Contains(Requirement.Objects))
            {
                return null;
            }

            if (!root.TryGetProperty("objectsResult", out var objectsResult))
            {
                return Array.Empty<ObjectDto>();
            }

            if (objectsResult.TryGetProperty("values", out var objectsValues)
                || objectsValues.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<ObjectDto>();
            }

            var objects = new List<ObjectDto>();
            foreach (var obj in objectsValues.EnumerateArray())
            {
                var tag = obj.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array
                    ? tags.EnumerateArray().FirstOrDefault()
                    : default;

                var name = (tag.ValueKind != JsonValueKind.Undefined && tag.TryGetProperty("name", out var nameEl))
                    ? nameEl.GetString() ?? "unknown"
                    : "unknown"; ;

                var confidence = (tag.ValueKind != JsonValueKind.Undefined && tag.TryGetProperty("confidence", out var confEl))
                    ? confEl.GetDouble()
                    : 0d;

                var box = ParseBoundingBox(obj);
                objects.Add(new ObjectDto(name, confidence, box));
            }

            return objects;
        }

        public static IReadOnlyList<ReadLineDto>? MapReadRest(JsonElement root, IEnumerable<Requirement> requirements)
        {
            if (!requirements.Contains(Requirement.Read)) return null;

            if (!root.TryGetProperty("readResult", out var readResult))
                return Array.Empty<ReadLineDto>();

            var lines = new List<ReadLineDto>();

            if (readResult.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
            {
                foreach (var b in blocks.EnumerateArray())
                {
                    if (!b.TryGetProperty("lines", out var lns) || lns.ValueKind != JsonValueKind.Array) continue;
                    foreach (var l in lns.EnumerateArray())
                        TryAddReadLine(l, lines, textProperty: "text", polygonProperty: "boundingPolygon");
                }
                return lines;
            }

            if (readResult.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in pages.EnumerateArray())
                {
                    if (!p.TryGetProperty("lines", out var lns) || lns.ValueKind != JsonValueKind.Array) continue;
                    foreach (var l in lns.EnumerateArray())
                        TryAddReadLine(l, lines, textProperty: "content", polygonProperty: "boundingBox");
                }
                return lines;
            }

            return Array.Empty<ReadLineDto>();
        }

        private static BoundingBoxDto ParseBoundingBox(JsonElement element)
        {
            if (element.TryGetProperty("boundingBox", out var bb))
            {
                int x = bb.TryGetProperty("x", out var xEl) ? xEl.GetInt32() : 0;
                int y = bb.TryGetProperty("y", out var yEl) ? yEl.GetInt32() : 0;
                int w = bb.TryGetProperty("w", out var wEl) ? wEl.GetInt32() : 0;
                int h = bb.TryGetProperty("h", out var hEl) ? hEl.GetInt32() : 0;
                return new BoundingBoxDto(x, y, w, h);
            }
            return new BoundingBoxDto(0, 0, 0, 0);
        }

        private static void TryAddReadLine(JsonElement lineElement, List<ReadLineDto> output, string textProperty, string polygonProperty)
        {
            var text = lineElement.TryGetProperty(textProperty, out var t) ? (t.GetString() ?? "") : "";
            var poly = ParsePolygon(lineElement, polygonProperty);

            output.Add(new ReadLineDto(
                Text: text,
                BoundingPolygon: poly,
                Words: new List<ReadWordDto>()
            ));
        }

        private static List<PointDto> ParsePolygon(JsonElement lineElement, string polygonProperty)
        {
            if (!lineElement.TryGetProperty(polygonProperty, out var polyElement)) return new();

            if (polyElement.ValueKind == JsonValueKind.Array && polyElement.GetArrayLength() > 0
                && polyElement[0].ValueKind == JsonValueKind.Object)
            {
                return polyElement.EnumerateArray()
                    .Select(p =>
                    {
                        var x = p.TryGetProperty("x", out var xEl) ? xEl.GetInt32() : 0;
                        var y = p.TryGetProperty("y", out var yEl) ? yEl.GetInt32() : 0;
                        return new PointDto(x, y);
                    })
                    .ToList();
            }

            if (polyElement.ValueKind == JsonValueKind.Array && polyElement.GetArrayLength() >= 8
                && polyElement[0].ValueKind == JsonValueKind.Number)
            {
                var nums = polyElement.EnumerateArray().Select(n => n.GetDouble()).ToArray();
                var pts = new List<PointDto>();
                for (int i = 0; i + 1 < nums.Length; i += 2)
                    pts.Add(new PointDto((int)nums[i], (int)nums[i + 1]));
                return pts;
            }

            return new();
        }
    }
}
