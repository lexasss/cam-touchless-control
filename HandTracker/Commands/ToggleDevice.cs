using System.Windows.Input;

namespace HandTracker.Commands;

internal class ToggleDevice
{
    public static RoutedCommand Instance = new();
    public CommandBinding Binding { get; }

    public ToggleDevice(MainViewModel vm)
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
        _vm.IsRunning = !_vm.IsRunning;
    }

    private void ToggleDeviceCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
}
