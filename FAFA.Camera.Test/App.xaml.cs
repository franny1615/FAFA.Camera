﻿namespace FAFA.Camera.Test;

public partial class App : Application
{
    public static string VideoPreviewPath = string.Empty;
    
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}