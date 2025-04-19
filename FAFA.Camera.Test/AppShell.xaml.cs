namespace FAFA.Camera.Test;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(CameraViewPage), typeof(CameraViewPage));
        Routing.RegisterRoute(nameof(VideoPreviewPage), typeof(VideoPreviewPage));
    }
}