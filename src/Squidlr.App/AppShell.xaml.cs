using Squidlr.App.Pages;

namespace Squidlr.App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("download", typeof(DownloadPage));
        }
    }
}
