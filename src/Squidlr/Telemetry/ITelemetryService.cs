namespace Squidlr.Telemetry;

public interface ITelemetryService
{
    void TrackEvent(string name, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
}