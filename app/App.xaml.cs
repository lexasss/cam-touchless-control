using Leap;
using System.Windows;
using System.Windows.Input;

namespace CameraTouchlessControl;

public partial class App : Application
{
    public LeapMotion? HandTrackingService { get; }
    public CameraService CameraService { get; } = new();

    public App() : base()
    {
        try
        {
            HandTrackingService = new();

            Exit += (s, e) =>
            {
                CameraService.Dispose();
                HandTrackingService?.Dispose();
            };
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }
    }
}
