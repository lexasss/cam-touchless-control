using System.ComponentModel;
using System.Windows;

namespace CameraTouchlessControl;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainViewModel? ViewModel
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
            Dispatcher);

        CommandBindings.Add(new Commands.ToggleHandTracker(ViewModel).Binding);
        CommandBindings.Add(new Commands.ToggleCamera(ViewModel).Binding);
    }
}