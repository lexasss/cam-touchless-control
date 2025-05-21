using Leap;
using System.Windows;

namespace CameraTouchlessControl;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        try
        { 
            var handTrackingService = new LeapMotion();
            var cameraService = new CameraService();

            _viewModel = new MainViewModel(handTrackingService, cameraService, Dispatcher);
            CommandBindings.Add(new Commands.ToggleHandTracker(_viewModel).Binding);
            CommandBindings.Add(new Commands.ToggleCamera(_viewModel).Binding);

            DataContext = _viewModel;

            Application.Current.Exit += (s, e) =>
            {
                cameraService.Dispose();
                handTrackingService?.Dispose();
            };
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to connect to LeapMotion: {e.Message}");
        }
    }

    // Internal

    readonly MainViewModel? _viewModel = null;
}