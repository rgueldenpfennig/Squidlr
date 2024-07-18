namespace Squidlr.App.Pages;

public partial class DownloadPage : ContentPage
{
    private readonly DownloadPageViewModel _viewModel;

    public DownloadPageViewModel ViewModel => _viewModel;

    public DownloadPage(DownloadPageViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = ViewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel.GetContentCommand.CanExecute(null))
        {
            await ViewModel.GetContentCommand.ExecuteAsync(null);
        }
    }
}
