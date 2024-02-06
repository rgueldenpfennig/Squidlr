using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Net.Http.Headers;
using Squidlr.Web.States;

namespace Squidlr.Web.Telemetry;

public sealed class AppStateTelemetryInitializer : TelemetryInitializerBase
{

    public AppStateTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
             : base(httpContextAccessor)
    {
    }

    protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
    {
        var appState = platformContext.Items[nameof(AppState)] as AppState;
        if (appState != null)
        {
            telemetry.Context.Session.Id = appState.SessionId;
            telemetry.Context.User.Id = appState.SessionId;
            // telemetry.Context.User.UserAgent is obsolete: https://github.com/microsoft/ApplicationInsights-dotnet/issues/2722

            if (telemetry is ISupportProperties telemetryWithProperties)
            {
                if (!string.IsNullOrEmpty(appState.Referer))
                    telemetryWithProperties.Properties.TryAdd(HeaderNames.Referer, appState.Referer);
                if (!string.IsNullOrEmpty(appState.UserAgent))
                    telemetryWithProperties.Properties.TryAdd(HeaderNames.UserAgent, appState.UserAgent);
            }
        }
    }
}
