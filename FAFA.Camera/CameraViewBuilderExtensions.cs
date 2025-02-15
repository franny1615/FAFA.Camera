namespace FAFA.Camera;

public static class CameraViewBuilderExtensions
{
    public static MauiAppBuilder UseCameraView(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<CameraView, CameraViewHandler>();
        });
        
        return builder;
    }
}