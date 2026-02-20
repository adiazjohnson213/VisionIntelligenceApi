using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace VisionIntelligenceAPI.Observability
{
    public static class VisionTelemetry
    {
        public const string ServiceName = "VisionIntelligenceAPI";

        public static readonly ActivitySource ActivitySource = new(ServiceName);

        public static readonly Meter Meter = new(ServiceName);

        public static readonly Counter<long> RequestsTotal =
            Meter.CreateCounter<long>("vision.requests.total", unit: "request");

        public static readonly Counter<long> Throttles429Total =
            Meter.CreateCounter<long>("vision.throttles_429.total", unit: "event");

        public static readonly Histogram<double> RequestDurationMs =
            Meter.CreateHistogram<double>("vision.request.duration_ms", unit: "ms");
    }
}
