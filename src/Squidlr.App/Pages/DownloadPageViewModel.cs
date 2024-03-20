using CommunityToolkit.Mvvm.ComponentModel;

namespace Squidlr.App.Pages;

[QueryProperty(nameof(Url), "url")]
public partial class DownloadPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _url;

    partial void OnUrlChanging(string? value)
    {
    }
}
