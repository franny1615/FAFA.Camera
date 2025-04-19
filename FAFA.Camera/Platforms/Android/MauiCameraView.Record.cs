using Android.Content;
using Android.Hardware.Camera2;
using Android.Media;
using Android.Views;
using Android.Hardware.Camera2.Params;
using Android.Runtime;
using FAFA.Camera.Enums;

namespace FAFA.Camera.Platforms.Android;

public partial class MauiCameraView
{
    internal async Task<CameraResult> StartRecordingAsync(string file, Size Resolution)
    {
        var havePermissions = await CameraView.RequestPermissions(true, true);
        if (!havePermissions)
            return CameraResult.AccessDenied;

        if (cameraView?.Camera == null)
            return CameraResult.NoCameraSelected;

        if (!initiated || recording)
            return CameraResult.NotInitiated;
        
        try
        {
            recording = true;

            if (File.Exists(file)) File.Delete(file);
            
            SetupMediaRecorder(file, Resolution);
            StartRecording();
            
            started = true;

            return CameraResult.Success;
        }
        catch (Exception ex)
        {
            #if DEBUG 
            
            System.Diagnostics.Debug.WriteLine(ex);
            
            #endif
            
            return CameraResult.AccessError;
        }
    }

    private void SetupMediaRecorder(string file, Size Resolution)
    {
        if (audioManager is null || cameraManager is null)
            return;
        
        mediaRecorder = OperatingSystem.IsAndroidVersionAtLeast(31) ? 
            new MediaRecorder(context) : 
            new MediaRecorder();
        audioManager.Mode = Mode.Normal;
        mediaRecorder.SetAudioSource(AudioSource.Mic);
        mediaRecorder.SetVideoSource(VideoSource.Surface);
        mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
        mediaRecorder.SetOutputFile(file);
        mediaRecorder.SetVideoEncodingBitRate(10000000);
        mediaRecorder.SetVideoFrameRate(30);

        camChars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
        
        var choices = GetVideoSizeChoices();
        videoSize = ChooseVideoSize(choices);
        var maxVideoSize = ChooseMaxVideoSize(choices);
        if (Resolution.Width != 0 && Resolution.Height != 0)
            maxVideoSize = new((int)Resolution.Width, (int)Resolution.Height);
        mediaRecorder.SetVideoSize(maxVideoSize.Width, maxVideoSize.Height);

        mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
        mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
        var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var rotation = (int)(windowManager?.DefaultDisplay?.Rotation ?? SurfaceOrientation.Rotation90);
        var orientation = cameraView.Camera.Position == CameraPosition.Back ? 
            ORIENTATIONS.Get(rotation) : 
            ORIENTATIONSFRONT.Get(rotation);
        mediaRecorder.SetOrientationHint(orientation);
        mediaRecorder.Prepare();
    }

    private void StartRecording()
    {
        if (videoSize == null)
            return;
        
        while (textureView?.SurfaceTexture == null || 
               !textureView.IsAvailable || 
               cameraDevice == null) Thread.Sleep(100);
        
        var texture = textureView.SurfaceTexture;
        texture.SetDefaultBufferSize(videoSize.Width, videoSize.Height);

        previewBuilder = cameraDevice.CreateCaptureRequest(recording ? CameraTemplate.Record : CameraTemplate.Preview);
        var previewSurface = new Surface(texture);
        previewBuilder.AddTarget(previewSurface);

        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            List<OutputConfiguration> surfaces = 
            [ 
                new(previewSurface) 
            ];
            
            if (mediaRecorder is { Surface: not null })
            {
                surfaces.Add(new OutputConfiguration(mediaRecorder.Surface));
                previewBuilder.AddTarget(mediaRecorder.Surface);
            }

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
            
            if (mediaRecorder is { Surface: not null })
            {
                surfaces.Add(mediaRecorder.Surface);
                previewBuilder.AddTarget(mediaRecorder.Surface);
            }
            
            cameraDevice.CreateCaptureSession(surfaces, sessionCallback, null);
        }
    }
}