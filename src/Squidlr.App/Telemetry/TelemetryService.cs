using Squidlr.Telemetry;

namespace Squidlr.App.Telemetry;

internal class TelemetryService : ITelemetryService
{
    public void TrackEvent(string name, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        // do nothing for now
    }
}
