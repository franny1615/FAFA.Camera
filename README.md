
# FAFA.Camera

Essentially a minimal fork of [Camera.MAUI](https://github.com/hjam40/Camera.MAUI/tree/master),
focused on CameraView only for iOS and Android.

## CameraView

A ContentView control for camera management with the next properties:

|   | Android  | iOS/Mac |
|---|---|---|
| Preview  |  ✅ | ✅  |
| Mirror preview  | ✅  | ✅  |
| Flash  | ✅  | ✅  |
| Torch  | ✅  | ✅  |
| Zoom  | ✅  | ✅  |
| Take snapshot  | ✅  | ✅  |
| Save snapshot  | ✅  | ✅  |
| Video/audio recording  | ✅  | ✅  |
| Take Photo  | ✅  | ✅  |

### Install and configure CameraView

* Initialize the plugin in your `MauiProgram.cs`:

    ```csharp
    // Add the using to the top
    using Camera.MAUI;
    
    public static MauiApp CreateMauiApp()
    {
    	var builder = MauiApp.CreateBuilder();
    
    	builder
    		.UseMauiApp<App>()
    		.UseCameraView(); // Add the use of the plugin
    
    	return builder.Build();
    }
    ```
1. Add camera/microphone permissions to your application:

#### Android

In your `AndroidManifest.xml` file (Platforms\Android) add the following permission:

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.RECORD_VIDEO" />

```

#### iOS/MacCatalyst

In your `info.plist` file (Platforms\iOS / Platforms\MacCatalyst) add the following permission:

```xml
<key>NSCameraUsageDescription</key>
<string>This app uses camera for...</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app needs access to the microphone for record videos</string>
```
Make sure that you enter a clear and valid reason for your app to access the camera. This description will be shown to the user.

### Using CameraView

In XAML, make sure to add the right XML namespace:

`xmlns:camera="clr-namespace:FAFA.Camera;assembly=FAFA.Camera"`

Use the control:
```xaml
<cv:CameraView x:Name="cameraView" WidthRequest="300" HeightRequest="200"/>
```

Configure the events:
```csharp
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
        cameraView.BarcodeDetected += CameraView_BarcodeDetected;
```
Configure the camera and microphone to use:
```csharp
    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        if (cameraView.NumCamerasDetected > 0)
        {
            if (cameraView.NumMicrophonesDetected > 0)
                cameraView.Microphone = cameraView.Microphones.First();
            cameraView.Camera = cameraView.Cameras.First();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (await cameraView.StartCameraAsync() == CameraResult.Success)
                {
                    controlButton.Text = "Stop";
                    playing = true;
                }
            });
        }
    }
```
CameraInfo type (Camera Property):
CameraInfo has the next properties:
```csharp
    public string Name
    public string DeviceId
    public CameraPosition Position
    public bool HasFlashUnit
    public float MinZoomFactor
    public float MaxZoomFactor
    public float HorizontalViewAngle
    public float VerticalViewAngle
    public List<Size> AvailableResolutions
```
Start camera playback:
```csharp
         if (await cameraView.StartCameraAsync(new Size(1280, 720)) == CameraResult.Success)
         {
             playing = true;
         }
```
Stop camera playback:
```csharp
         if (await cameraView.StopCameraAsync() == CameraResult.Success)
         {
             playing = false;
         }
```
Set Flash mode
```csharp
cameraView.FlashMode = FlashMode.Auto;
```
Toggle Torch
```csharp
cameraView.TorchEnabled = !cameraView.TorchEnabled;
```
Set mirrored mode
```csharp
cameraView.MirroredImage = true;
```
Set zoom factor
```csharp
if (cameraView.MaxZoomFactor >= 2.5f)
    cameraView.ZoomFactor = 2.5f;
```
Get a snapshot from the playback
```csharp
ImageSource imageSource = cameraView.GetSnapShot(ImageFormat.PNG);
bool result = cameraView.SaveSnapShot(ImageFormat.PNG, filePath);
```
Record a video:
```csharp
var result = await cameraView.StartRecordingAsync(Path.Combine(FileSystem.Current.CacheDirectory, "Video.mp4"), new Size(1920, 1080));
....
result = await cameraView.StopRecordingAsync();
```
Take a photo
```csharp
var stream = await cameraView.TakePhotoAsync();
if (stream != null)
{
    var result = ImageSource.FromStream(() => stream);
    snapPreview.Source = result;
}
```