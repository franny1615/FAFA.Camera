namespace FAFA.Camera.Models;

public record BarcodeEventArgs
{
    public BarcodeResult[] Result { get; init; } = [];
}