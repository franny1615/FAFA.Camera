using Android.Hardware.Camera2;

namespace FAFA.Camera.Platforms.Android;

public class PreviewCaptureStateCallback(
    Action<CameraCaptureSession> configured,
    Action<CameraCaptureSession> failure) : CameraCaptureSession.StateCallback
{
    public override void OnConfigured(CameraCaptureSession session)
    {
        configured?.Invoke(session);    
    }
    
    public override void OnConfigureFailed(CameraCaptureSession session)
    {
        failure?.Invoke(session);
    }
}