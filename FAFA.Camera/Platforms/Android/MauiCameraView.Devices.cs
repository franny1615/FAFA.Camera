using Android.Content;
using Java.Util.Concurrent;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Android.Hardware.Camera2;
using Android.Media;
using SizeF = Android.Util.SizeF;
using FAFA.Camera.Enums;
using FAFA.Camera.Models;

namespace FAFA.Camera.Platforms.Android;

public partial class MauiCameraView
{
    private void InitDevices()
    {
        if (initiated) return;
        
        cameraManager = (CameraManager?)context.GetSystemService(Context.CameraService);
        audioManager = (AudioManager?)context.GetSystemService(Context.AudioService);
        cameraView.Cameras.Clear();

        if (cameraManager == null || audioManager == null) 
            return;
        
        foreach (var id in cameraManager.GetCameraIdList())
        {
            var cameraInfo = new CameraInfo { DeviceId = id, MinZoomFactor = 1 };
            var chars = cameraManager.GetCameraCharacteristics(id);
            if ((int)(chars.Get(CameraCharacteristics.LensFacing) ?? 0) == (int)LensFacing.Back)
            {
                cameraInfo.Name = "Back Camera";
                cameraInfo.Position = CameraPosition.Back;
            }
            else if ((int)(chars.Get(CameraCharacteristics.LensFacing) ?? 0) == (int)LensFacing.Front)
            {
                cameraInfo.Name = "Front Camera";
                cameraInfo.Position = CameraPosition.Front;
            }
            else
            {
                cameraInfo.Name = "Camera " + id;
                cameraInfo.Position = CameraPosition.Unknow;
            }
            cameraInfo.MaxZoomFactor = (float)(chars.Get(CameraCharacteristics.ScalerAvailableMaxDigitalZoom) ?? 0f);
            cameraInfo.HasFlashUnit = (bool)(chars.Get(CameraCharacteristics.FlashInfoAvailable) ?? 0f);
            cameraInfo.AvailableResolutions = [];
            
            try
            {
                var maxFocus = (float[]?)chars.Get(CameraCharacteristics.LensInfoAvailableFocalLengths);
                var size = (SizeF?)chars.Get(CameraCharacteristics.SensorInfoPhysicalSize);
                if (maxFocus != null && size != null)
                {
                    cameraInfo.HorizontalViewAngle = (float)(2 * Math.Atan(size.Width / (maxFocus[0] * 2)));
                    cameraInfo.VerticalViewAngle = (float)(2 * Math.Atan(size.Height / (maxFocus[0] * 2)));    
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                foreach (var s in GetVideoSizeChoices())
                    cameraInfo.AvailableResolutions.Add(new Size(s.Width, s.Height));
            }
            catch
            {
                if (cameraInfo.Position == CameraPosition.Back)
                    cameraInfo.AvailableResolutions.Add(new Size(1920, 1080));
                cameraInfo.AvailableResolutions.Add(new Size(1280, 720));
                cameraInfo.AvailableResolutions.Add(new Size(640, 480));
                cameraInfo.AvailableResolutions.Add(new Size(352, 288));
            }
            cameraView.Cameras.Add(cameraInfo);
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            cameraView.Microphones.Clear();
            foreach (var device in audioManager.Microphones ?? [])
            {
                cameraView.Microphones.Add(new Models.MicrophoneInfo { Name = "Microphone " + device.Type.ToString() + " " + device.Address, DeviceId = device.Id.ToString() });
            }
        }
        
        executorService = Executors.NewSingleThreadExecutor();

        initiated = true;
        cameraView.RefreshDevices();
    }
}