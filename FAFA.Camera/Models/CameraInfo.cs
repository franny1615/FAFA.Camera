using FAFA.Camera.Enums;

namespace FAFA.Camera.Models;

public class CameraInfo
{
    public string Name { get; internal set; } = string.Empty;
    public string DeviceId { get; internal set; } = string.Empty;
    public CameraPosition Position { get; internal set; }
    public bool HasFlashUnit { get; internal set; }
    public float MinZoomFactor { get; internal set; }
    public float MaxZoomFactor { get; internal set; }
    public float HorizontalViewAngle { get; internal set; }
    public float VerticalViewAngle { get; internal set; }

    public List<Size> AvailableResolutions { get; internal set; } = [];
    public override string ToString()
    {
        return Name;
    }
}