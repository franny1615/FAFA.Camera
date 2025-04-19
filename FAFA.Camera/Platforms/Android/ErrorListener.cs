using Android.Media;

namespace FAFA.Camera.Platforms.Android;

public class ErrorListener(Action<MediaRecorder?, MediaRecorderError, int> onerror) : 
    Java.Lang.Object, 
    MediaRecorder.IOnErrorListener
{
    public void OnError(MediaRecorder? mr, MediaRecorderError what, int extra)
    {
        onerror?.Invoke(mr, what, extra);
    }
}