using Leap;
using System.Windows;

namespace CameraTouchlessControl;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var cameraService = new CameraService();

        if (((App)Application.Current).LeapMotion is LeapMotion handTrackingService)
        {
            _viewModel = new MainViewModel(handTrackingService, cameraService, Dispatcher);
            CommandBindings.Add(new Commands.ToggleHandTracker(_viewModel).Binding);
            CommandBindings.Add(new Commands.ToggleCamera(_viewModel).Binding);

            DataContext = _viewModel;
        }

        Application.Current.Exit += (s, e) =>
        {
            cameraService.Dispose();
        };
    }

    // Internal

    readonly MainViewModel? _viewModel = null;
}