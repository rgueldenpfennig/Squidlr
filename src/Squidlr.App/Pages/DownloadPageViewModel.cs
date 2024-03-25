using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Squidlr.App.Pages;

[QueryProperty(nameof(ContentIdentifier), "contentIdentifier")]
public sealed partial class DownloadPageViewModel : ObservableObject, IDisposable
{
    private readonly ContentProvider _contentProvider;

    private readonly CancellationTokenSource _cts = new();

    [ObservableProperty]
    private string _title = "Download";

    [ObservableProperty]
    private ContentIdentifier _contentIdentifier;

    [ObservableProperty]
    private Content? _content;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand DownloadCommand { private set; get; }

    public DownloadPageViewModel(ContentProvider contentProvider)
    {
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        //DownloadCommand = new AsyncRelayCommand()
    }

    public async ValueTask GetContentAsync()
    {
        if (ContentIdentifier.Platform == SocialMediaPlatform.Unknown)
        {
            throw new InvalidOperationException("No valid platform content identifier was provided.");
        }

        Title = $"Download from {ContentIdentifier.Platform}";
        IsBusy = true;

        var response = await _contentProvider.GetContentAsync(ContentIdentifier, _cts.Token);
        if (response.IsSuccessful)
        {
            Content = response.Value;
        }
        else
        {
            // SetErrorMessage(response.Error);
        }

        IsBusy = false;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _cts.Dispose();
    }
}
