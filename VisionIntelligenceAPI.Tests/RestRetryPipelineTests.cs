using System.Net;
using VisionIntelligenceAPI.Resilience;

namespace VisionIntelligenceAPI.Tests
{
    public sealed class RestRetryPipelineTests
    {
        [Fact]
        public async Task Pipeline_RetriesOn429_ThenSucceeds()
        {
            var pipeline = RestRetryPipeline.Create(maxRetryAttempts: 3, baseDelay: TimeSpan.Zero, useJitter: false);

            int calls = 0;

            var resp = await pipeline.ExecuteAsync(async ct =>
            {
                calls++;

                if (calls <= 2)
                    return new HttpResponseMessage((HttpStatusCode)429);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }, CancellationToken.None);

            Assert.Equal(3, calls);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
