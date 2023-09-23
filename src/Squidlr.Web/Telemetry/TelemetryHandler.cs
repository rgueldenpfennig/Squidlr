using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Net.Http.Headers;
using Squidlr.Web.States;

namespace Squidlr.Web.Telemetry;

public sealed class TelemetryHandler
{
    private readonly ILogger<TelemetryHandler> _logger;
    private readonly AppState _appState;
    private readonly TelemetryClient? _telemetryClient;

    public TelemetryHandler(ILogger<TelemetryHandler> logger, AppState appState, TelemetryClient? telemetryClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
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
            var pageView = new PageViewTelemetry(uri.ToString());
            pageView.Context.Session.Id = _appState.SessionId;
            pageView.Url = uri;

            if (!string.IsNullOrEmpty(_appState.Referer))
                pageView.Properties.Add(HeaderNames.Referer, _appState.Referer);
            if (!string.IsNullOrEmpty(_appState.UserAgent))
                pageView.Properties.Add(HeaderNames.UserAgent, _appState.UserAgent);

            _telemetryClient.TrackPageView(pageView);
        }
        else
        {
            _logger.LogInformation($"Tracked PageView: {uri}");
        }
    }
}
