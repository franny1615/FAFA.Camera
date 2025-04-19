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
            StopCamera();
            
            recording = true;
            recordingFilePath = file;
            recordingVideoSize = Resolution;

            if (File.Exists(file)) File.Delete(file);

            await StartCameraAsync(cameraView.PhotosResolution);
            
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
        
        camChars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
        
        mediaRecorder = OperatingSystem.IsAndroidVersionAtLeast(31) ? 
            new MediaRecorder(context) : 
            new MediaRecorder();
        mediaRecorder.SetOnErrorListener(new ErrorListener((_, err, _) =>
        {
            System.Diagnostics.Debug.WriteLine(err);
        }));
        
        audioManager.Mode = Mode.Normal;
        
        var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var rotation = (int)(windowManager?.DefaultDisplay?.Rotation ?? SurfaceOrientation.Rotation90);
        var orientation = cameraView.Camera.Position == CameraPosition.Back ? 
            ORIENTATIONS.Get(rotation) : 
            ORIENTATIONSFRONT.Get(rotation);
        mediaRecorder.SetOrientationHint(orientation);
        
        mediaRecorder.SetAudioSource(AudioSource.Mic);
        mediaRecorder.SetVideoSource(VideoSource.Surface);
        mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
        mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
        mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            mediaRecorder.SetOutputFile(new Java.IO.File(file));
        }
        else
        {
            mediaRecorder.SetOutputFile(file);
        }
        var choices = GetVideoSizeChoices();
        var maxVideoSize = ChooseMaxVideoSize(choices);
        if (Resolution.Width != 0 && Resolution.Height != 0)
            maxVideoSize = new((int)Resolution.Width, (int)Resolution.Height);
        mediaRecorder.SetVideoSize(maxVideoSize.Width, maxVideoSize.Height);
        mediaRecorder.SetVideoFrameRate(30);
        mediaRecorder.SetVideoEncodingBitRate(10000000);
        mediaRecorder.SetAudioEncodingBitRate(16*44100);
        mediaRecorder.SetAudioSamplingRate(44100);
        
        mediaRecorder.Prepare();
    }
}