using Android.Hardware.Camera2;

namespace FAFA.Camera.Platforms.Android;

public class MyCameraStateCallback(
    Action<CameraDevice?> disconnected,
    Action<CameraDevice?> opened,
    Action<CameraDevice?, CameraError> err) : CameraDevice.StateCallback
{
    public override void OnOpened(CameraDevice? camera)
    {
        opened?.Invoke(camera);
    }

    public override void OnDisconnected(CameraDevice? camera)
    {
        disconnected?.Invoke(camera);
    }

    public override void OnError(CameraDevice? camera, CameraError error)
    {
        err?.Invoke(camera, error);
    }
}