using Microsoft.ApplicationInsights;
using Squidlr.Telemetry;

namespace Squidlr.Api.Telemetry;

public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient? _telemetryClient;

    public TelemetryService(TelemetryClient? telemetryClient = null)
    {
        _telemetryClient = telemetryClient;
    }

    public void TrackEvent(
        string name,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        _telemetryClient?.TrackEvent(name, properties, metrics);
    }
}
