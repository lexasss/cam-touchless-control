using System.Windows.Input;

namespace HandTracker.Commands;

internal class ToggleHandTracker
{
    public static RoutedCommand Instance = new();
    public CommandBinding Binding { get; }

    public ToggleHandTracker(MainViewModel vm)
    {
        _vm = vm;

        Binding = new CommandBinding(
            Instance,
            ToggleDeviceCmdExecuted,
            ToggleDeviceCmdCanExecute);
    }

    // Internal

    MainViewModel _vm;

    private void ToggleDeviceCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        _vm.IsHandTrackingRunning = !_vm.IsHandTrackingRunning;
    }

    private void ToggleDeviceCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
}
