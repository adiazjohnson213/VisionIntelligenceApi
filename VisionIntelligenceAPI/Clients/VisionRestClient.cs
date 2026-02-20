using System.Text;
using System.Text.Json;
using Polly;
using VisionIntelligenceAPI.Commons;
using VisionIntelligenceAPI.Resilience;

namespace VisionIntelligenceAPI.Clients
{
    public class VisionRestClient(HttpClient _http, VisionOptions _opt)
    {
        private static readonly ResiliencePipeline<HttpResponseMessage> _pipeline = RestRetryPipeline.Create();
        public async Task<JsonDocument> AnalyzeUrlAsync(string featuresCsv, string url, CancellationToken cancellationToken)
        {
            var resp = await _pipeline.ExecuteAsync(async ct =>
            {
                using var req = BuildRequest(featuresCsv, url);
                return await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            }, cancellationToken);

            var body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Vision REST failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }

            return JsonDocument.Parse(body);
        }

        private HttpRequestMessage BuildRequest(string featuresCsv, string url)
        {
            var requestUri =
                $"{_opt.Endpoint.TrimEnd('/')}/computervision/imageanalysis:analyze?api-version=2024-02-01&features={Uri.EscapeDataString(featuresCsv)}";

            var req = new HttpRequestMessage(HttpMethod.Post, requestUri);
            req.Headers.Add("Ocp-Apim-Subscription-Key", _opt.ApiKey);

            req.Content = new StringContent(
                JsonSerializer.Serialize(new { url }),
                Encoding.UTF8,
                "application/json");

            return req;
        }
    }
}
