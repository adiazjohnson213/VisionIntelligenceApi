using System.Net;
using System.Net.Http.Json;

namespace VisionIntelligenceAPI.Tests
{
    public class VisionEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public VisionEndpointsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AnalyzeUrl_HappyPath_Returns200_AndEchoesCorrelationId()
        {
            var req = new
            {
                url = "https://example.com/img.jpg",
                requirements = new[] { "Caption" },
                engine = "Sdk"
            };

            var http = new HttpRequestMessage(HttpMethod.Post, "/api/v1/vision/image-analysis:analyze-url")
            {
                Content = JsonContent.Create(req)
            };
            http.Headers.Add("X-Correlation-Id", "it-001");

            var resp = await _client.SendAsync(http);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            Assert.NotNull(json);
            Assert.Equal("it-001", json!["correlationId"]?.ToString());
        }

        [Fact]
        public async Task AnalyzeUrl_InvalidRequirementString_Returns400()
        {
            var req = new
            {
                url = "https://example.com/img.jpg",
                requirements = new[] { "NotARealRequirement" },
                engine = "Sdk"
            };

            var resp = await _client.PostAsJsonAsync("/api/v1/vision/image-analysis:analyze-url", req);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }
}
