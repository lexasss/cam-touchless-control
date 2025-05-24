using CameraTouchlessControl.Services;
using System.Windows;

namespace CameraTouchlessControl;

public partial class App : Application
{
    public HandTrackingService HandTrackingService { get; } = new();
    public CameraService CameraService { get; } = new();
    public ZoomPanService ZoomPanService { get; } = new();

    public App() : base()
    {
        Exit += (s, e) =>
        {
            CameraService.Dispose();
            HandTrackingService.Dispose();
        };
    }
}
