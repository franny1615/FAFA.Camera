using Android.App;
using Android.Content.PM;
using Android.OS;

namespace FAFA.Camera.Test;

[Activity(
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true, 
    LaunchMode = LaunchMode.SingleTop,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}