using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Squidlr.Web.Telemetry;

public sealed class TelemetryComponent : ComponentBase, IDisposable
{
    [Inject]
    public required TelemetryHandler TelemetryHandler { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            NavigationManager.LocationChanged += NavigationManagerOnLocationChanged;
        }
    }

    private void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        TelemetryHandler.TrackPageView(new Uri(e.Location, UriKind.Absolute));
    }


    public void Dispose()
    {
        NavigationManager.LocationChanged -= NavigationManagerOnLocationChanged;
    }
}
