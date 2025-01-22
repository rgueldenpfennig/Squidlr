using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using Squidlr.Web.States;
using Squidlr.Web.Telemetry;

namespace Squidlr.Web.Shared;

public partial class VideoSearchQuery
{
    [Parameter]
    public bool ShowPlatformTabs { get; set; }

    [Parameter]
    public bool FocusInput { get; set; }

    [Inject]
    public TelemetryHandler TelemetryHandler { get; set; } = default!;

    [Inject]
    public AppState AppState { get; set; } = default!;

    private ElementReference _inputReference;

    private ContentIdentifier _contentIdentifier = ContentIdentifier.Unknown;
    private bool _downloadEnabled;
    private string? _errorMessage;
    private DateTimeOffset? _unknownContentUrlTrackedAt;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (FocusInput && firstRender)
        {
            await _inputReference.FocusAsync();
        }
    }

    private async Task PasteClipboard()
    {
        try
        {
            var text = await ClipboardService.ReadTextAsync();
            HandleInput(text);
        }
        catch
        {
            _errorMessage = $"Sorry, Squidlr is not able to read from your clipboard this time 😔<br/>Please paste your URL manually.";
        }
    }

    private void HandleInput(string? value)
    {
        State.Url = value;
        _errorMessage = null;
        _downloadEnabled = false;

        if (!string.IsNullOrEmpty(State.Url))
        {
            if (!Uri.TryCreate(State.Url, UriKind.Absolute, out _))
            {
                _errorMessage = "Please ensure to enter a valid URL before you continue.";
                return;
            }

            _contentIdentifier = UrlResolver.ResolveUrl(State.Url);
            if (_contentIdentifier != ContentIdentifier.Unknown)
            {
                _downloadEnabled = true;
                DownloadVideo();

                return;
            }

            _errorMessage =
                """
                We couldn’t recognize the URL you provided. Please double-check that the link is from a <a href="#supported-platforms">supported social media platform</a>.<br/>
                If you believe this is a mistake, we’d appreciate it if you could <a href="#report-url">report the issue here</a>.<br/>
                Your feedback helps us improve!
                """;

            if (!AppState.DoNotTrack && (_unknownContentUrlTrackedAt == null || DateTimeOffset.UtcNow - _unknownContentUrlTrackedAt > TimeSpan.FromSeconds(15)))
            {
                _unknownContentUrlTrackedAt = DateTimeOffset.UtcNow;
                var eventProperties = new Dictionary<string, string>
                {
                    { "Url", State.Url }
                };

                TelemetryHandler.TrackEvent("UnknownContentUrl", eventProperties);
            }
        }
    }

    private void OnKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && _downloadEnabled)
        {
            DownloadVideo();
        }
    }

    private void DownloadVideo()
    {
        var url = State.Url;
        State.Url = null;

        NavigationManager.NavigateTo(QueryHelpers.AddQueryString("/download", "url", url!));
    }

    private bool IsActive(SocialMediaPlatform platform)
    {
        return _contentIdentifier.Platform == platform;
    }
}