using Android.Graphics;
using ImageFormat = FAFA.Camera.Enums.ImageFormat;

namespace FAFA.Camera.Platforms.Android;

public partial class MauiCameraView
{
    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!snapping && 
            cameraView.AutoSnapShotSeconds > 0 
            && (DateTime.Now - cameraView.lastSnapshot).TotalSeconds >= cameraView.AutoSnapShotSeconds)
        {
            Task.Run(RefreshSnapShot);
        }
        else if (cameraView.BarCodeDetectionEnabled)
        {
            frames++;
            if (frames < cameraView.BarCodeDetectionFrameRate) return;
            
            var processQR = false;
            lock (cameraView.currentThreadsLocker)
            {
                if (cameraView.currentThreads < cameraView.BarCodeDetectionMaxThreads)
                {
                    cameraView.currentThreads++;
                    processQR = true;
                }
            }

            if (!processQR) return;
            
            ProccessQR();
            frames = 0;
        }
    }
    
    private void RefreshSnapShot()
    {
        switch (cameraView.AutoSnapShotFormat)
        {
            case ImageFormat.JPEG:
                var snap = GetSnapShot(ImageFormat.JPEG, true);
                if (snap != null)
                    cameraView.RefreshSnapshot(snap);
                break;
            case ImageFormat.PNG:
            default:
                var snap2 = GetSnapShot(ImageFormat.PNG, true);
                if (snap2 != null)
                    cameraView.RefreshSnapshot(snap2);
                break;
        }
    }
    
    private Bitmap? TakeSnap()
    {
        Bitmap? bitmap = null;
        try
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                bitmap = textureView?.GetBitmap(null); 
                bitmap = textureView?.Bitmap;
            }).Wait();
            
            if (bitmap != null)
            {
                bitmap = Bitmap.CreateBitmap(
                    bitmap, 
                    0, 
                    0, 
                    bitmap.Width, 
                    bitmap.Height, 
                    textureView?.GetTransform(null), 
                    false);
                bitmap = Bitmap.CreateBitmap(
                    bitmap, 
                    (bitmap.Width - Width) / 2, 
                    (bitmap.Height - Height) / 2, 
                    Width, 
                    Height);
                
                if (textureView is { ScaleX: -1 })
                {
                    Matrix matrix = new();
                    matrix.PreScale(-1, 1);
                    bitmap = Bitmap.CreateBitmap(
                        bitmap, 
                        0, 
                        0, 
                        bitmap.Width, 
                        bitmap.Height, 
                        matrix, 
                        false);
                }
            }
        }
        catch
        {
            // ignored
        }

        return bitmap;
    }
    internal ImageSource? GetSnapShot(ImageFormat imageFormat, bool auto = false)
    {
        ImageSource? result = null;

        if (!started || snapping) return result;
        
        snapping = true;
        var bitmap = TakeSnap();

        if (bitmap != null)
        {
            var iformat = imageFormat switch
            {
                ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                _ => Bitmap.CompressFormat.Png
            };
            MemoryStream stream = new();
            if (iformat != null)
                bitmap.Compress(iformat, 100, stream);
            stream.Position = 0;
            if (auto)
            {
                if (cameraView.AutoSnapShotAsImageSource)
                    result = ImageSource.FromStream(() => stream);
                cameraView.SnapShotStream?.Dispose();
                cameraView.SnapShotStream = stream;
            }
            else
                result = ImageSource.FromStream(() => stream);
            bitmap.Dispose();
        }
        snapping = false;
        return result;
    }

    internal bool SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        var result = true;

        if (started && !snapping)
        {
            snapping = true;
            var bitmap = TakeSnap();
            if (bitmap != null)
            {
                if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);
                var iformat = imageFormat switch
                {
                    ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                    _ => Bitmap.CompressFormat.Png
                };
                using FileStream stream = new(SnapFilePath, FileMode.OpenOrCreate);
                if (iformat != null)
                    bitmap.Compress(iformat, 80, stream);
                stream.Close();
            }
            snapping = false;
        }
        else
            result = false;

        return result;
    }
}