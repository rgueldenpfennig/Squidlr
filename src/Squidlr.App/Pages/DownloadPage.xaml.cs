namespace Squidlr.App.Pages;

public partial class DownloadPage : ContentPage
{
    public DownloadPage(DownloadPageViewModel viewModel)
    {
        BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }
}
