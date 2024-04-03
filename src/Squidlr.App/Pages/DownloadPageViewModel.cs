using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Squidlr.App.Pages;

[QueryProperty(nameof(ContentIdentifier), "contentIdentifier")]
public sealed partial class DownloadPageViewModel : ObservableObject, IDisposable
{
    private readonly ContentProvider _contentProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileSaver _fileSaver;
    private readonly CancellationTokenSource _cts = new();

    [ObservableProperty]
    private string _title = "Download";

    [ObservableProperty]
    private ContentIdentifier _contentIdentifier;

    [ObservableProperty]
    private Content? _content;

    [ObservableProperty]
    private bool _isBusy;

    public DownloadPageViewModel(
        ContentProvider contentProvider,
        IHttpClientFactory httpClientFactory,
        IFileSaver fileSaver)
    {
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _fileSaver = fileSaver ?? throw new ArgumentNullException(nameof(fileSaver));
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

    private bool CanExecuteDownload(Video? video)
    {
        return Content is not null && video?.VideoSources.Count >= 1;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDownload), IncludeCancelCommand = true)]
    private async Task DownloadAsync(Video? video, CancellationToken cancellationToken)
    {
        // TODO: modal dialog to choose desired resolution (VideoSource)
        //var result = await Shell.Current.DisplayActionSheet(
        //    "Select resolution",
        //    "Cancel",
        //    "Destruction",
        //    video.VideoSources.Select(vs => vs.Size.ToString()).ToArray());

        IsBusy = true;
        var canceled = false;

        try
        {
            var selectedVideoSource = video!.VideoSources.OrderByDescending(vs => vs.Bitrate).First();
            var client = _httpClientFactory.GetPlatformHttpClient(Content!.Platform);
            var fileName = ContentIdentifier.GetSafeVideoFileName(selectedVideoSource.Url);

            using var stream = await client.GetStreamAsync(selectedVideoSource.Url, cancellationToken);
            var fileSaverResult = await _fileSaver.SaveAsync(fileName, stream, cancellationToken);
            if (fileSaverResult.IsSuccessful)
            {
                await Toast.Make($"The video was saved at: {fileSaverResult.FilePath}", ToastDuration.Long)
                           .Show(cancellationToken);
            }
            else if (fileSaverResult.Exception is TaskCanceledException)
            {
                canceled = true;
            }
            else
            {
                await Toast.Make($"Failed to save the video: {fileSaverResult.Exception.Message}", ToastDuration.Long)
                           .Show(cancellationToken);
            }
        }
        catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
        {
            canceled = true;
        }
        finally
        {
            IsBusy = false;
        }

        if (canceled)
        {
            await Toast.Make("Download has been canceled.")
                       .Show(CancellationToken.None);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _cts.Dispose();
    }
}
