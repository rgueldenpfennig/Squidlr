using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Squidlr.App.Pages;

public class MainPageViewModel : ObservableObject
{
    private string? _url;

    private bool _isValidUrl;
    private readonly UrlResolver _urlResolver;
    private readonly IClipboard _clipboard;
    private readonly IServiceProvider _serviceProvider;

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

    public MainPageViewModel(UrlResolver urlResolver, IClipboard clipboard, IServiceProvider serviceProvider)
    {
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
                Debug.WriteLine($"Clipboard content changed and set URL: {text}");
                if (!DownloadCommand.IsRunning)
                {
                    await DownloadCommand.ExecuteAsync(null);
                }
            }
        }
    }

    private async Task ExecuteDownloadCommandAsync()
    {
        // TODO: SemanticScreenReader.Announce(...)

        var downloadPage = _serviceProvider.GetRequiredService<DownloadPage>();
        var contentIdentifier = _urlResolver.ResolveUrl(Url);
        if (downloadPage.IsLoaded)
        {
            Debug.WriteLine($"{nameof(DownloadPage)} is loaded");
            downloadPage.ViewModel.ContentIdentifier = contentIdentifier;
            if (downloadPage.ViewModel.GetContentCommand.CanExecute(null))
            {
                await downloadPage.ViewModel.GetContentCommand.ExecuteAsync(null);
            }
        }
        else
        {
            Debug.WriteLine($"Navigating to {nameof(DownloadPage)}");
            var navigationParameter = new Dictionary<string, object>
            {
                { "contentIdentifier", contentIdentifier }
            };
            await Shell.Current.GoToAsync("/download", navigationParameter);
        }
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
