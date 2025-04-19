using Android.Hardware.Camera2;
using Android.Views;
using Android.Hardware.Camera2.Params;

namespace FAFA.Camera.Platforms.Android;

public partial class MauiCameraView
{
    private void StartPreview()
    {
        if (textureView == null)
            return;
        
        while (textureView.SurfaceTexture == null || 
               !textureView.IsAvailable || 
               cameraDevice == null) Thread.Sleep(100);
        
        SetupImageReader(cameraView.PhotosResolution);
        if (videoSize == null)
            return;
        
        var texture = textureView.SurfaceTexture;
        texture.SetDefaultBufferSize(videoSize.Width, videoSize.Height);

        previewBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
        
        var previewSurface = new Surface(texture);
        previewBuilder.AddTarget(previewSurface);
        
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            List<OutputConfiguration> surfaces = 
            [
                new(previewSurface)
            ];
            
            if (imgReader?.Surface != null)
                surfaces.Add(new OutputConfiguration(imgReader.Surface));

            if (executorService is null) return;
            
            SessionConfiguration config = new((int)SessionType.Regular, surfaces, executorService, sessionCallback);
            cameraDevice.CreateCaptureSession(config);
        }
        else
        {
            List<Surface> surfaces = 
            [
                previewSurface
            ];
            
            if (imgReader?.Surface != null)
                surfaces.Add(imgReader.Surface);
            
            cameraDevice.CreateCaptureSession(surfaces, sessionCallback, null);
        }
    }
    
    private void UpdatePreview()
    {
        if (cameraDevice == null || videoSize == null)
            return;

        try
        {
            if (CaptureRequest.ControlMode is not null)
                previewBuilder?.Set(CaptureRequest.ControlMode, Java.Lang.Integer.ValueOf((int)ControlMode.Auto));
            AdjustAspectRatio(videoSize.Width, videoSize.Height);
            SetZoomFactor(cameraView.ZoomFactor);
            
            if (recording) 
                mediaRecorder?.Start();
        }
        catch (CameraAccessException e)
        {
            e.PrintStackTrace();
        }
    }
}