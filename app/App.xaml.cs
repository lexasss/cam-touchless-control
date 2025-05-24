using System.Windows;

namespace CameraTouchlessControl;

public partial class App : Application
{
    public HandTrackingService HandTrackingService { get; } = new();
    public CameraService CameraService { get; } = new();

    public App() : base()
    {
        HandTrackingService = new();

        Exit += (s, e) =>
        {
            CameraService.Dispose();
            HandTrackingService?.Dispose();
        };
    }
}
