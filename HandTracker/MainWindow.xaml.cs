using Leap;
using System.Windows;

namespace HandTracker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (((App)Application.Current).LeapMotion is LeapMotion lm)
        {
            _viewModel = new MainViewModel(lm, Dispatcher);
            CommandBindings.Add(new Commands.ToggleDevice(_viewModel).Binding);

            DataContext = _viewModel;
        }
    }

    // Internal

    readonly MainViewModel? _viewModel = null;
}