using System.Windows.Input;

namespace Squidlr.App.Controls;

public partial class SocialMediaContentView : ContentView
{
    public static readonly BindableProperty SocialMediaContentProperty = BindableProperty.Create(nameof(SocialMediaContent), typeof(Content), typeof(SocialMediaContentView));
    
    public static readonly BindableProperty DownloadCommandProperty = BindableProperty.Create(nameof(DownloadCommand), typeof(ICommand), typeof(SocialMediaContentView));

    public Content SocialMediaContent
    {
        get => (Content)GetValue(SocialMediaContentProperty);
        set => SetValue(SocialMediaContentProperty, value);
    }

    public ICommand DownloadCommand
    {
        get => (ICommand)GetValue(DownloadCommandProperty);
        set => SetValue(DownloadCommandProperty, value);
    }

    public SocialMediaContentView()
    {
        InitializeComponent();
    }
}
