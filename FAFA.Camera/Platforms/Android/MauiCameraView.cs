using Android.Content;
using Android.Widget;
using Java.Util.Concurrent;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.Views;
using Android.Util;
using Size = Android.Util.Size;
using Rect = Android.Graphics.Rect;
using Android.OS;
using Android.Content.Res;
using FAFA.Camera.Enums;
using Timer = System.Timers.Timer;

namespace FAFA.Camera.Platforms.Android;

public partial class MauiCameraView : GridLayout
{
    private readonly CameraView cameraView;
    private IExecutorService? executorService;
    private bool started;
    private int frames;
    private bool initiated;
    private bool snapping;
    private bool recording;
    private readonly Context context;

    private readonly TextureView? textureView;
    public CameraCaptureSession? previewSession;
    public MediaRecorder? mediaRecorder;
    private CaptureRequest.Builder? previewBuilder;
    private CameraDevice? cameraDevice;
    private readonly MyCameraStateCallback stateListener;
    private Size? videoSize;
    private CameraManager? cameraManager;
    private AudioManager? audioManager;
    private readonly Timer? timer;
    private readonly SparseIntArray ORIENTATIONS = new();
    private readonly SparseIntArray ORIENTATIONSFRONT = new();
    private CameraCharacteristics? camChars;
    private PreviewCaptureStateCallback sessionCallback;
    private byte[] capturePhoto = [];
    private bool captureDone;
    private readonly ImageAvailableListener? photoListener;
    private HandlerThread? backgroundThread;
    private Handler? backgroundHandler;
    private ImageReader? imgReader;
    
    // video recording
    private string recordingFilePath = string.Empty;
    private Microsoft.Maui.Graphics.Size recordingVideoSize = new(0,0);

