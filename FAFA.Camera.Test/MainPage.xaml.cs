namespace FAFA.Camera.Test;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void GoToCameraViewPage(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(CameraViewPage));
    }

    private void GoPreviewTakenVideo(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(VideoPreviewPage));
    }
}