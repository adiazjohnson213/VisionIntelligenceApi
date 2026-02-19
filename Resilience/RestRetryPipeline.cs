using System.Net;
using Polly;
using Polly.Retry;

namespace VisionIntelligenceAPI.Resilience
{
    public static class RestRetryPipeline
    {
        public static ResiliencePipeline<HttpResponseMessage> Create()
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 5,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => r.StatusCode == (HttpStatusCode)429 || r.StatusCode == HttpStatusCode.ServiceUnavailable),
                    Delay = TimeSpan.FromMilliseconds(400),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                })
                .Build();
        }
    }
}
