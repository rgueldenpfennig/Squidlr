using System.Windows.Input;

namespace Squidlr.App.Controls;

public partial class VideoView : ContentView
{
    public static readonly BindableProperty VideoProperty = BindableProperty.Create(nameof(Video), typeof(Video), typeof(VideoView));

    public static readonly BindableProperty VideoSourceProperty = BindableProperty.Create(nameof(VideoSource), typeof(VideoSource), typeof(VideoView));

    public static readonly BindableProperty DownloadCommandProperty = BindableProperty.Create(nameof(DownloadCommand), typeof(ICommand), typeof(VideoView));

    /// <summary>
    /// The video to display.
    /// </summary>
    public Video Video
    {
        get => (Video)GetValue(VideoProperty);
        set => SetValue(VideoProperty, value);
    }

    /// <summary>
    /// The source of the video.
    /// </summary>
    public VideoSource VideoSource
    {
        get => (VideoSource)GetValue(VideoSourceProperty);
        set
        {
            SetValue(VideoSourceProperty, value);
            DownloadCommand.CanExecute(this);
        }
    }

    /// <summary>
    /// The command to execute when the download button is clicked.
    /// </summary>
    public ICommand DownloadCommand
    {
        get => (ICommand)GetValue(DownloadCommandProperty);
        set => SetValue(DownloadCommandProperty, value);
    }

    public VideoView()
    {
        InitializeComponent();
    }
}
