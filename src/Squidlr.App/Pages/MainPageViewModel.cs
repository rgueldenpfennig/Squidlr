using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Squidlr.App.Pages;

public class MainPageViewModel : ObservableObject
{
    private string? _url;

    private bool _isValidUrl;
    private readonly UrlResolver _urlResolver;

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

    public MainPageViewModel(UrlResolver urlResolver)
    {
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));

        DownloadCommand = new AsyncRelayCommand(
            execute: ExecuteDownloadCommandAsync,
            canExecute: () =>
            {
                return IsValidUrl;
            });
    }

    private async Task ExecuteDownloadCommandAsync()
    {
        // SemanticScreenReader.Announce(...);
        await Shell.Current.GoToAsync($"download?url={Url}");
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
