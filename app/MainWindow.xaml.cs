using Leap;
using System.Windows;

namespace CameraTouchlessControl;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var app = (App)Application.Current;

        _viewModel = new MainViewModel(
            app.HandTrackingService,
            app.CameraService,
            Dispatcher);

        CommandBindings.Add(new Commands.ToggleHandTracker(_viewModel).Binding);
        CommandBindings.Add(new Commands.ToggleCamera(_viewModel).Binding);

        DataContext = _viewModel;
    }

    // Internal

    readonly MainViewModel? _viewModel = null;
}