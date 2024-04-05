using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Squidlr.App.Pages;

public class MainPageViewModel : ObservableObject
{
    private string? _url;

    private bool _isValidUrl;
    private readonly UrlResolver _urlResolver;
    private readonly IClipboard _clipboard;

    public IAsyncRelayCommand DownloadCommand { private set; get; }

    public string? Url
    {
        set
        {
            OnUrlChanged(value);
            SetProperty(ref _url, value);
        }

        get => _url;
    }

    public bool IsValidUrl
    {
        private set => SetProperty(ref _isValidUrl, value);
        get => _isValidUrl;
    }

    public MainPageViewModel(UrlResolver urlResolver, IClipboard clipboard)
    {
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));

        _clipboard.ClipboardContentChanged += OnClipboardContentChangedAsync;

#if DEBUG
        Url = "https://twitter.com/sentdefender/status/1772514015790477667";
#endif

        DownloadCommand = new AsyncRelayCommand(
            execute: ExecuteDownloadCommandAsync,
            canExecute: () =>
            {
                return IsValidUrl;
            });
    }

    private async void OnClipboardContentChangedAsync(object? sender, EventArgs e)
    {
        if (_clipboard.HasText)
        {
            var text = await _clipboard.GetTextAsync();
            var contentIdentifier = _urlResolver.ResolveUrl(text);
            if (contentIdentifier != ContentIdentifier.Unknown)
            {
                Url = text;
            }
        }
    }

    private async Task ExecuteDownloadCommandAsync()
    {
        // TODO: SemanticScreenReader.Announce(...)
        var navigationParameter = new Dictionary<string, object>
        {
            { "contentIdentifier", _urlResolver.ResolveUrl(Url) }
        };
        await Shell.Current.GoToAsync("/download", navigationParameter);
    }

    private void OnUrlChanged(string? value)
    {
        var contentIdentifier = _urlResolver.ResolveUrl(value);
        if (contentIdentifier == ContentIdentifier.Unknown)
        {
            IsValidUrl = false;
        }
        else
        {
            IsValidUrl = true;
        }
    }
}
