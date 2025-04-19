using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAFA.Camera.Enums;
using FAFA.Camera.Models;
using ImageFormat = FAFA.Camera.Enums.ImageFormat;

namespace FAFA.Camera.Test;

public partial class CameraViewPage
{
    private readonly CameraViewPageViewModel _viewModel;
    private bool _isRecording = false;
    
    public CameraViewPage(CameraViewPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private void CamerasLoaded(object? sender, EventArgs e)
    {
        if (cameraView.NumCamerasDetected <= 0) return;
        
        if (cameraView.NumMicrophonesDetected > 0)
            cameraView.Microphone = cameraView.Microphones.First();

        _viewModel.CameraOptions = new ObservableCollection<string>(
            cameraView.Cameras.Select((c) => c.Name).ToList());
        _viewModel.SelectedCamera = _viewModel.CameraOptions.First();
        _viewModel.SelectedCameraIndex = 0;
    }
    
    private void DifferentItemPicked(object? sender, EventArgs e)
    {
        if (_viewModel.SelectedCameraIndex >= _viewModel.CameraOptions.Count ||
            _viewModel.SelectedCameraIndex < 0) return;

        _ = StartCamera(cameraView.Cameras[_viewModel.SelectedCameraIndex]);
    }
    
    private async Task StartCamera(CameraInfo camera)
    {
        await Task.Delay(1000);
        cameraView.Camera = camera;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await cameraView.StartCameraAsync();
        });
    }

    private void TakePhoto(object? sender, EventArgs e)
    {
        _ = TakePhotoAsync();
    }

    private async Task TakePhotoAsync()
    {
        try
        {
            var result = await cameraView.TakePhotoAsync();
            var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}.png");
            
            var fileStream = new FileStream(path, FileMode.Create);
            await result.CopyToAsync(fileStream);
            fileStream.Close();
            
            _viewModel.ImagePaths.Add(new ImageResult { ImagePath = path });
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
        }
    }

    private void TakeSnapShot(object? sender, EventArgs e)
    {
        _ = SaveSnapshotAsync();
    }

    private async Task SaveSnapshotAsync()
    {
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}.png");
            var result = await cameraView.SaveSnapShot(
                ImageFormat.PNG,
                path);

            if (result)
            {
                _viewModel.ImagePaths.Add(new ImageResult { ImagePath = path });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to save snapshot");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void TakeVideo(object? sender, EventArgs e)
    {
        if (!_isRecording)
        {
            _ = TakeVideoAsync();    
        }
        else
        {
            _ = StopRecordingAsync();
        }
    }

    private async Task StopRecordingAsync()
    {
        try
        {
            _isRecording = false;
            RecordButton.Text = "Take Video";
            var result = await cameraView.StopRecordingAsync();
            System.Diagnostics.Debug.WriteLine(result.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private async Task TakeVideoAsync()
    {
        try
        {
            var extension = "mp4";
            if (OperatingSystem.IsAndroid())
                extension = "mov";
            var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}.{extension}");
            var result = await cameraView.StartRecordingAsync(path);
            
            if (result == CameraResult.Success)
            {
                _isRecording = true;
                RecordButton.Text = "Stop Recording";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}