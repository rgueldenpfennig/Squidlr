namespace Squidlr.App.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageViewModel viewModel)
        {
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();

            // TODO: ensure internet connection (https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/communication/networking?view=net-maui-8.0&tabs=android)
        }
    }
}