    public MauiCameraView(Context context, CameraView cameraView) : base(context)
    {
        this.context = context;
        this.cameraView = cameraView;

        textureView = new TextureView(context);
        timer = new Timer(33.3);
        timer.Elapsed += Timer_Elapsed;
        stateListener = new MyCameraStateCallback(
            disconnected: (camera) =>
            {
                camera?.Close();
                cameraDevice = null;
            },
            opened: (camera) =>
            {
                if (camera == null)
                    return;
                
                cameraDevice =  camera;
                StartPreview();
            },
            err: (camera, error) =>
            {
                camera?.Close();
                cameraDevice = null;
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine(nameof(MyCameraStateCallback) + " error >>> " + error);
#endif
            });
        sessionCallback = new PreviewCaptureStateCallback(
            configured: (session) =>
            {
                previewSession = session;
                UpdatePreview();
            },
            failure: (session) =>
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(nameof(PreviewCaptureStateCallback) + " error");
#endif
            });
        photoListener = new ImageAvailableListener(this);
        
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);
        ORIENTATIONSFRONT.Append((int)SurfaceOrientation.Rotation0, 270);
        ORIENTATIONSFRONT.Append((int)SurfaceOrientation.Rotation90, 0);
        ORIENTATIONSFRONT.Append((int)SurfaceOrientation.Rotation180, 90);
        ORIENTATIONSFRONT.Append((int)SurfaceOrientation.Rotation270, 180);
        InitDevices();
        AddView(textureView);
    }
    
    internal async Task<CameraResult> StartCameraAsync(Microsoft.Maui.Graphics.Size PhotosResolution)
    {
        if (!initiated)
            return CameraResult.NotInitiated;

        var hasPermissions = await CameraView.RequestPermissions();
        if (!hasPermissions)
            return CameraResult.AccessDenied;
        
        if (started) StopCamera();
        if (cameraView.Camera == null || executorService == null)
            return CameraResult.NoCameraSelected;
        
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
                cameraManager?.OpenCamera(cameraView.Camera.DeviceId, executorService, stateListener);
            else
                cameraManager?.OpenCamera(cameraView.Camera.DeviceId, stateListener, backgroundHandler);
            timer?.Start();

            started = true;
            
            return CameraResult.Success;
        }
        catch
        {
            return CameraResult.AccessError;
        }
    }
    
    internal Task<CameraResult> StopRecordingAsync()
    {
        recording = false;
        return StartCameraAsync(cameraView.PhotosResolution);
    }

    internal CameraResult StopCamera()
    {
        var result = CameraResult.Success;
        
        if (initiated)
        {
            timer?.Stop();
            try
            {
                mediaRecorder?.Stop();
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex);
#endif
            }
            finally
            {
                mediaRecorder?.Release();
            }

            try
            {
                backgroundThread?.QuitSafely();
                backgroundThread?.Join();
                backgroundThread = null;
                backgroundHandler = null;
                imgReader?.Dispose();
                imgReader = null;
            }
            catch
            {
                // ignored
            }

            try
            {
                previewSession?.StopRepeating();
                previewSession?.AbortCaptures();
                previewSession?.Dispose();
            }
            catch
            {
                // ignored
            }

            try
            {
                cameraDevice?.Close();
                cameraDevice?.Dispose();
            }
            catch
            {
                // ignored
            }

            previewSession = null;
            cameraDevice = null;
            previewBuilder = null;
            mediaRecorder = null;
            started = false;
            recording = false;
        }
        else
            result = CameraResult.NotInitiated;
        return result;
    }
    internal void DisposeControl()
    {
        try
        {
            if (started) StopCamera();
            executorService?.Shutdown();
            executorService?.Dispose();
            RemoveAllViews();
            textureView?.Dispose();
            timer?.Dispose();
            Dispose();
        }
        catch
        {
            // ignored
        }
    }
    
    #region PROCESS QR
    
    private void ProccessQR()
    {
        Task.Run(() =>
        {
            var bitmap = TakeSnap();
            if (bitmap != null)
            {
                System.Diagnostics.Debug.WriteLine($"Processing QR ({bitmap.Width}x{bitmap.Height}) " + DateTime.Now.ToString("mm:ss:fff"));
                cameraView.DecodeBarcode(bitmap);
                bitmap.Dispose();
                System.Diagnostics.Debug.WriteLine("QR Processed " + DateTime.Now.ToString("mm:ss:fff"));
            }
            lock (cameraView.currentThreadsLocker) cameraView.currentThreads--;
        });
    }
    
    #endregion
    
    #region MIRROR
    
    public void UpdateMirroredImage()
    {
        if (textureView == null) return;
        
        if (cameraView.MirroredImage) 
            textureView.ScaleX = -1;
        else
            textureView.ScaleX = 1;
    }
    
    #endregion
    
    #region TORCH
    
    internal void UpdateTorch()
    {
        if (cameraView.Camera is not { HasFlashUnit: true } || 
            cameraDevice == null || 
            previewBuilder == null || 
            previewSession == null ||
            cameraManager == null) return;
        
        if (started)
        {
            if (CaptureRequest.ControlAeMode is not null)
                previewBuilder.Set(CaptureRequest.ControlAeMode, Java.Lang.Integer.ValueOf((int)ControlAEMode.On));
            if (CaptureRequest.FlashMode is not null)
                previewBuilder.Set(CaptureRequest.FlashMode, 
                    cameraView.TorchEnabled ? (int)ControlAEMode.OnAutoFlash : Java.Lang.Integer.ValueOf((int)ControlAEMode.Off));
            previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
        }
        else if (initiated)
        {
#pragma warning disable CA1416
            cameraManager.SetTorchMode(cameraView.Camera.DeviceId, cameraView.TorchEnabled);
#pragma warning restore CA1416            
        }
    }
    
    #endregion
    
    #region FLASH MODE
    
    internal void UpdateFlashMode()
    {
        if (previewSession == null || 
            previewBuilder == null) return;
        
        try
        {
            if (!cameraView.Camera.HasFlashUnit) return;
            
            switch (cameraView.FlashMode)
            {
                case Enums.FlashMode.Auto:
                    if (CaptureRequest.ControlAeMode is not null)
                        previewBuilder.Set(CaptureRequest.ControlAeMode, Java.Lang.Integer.ValueOf((int)ControlAEMode.OnAutoFlash));
                    previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
                    break;
                case Enums.FlashMode.Enabled:
                    if (CaptureRequest.ControlAeMode is not null)
                        previewBuilder.Set(CaptureRequest.ControlAeMode, Java.Lang.Integer.ValueOf((int)ControlAEMode.On));
                    previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
                    break;
                case Enums.FlashMode.Disabled:
                    if (CaptureRequest.ControlAeMode is not null)
                        previewBuilder.Set(CaptureRequest.ControlAeMode, Java.Lang.Integer.ValueOf((int)ControlAEMode.Off));
                    previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
                    break;
            }
        }
        catch
        {
            // ignored
        }
    }
    
    #endregion
    
    #region ZOOM FACTOR
    
    internal void SetZoomFactor(float zoom)
    {
        if (previewSession == null || 
            previewBuilder == null ||
            camChars == null) return;
        
        var destZoom = Math.Clamp(zoom, 1, Math.Min(6, cameraView.Camera.MaxZoomFactor)) - 1;
        var m = (Rect?)camChars.Get(CameraCharacteristics.SensorInfoActiveArraySize);

        if (m == null)
            return;
        
        var minW = (int)(m.Width() / (cameraView.Camera.MaxZoomFactor));
        var minH = (int)(m.Height() / (cameraView.Camera.MaxZoomFactor));
        var newWidth = (int)(m.Width() - (minW * destZoom));
        var newHeight = (int)(m.Height() - (minH * destZoom));
        Rect zoomArea = new((m.Width()-newWidth)/2, (m.Height()-newHeight)/2, newWidth, newHeight);
        if (CaptureRequest.ScalerCropRegion is not null)
            previewBuilder.Set(CaptureRequest.ScalerCropRegion, zoomArea);
        previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
    }
    
    #endregion
    
    #region FORCE AUTO FOCUS
    
    internal void ForceAutoFocus()
    {
        if (previewSession == null || 
            previewBuilder == null) return;
        if (CaptureRequest.ControlAfMode is not null)
            previewBuilder.Set(CaptureRequest.ControlAfMode, Java.Lang.Integer.ValueOf((int)ControlAFMode.Off));
        if (CaptureRequest.ControlAfTrigger is not null)
            previewBuilder.Set(CaptureRequest.ControlAfTrigger, Java.Lang.Integer.ValueOf((int)ControlAFTrigger.Cancel));
        previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
        if (CaptureRequest.ControlAfMode is not null)
            previewBuilder.Set(CaptureRequest.ControlAfMode, Java.Lang.Integer.ValueOf((int)ControlAFMode.Auto));
        if (CaptureRequest.ControlAfTrigger is not null)
            previewBuilder.Set(CaptureRequest.ControlAfTrigger, Java.Lang.Integer.ValueOf((int)ControlAFTrigger.Start));
        previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
    }
    
    #endregion
    
    protected override async void OnConfigurationChanged(Configuration? newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        if (started && !recording)
            await StartCameraAsync(cameraView.PhotosResolution);
    }
}