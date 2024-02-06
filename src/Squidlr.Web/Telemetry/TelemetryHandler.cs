using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Squidlr.Web.Telemetry;

public sealed class TelemetryHandler
{
    private readonly ILogger<TelemetryHandler> _logger;
    private readonly TelemetryClient? _telemetryClient;

    public TelemetryHandler(ILogger<TelemetryHandler> logger, TelemetryClient? telemetryClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryClient = telemetryClient;
    }

    public void TrackEvent(string name, IDictionary<string, string>? properties = null)
    {
        if (_telemetryClient != null)
        {
            _telemetryClient.TrackEvent(name, properties);
        }
        else
        {
            _logger.LogInformation($"Tracked Event: {name}");
        }
    }

    public void TrackPageView(Uri uri)
    {
        if (_telemetryClient != null)
        {
            var pageView = new PageViewTelemetry(pageName: uri.ToString())
            {
                Url = uri
            };

            _telemetryClient.TrackPageView(pageView);
        }
        else
        {
            _logger.LogInformation($"Tracked PageView: {uri}");
        }
    }
}
