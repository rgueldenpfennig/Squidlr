namespace Squidlr.App.Pages;

public partial class DownloadPage : ContentPage
{
    private readonly DownloadPageViewModel _viewModel;

    public DownloadPage(DownloadPageViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.GetContentAsync();
    }
}
