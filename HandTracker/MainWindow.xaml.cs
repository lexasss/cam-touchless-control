using Leap;
using System.Windows;

namespace HandTracker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var imageSource = new ImageSource();

        if (((App)Application.Current).LeapMotion is LeapMotion lm)
        {
            _viewModel = new MainViewModel(lm, imageSource, Dispatcher);
            CommandBindings.Add(new Commands.ToggleDevice(_viewModel).Binding);
            CommandBindings.Add(new Commands.ToggleCamera(_viewModel).Binding);

            DataContext = _viewModel;
        }

        Application.Current.Exit += (s, e) =>
        {
            imageSource.Dispose();
        };
    }

    // Internal

    readonly MainViewModel? _viewModel = null;
}