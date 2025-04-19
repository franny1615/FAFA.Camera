using Android.Content;
using Android.Graphics;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.Views;
using Android.Hardware.Camera2.Params;
using Size = Android.Util.Size;
using Class = Java.Lang.Class;
using Rect = Android.Graphics.Rect;
using Android.Runtime;
using Android.OS;
using FAFA.Camera.Enums;
using ImageFormat = FAFA.Camera.Enums.ImageFormat;


namespace FAFA.Camera.Platforms.Android;

public partial class MauiCameraView
{
    internal async Task<System.IO.Stream> TakePhotoAsync(
        ImageFormat imageFormat, 
        Microsoft.Maui.Graphics.Size photosResolution)
    {
        if (camChars == null || !started || recording || textureView == null)
            return new MemoryStream();

        MemoryStream stream = new();
        
        var singleRequest = cameraDevice?.CreateCaptureRequest(CameraTemplate.StillCapture);
        
        captureDone = false;
        capturePhoto = [];
        
        if (cameraView.Camera.HasFlashUnit && CaptureRequest.FlashMode is not null)
        {
            switch (cameraView.FlashMode)
            {
                case Enums.FlashMode.Auto:
                    singleRequest?.Set(CaptureRequest.FlashMode, Java.Lang.Integer.ValueOf((int)ControlAEMode.OnAutoFlash));
                    break;
                case Enums.FlashMode.Enabled:
                    singleRequest?.Set(CaptureRequest.FlashMode, Java.Lang.Integer.ValueOf((int)ControlAEMode.On));
                    break;
                case Enums.FlashMode.Disabled:
                    singleRequest?.Set(CaptureRequest.FlashMode, Java.Lang.Integer.ValueOf((int)ControlAEMode.Off));
                    break;
            }
        }

        var rotation = GetJpegOrientation();
        if (CaptureRequest.JpegOrientation is not null)
            singleRequest?.Set(CaptureRequest.JpegOrientation, Java.Lang.Integer.ValueOf(rotation));

        var destZoom = Math.Clamp(cameraView.ZoomFactor, 1, Math.Min(6, cameraView.Camera.MaxZoomFactor)) - 1;
        var m = (Rect?)camChars.Get(CameraCharacteristics.SensorInfoActiveArraySize);

        if (m == null) 
            return new MemoryStream();
        
        var minW = (int)(m.Width() / (cameraView.Camera.MaxZoomFactor));
        var minH = (int)(m.Height() / (cameraView.Camera.MaxZoomFactor));
        var newWidth = (int)(m.Width() - (minW * destZoom));
        var newHeight = (int)(m.Height() - (minH * destZoom));
        Rect zoomArea = new((m.Width() - newWidth) / 2, (m.Height() - newHeight) / 2, newWidth, newHeight);
        
        if (CaptureRequest.ScalerCropRegion is not null)
            singleRequest?.Set(CaptureRequest.ScalerCropRegion, zoomArea);
        
        try
        {
            if (imgReader?.Surface is not null)
                singleRequest?.AddTarget(imgReader.Surface);
            
            if (singleRequest is not null)
                previewSession?.Capture(singleRequest.Build(), null, null);
            
            while (!captureDone) await Task.Delay(50);

            if (capturePhoto.Length == 0)
                return new MemoryStream();
            
            if ((int)textureView.ScaleX == -1 || imageFormat != ImageFormat.JPEG)
            {
                var bitmap = await BitmapFactory.DecodeByteArrayAsync(capturePhoto, 0, capturePhoto.Length);
                if (bitmap != null)
                {
                    if ((int)textureView.ScaleX == -1)
                    {
                        Matrix matrix = new();
                        matrix.PreRotate(rotation);
                        matrix.PostScale(-1, 1);
                        bitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, false);
                    }

                    var iformat = imageFormat switch
                    {
                        ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                        _ => Bitmap.CompressFormat.Png
                    };
                    
                    stream = new MemoryStream();
                    
                    if (iformat is not null)
                        await bitmap.CompressAsync(iformat, 100, stream);
                    
                    stream.Position = 0;
                }
            }
            else
            {
                stream = new();
                stream.Write(capturePhoto);
                stream.Position = 0;
            }
        }
        catch (Java.Lang.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }

        return stream;
    }

    private void SetupImageReader(Microsoft.Maui.Graphics.Size PhotosResolution)
    {
        var maxVideoSize = ChooseMaxVideoSize(GetVideoSizeChoices());
        if (PhotosResolution.Width != 0 && PhotosResolution.Height != 0)
            maxVideoSize = new Size((int)PhotosResolution.Width, (int)PhotosResolution.Height);
        
        imgReader = ImageReader.NewInstance(maxVideoSize.Width, maxVideoSize.Height, ImageFormatType.Jpeg, 1);
        backgroundThread = new HandlerThread("CameraBackground");
        backgroundThread.Start();
        if (backgroundThread.Looper is not null)
            backgroundHandler = new Handler(backgroundThread.Looper);
        
        imgReader.SetOnImageAvailableListener(photoListener, backgroundHandler);
    }
    
    private Size[] GetVideoSizeChoices()
    {
        if (cameraManager == null)
            return [];
        
        camChars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
        
        var map = (StreamConfigurationMap?)camChars.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
        if (map is null) return [];

        var klass = Class.FromType(typeof(ImageReader));
        var choices = map.GetOutputSizes(klass) ?? [];
        videoSize = ChooseVideoSize(choices);
        
        return choices;
    }
    
    private int GetJpegOrientation()
    {
        if (cameraManager == null)
            return 0;
        
        var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var displayRotation = windowManager?.DefaultDisplay?.Rotation;
        var chars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
        var sensorOrientation = (int)(chars.Get(CameraCharacteristics.SensorOrientation) ?? 0);
        var deviceOrientation = displayRotation switch
        {
            SurfaceOrientation.Rotation90 => 90,
            SurfaceOrientation.Rotation180 => 180,
            SurfaceOrientation.Rotation270 => 270,
            _ => 0
        };

        var cameraPosition = cameraView.Camera.Position == CameraPosition.Front ? -1 : 1;
        return (sensorOrientation - deviceOrientation * cameraPosition + 360) % 360;
    }
    
    private class ImageAvailableListener(MauiCameraView camView) : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public void OnImageAvailable(ImageReader? reader)
        {
            try
            {
                var image = reader?.AcquireNextImage();

                var buffer = image?.GetPlanes()?[0].Buffer;
                if (buffer == null)
                    return;

                var imageData = new byte[buffer.Capacity()];
                buffer.Get(imageData);
                camView.capturePhoto = imageData;
                buffer.Clear();
                image?.Close();
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex);
#endif
            }
            camView.captureDone = true;
        }
    }
}