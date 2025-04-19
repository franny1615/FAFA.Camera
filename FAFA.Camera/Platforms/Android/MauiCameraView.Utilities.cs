using Android.Content;
using Android.Graphics;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Android.Views;
using Size = Android.Util.Size;
using Android.Runtime;
using RectF = Android.Graphics.RectF;

namespace FAFA.Camera.Platforms.Android;

public partial class MauiCameraView
{
    #region CHOOSE MAX VIDEO SIZE
    
    private static Size ChooseMaxVideoSize(Size[] choices)
    {
        var result = choices[0];
        var diference = 0;

        foreach (var size in choices)
        {
            if (size.Width != size.Height * 4 / 3 || 
                size.Width * size.Height <= diference) continue;
            
            result = size;
            diference = size.Width * size.Height;
        }

        return result;
    }
    
    #endregion
    
    #region CHOOSE VIDEO SIZE
    
    private Size ChooseVideoSize(Size[] choices)
    {
        var result = choices[0];
        var diference = int.MaxValue;
        var swapped = IsDimensionSwapped();
        foreach (var size in choices)
        {
            var w = swapped ? size.Height : size.Width;
            var h = swapped ? size.Width : size.Height;
            if (size.Width != size.Height * 4 / 3 || 
                w < Width || 
                h < Height ||
                size.Width * size.Height >= diference) continue;
            result = size;
            diference = size.Width * size.Height;
        }

        return result;
    }
    
    #endregion

    #region ADJUST ASPECT RATIO
    
    private void AdjustAspectRatio(int videoWidth, int videoHeight)
    {
        Matrix txform = new();
        RectF viewRect = new(0, 0, Width, Height);
        var centerX = viewRect.CenterX();
        var centerY = viewRect.CenterY();
        RectF bufferRect = new(0, 0, videoHeight, videoWidth);
        bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
        txform.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
        var scale = Math.Max(
                (float)Height / videoHeight,
                (float)Width / videoWidth);
        txform.PostScale(scale, scale, centerX, centerY);
        
        var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var rotation = windowManager?.DefaultDisplay?.Rotation;
        switch (rotation)
        {
            case SurfaceOrientation.Rotation90:
            case SurfaceOrientation.Rotation270:
                txform.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
                break;
            case SurfaceOrientation.Rotation180:
                txform.PostRotate(180, centerX, centerY);
                break;
        }
        textureView?.SetTransform(txform);
    }
    
    #endregion

    #region IS DIMENSION SWAPPED
    
    private bool IsDimensionSwapped()
    {
        var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var displayRotation = windowManager?.DefaultDisplay?.Rotation;
        var chars = cameraManager?.GetCameraCharacteristics(cameraView.Camera.DeviceId);
        var sensorOrientation = (int)(chars?.Get(CameraCharacteristics.SensorOrientation) ?? 0);
        var swappedDimensions = false;
        switch(displayRotation)
        {
            case SurfaceOrientation.Rotation0:
            case SurfaceOrientation.Rotation180:
                if (sensorOrientation == 90 || sensorOrientation == 270)
                {
                    swappedDimensions = true;
                }
                break;
            case SurfaceOrientation.Rotation90:
            case SurfaceOrientation.Rotation270:
                if (sensorOrientation == 0 || sensorOrientation == 180)
                {
                    swappedDimensions = true;
                }
                break;
        }
        return swappedDimensions;
    }
    
    #endregion
}