using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Squidlr.App.Controls;

namespace Squidlr.App.Pages;

[QueryProperty(nameof(ContentIdentifier), "contentIdentifier")]
public sealed partial class DownloadPageViewModel : ObservableObject
{
    private readonly ContentProvider _contentProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileSaver _fileSaver;

    [ObservableProperty]
    private string _title = "Download";

    [ObservableProperty]
    private ContentIdentifier _contentIdentifier;

    [ObservableProperty]
    private Content? _content;

    public DownloadPageViewModel(
        ContentProvider contentProvider,
        IHttpClientFactory httpClientFactory,
        IFileSaver fileSaver)
    {
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _fileSaver = fileSaver ?? throw new ArgumentNullException(nameof(fileSaver));
    }
    private bool CanExecuteCancel()
    {
        return GetContentCommand.CanBeCanceled || DownloadCommand.CanBeCanceled;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCancel))]
    private async Task CancelAsync()
    {
        if (GetContentCommand.CanBeCanceled)
        {
            GetContentCommand.Cancel();
            await Shell.Current.GoToAsync("..");
        }

        if (DownloadCommand.CanBeCanceled)
        {
            DownloadCommand.Cancel();
        }
    }

    private bool CanExecuteGetContent()
    {
        return ContentIdentifier.Platform != SocialMediaPlatform.Unknown;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteGetContent))]
    private async Task GetContentAsync(CancellationToken cancellationToken)
    {
        Title = $"Download from {ContentIdentifier.Platform.GetPlatformName()}";

        string? errorMessage = null;
        try
        {
            var response = await _contentProvider.GetContentAsync(ContentIdentifier, cancellationToken);
            if (response.IsSuccessful)
            {
                Content = response.Value;
            }
            else
            {
                errorMessage = GetErrorMessage(response.Error);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            errorMessage = "An error occurred. Please try again.";
        }

        if (errorMessage != null)
        {
            await Toast.Make(errorMessage, ToastDuration.Long)
                       .Show(CancellationToken.None);
            await Shell.Current.GoToAsync("..");
        }
    }

    private bool CanExecuteDownload(VideoView videoView)
    {
        return Content != null && videoView.VideoSource != null;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDownload))]
    private async Task DownloadAsync(VideoView videoViewModel, CancellationToken cancellationToken)
    {
        var canceled = false;
        var videoSource = videoViewModel.VideoSource;

        try
        {
            var client = _httpClientFactory.GetPlatformHttpClient(Content!.Platform);
            var fileName = Content!.GetSafeVideoFileName(videoSource);

            using var stream = await client.GetStreamAsync(videoSource.Url, cancellationToken);
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
        catch (Exception e)
        {
            Debug.WriteLine(e);
            await Toast.Make("An error occurred. Please try again.")
                       .Show(CancellationToken.None);
        }

        if (canceled)
        {
            await Toast.Make("Download has been canceled.")
                       .Show(CancellationToken.None);
        }
    }

    private static string GetErrorMessage(RequestContentResult result)
    {
        return result switch
        {
            RequestContentResult.Canceled => "The request has been canceled.",
            RequestContentResult.NotFound => "Unfortunately the requested content could not be found.",
            RequestContentResult.PlatformNotSupported => "The requested content seems to belong to an unsupported social media platform.",
            RequestContentResult.NoVideo => "It seems that the content does not contain any video.",
            RequestContentResult.UnsupportedVideo => "The content contains an embedded video source which is not yet supported.",
            RequestContentResult.AdultContent => "Age-restricted adult content. This content might not be appropriate for people under 18 years old.",
            RequestContentResult.AccountSuspended => "The account containing the requested content has been suspended.",
            RequestContentResult.Protected => "The account owner limits who can view its content.",
            RequestContentResult.GatewayError => "The response from the social media servers was not what we expected. Please try again in a few minutes. In the meantime we will try to fix the issue.",
            _ => "There was an unexpected error. We will try to fix that as soon as possible!"
        };
    }
}
