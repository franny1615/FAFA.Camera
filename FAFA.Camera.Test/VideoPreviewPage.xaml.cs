using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.Views;

namespace FAFA.Camera.Test;

public partial class VideoPreviewPage : ContentPage
{
    public VideoPreviewPage()
    {
        InitializeComponent();

        if (File.Exists(App.VideoPreviewPath))
        {
            System.Diagnostics.Debug.WriteLine($"FILE EXISTS");
        }
        
        if (!string.IsNullOrEmpty(App.VideoPreviewPath))
            Media.Source = new FileMediaSource
            {
                Path = App.VideoPreviewPath
            };
    }
}