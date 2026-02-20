using System.Net;
using Polly;
using Polly.Retry;

namespace VisionIntelligenceAPI.Resilience
{
    public static class RestRetryPipeline
    {
        public static ResiliencePipeline<HttpResponseMessage> Create()
        => Create(maxRetryAttempts: 5, baseDelay: TimeSpan.FromMilliseconds(400), useJitter: true);

        public static ResiliencePipeline<HttpResponseMessage> Create(
            int maxRetryAttempts = 5,
            TimeSpan? baseDelay = null,
            bool useJitter = true)
        {
            var delay = baseDelay ?? TimeSpan.FromMilliseconds(400);

            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = maxRetryAttempts,
                    Delay = delay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = useJitter,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => r.StatusCode == (HttpStatusCode)429
                                        || r.StatusCode == HttpStatusCode.ServiceUnavailable)
                })
                .Build();
        }
    }
}
