using System.ComponentModel;
using System.Windows;

namespace CameraTouchlessControl;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainViewModel ViewModel
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();

        var app = (App)Application.Current;

        ViewModel = new MainViewModel(
            app.HandTrackingService,
            app.CameraService,
            app.ZoomPanService,
            Dispatcher);

        ViewModel.ZoomPan.RequestViewportSize += (s, e) => e.ViewportSize =
            new Size(cnvViewportOverlay.ActualWidth, cnvViewportOverlay.ActualHeight);

        Commands.MainViewCommand[] commands = [
            new Commands.ToggleHandTracker(ViewModel),
            new Commands.ToggleCamera(ViewModel),
            new Commands.ZoomIn(ViewModel),
            new Commands.ZoomOut(ViewModel),
            new Commands.PanLeft(ViewModel),
            new Commands.PanRight(ViewModel),
            new Commands.PanUp(ViewModel),
            new Commands.PaneDown(ViewModel),
        ];

        foreach (var command in commands)
        {
            CommandBindings.Add(command.Binding);
            if (command.KeyBinding != null)
            {
                InputBindings.Add(command.KeyBinding);
            }
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel?.Layout.Update(e.NewSize);
    }
}