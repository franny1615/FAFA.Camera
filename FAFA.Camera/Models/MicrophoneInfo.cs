namespace FAFA.Camera.Models;

public class MicrophoneInfo
{
    public string Name { get; internal set; } = string.Empty;
    public string DeviceId { get; internal set; } = string.Empty;
    public override string ToString()
    {
        return Name;
    }
}