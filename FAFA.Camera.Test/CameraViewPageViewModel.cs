using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FAFA.Camera.Enums;
using FAFA.Camera.Models;

namespace FAFA.Camera.Test;

public partial class CameraViewPageViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<string> cameraOptions = [];
    
    [ObservableProperty] private string selectedCamera = string.Empty;
    
    [ObservableProperty] private int selectedCameraIndex = 0;

    [ObservableProperty] private float zoom = 0.0f;
    
    [ObservableProperty] private bool torchEnabled = false;

    [ObservableProperty] private bool mirror = false;
    
    [ObservableProperty] private FlashMode _flashMode = FlashMode.Disabled;

    [ObservableProperty] private bool enableFlash = false;

    [ObservableProperty] private ObservableCollection<ImageResult> imagePaths = [];

    partial void OnEnableFlashChanged(bool value)
    {
        FlashMode = value ? FlashMode.Enabled : FlashMode.Disabled;
    }
}

public class ImageResult
{
    public string ImagePath { get; set; } = string.Empty;
}