namespace Squidlr.App.Controls;

public partial class SocialMediaContentView : ContentView
{
    public static readonly BindableProperty SocialMediaContentProperty = BindableProperty.Create(nameof(SocialMediaContent), typeof(Content), typeof(SocialMediaContentView));

    public Content SocialMediaContent
    {
        get => (Content)GetValue(SocialMediaContentProperty);
        set => SetValue(SocialMediaContentProperty, value);
    }

    public SocialMediaContentView()
    {
        InitializeComponent();
    }
}