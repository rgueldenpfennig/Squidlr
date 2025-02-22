using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using Squidlr.Web.States;
using Squidlr.Web.Telemetry;

namespace Squidlr.Web;

public partial class Routes : ComponentBase
{
    protected override void OnInitialized()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            InitializeAppState(httpContext);

            if (!httpContext.WebSockets.IsWebSocketRequest)
            {
                TelemetryHandler.TrackPageView(new Uri(httpContext.Request.GetEncodedUrl(), UriKind.Absolute));
            }
        }
    }

    private void InitializeAppState(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.ContainsKey(ApplicationOptions.SessionCookieName))
        {
            AppState.SessionId = httpContext.Request.Cookies[ApplicationOptions.SessionCookieName]!;
        }
        else
        {
            AppState.SessionId = Guid.NewGuid().ToString();
            httpContext.Response.Cookies.Append(ApplicationOptions.SessionCookieName, AppState.SessionId, new()
            {
                HttpOnly = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Secure = true,
            });
        }

        AppState.Referer = httpContext.Request.Headers[HeaderNames.Referer].ToString() ?? "";
        AppState.UserAgent = httpContext.Request.Headers[HeaderNames.UserAgent].ToString() ?? "";

        if (httpContext.Request.Headers["Sec-GPC"] == "1" || httpContext.Request.Headers["DNT"] == "1")
        {
            AppState.DoNotTrack = true;
        }

        httpContext.Items[nameof(AppState)] = AppState;
    }

    private Task OnBeforeInternalNavigationAsync(LocationChangingContext context)
    {
        if (context.TargetLocation.Equals(NavigationManager.Uri, StringComparison.OrdinalIgnoreCase))
        {
            context.PreventNavigation();
        }

        return Task.CompletedTask;
    }
}