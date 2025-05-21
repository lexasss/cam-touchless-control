using System.Windows.Input;

namespace HandTracker.Commands;

internal class ToggleCamera
{
    public static RoutedCommand Instance = new();
    public CommandBinding Binding { get; }

    public ToggleCamera(MainViewModel vm)
    {
        _vm = vm;

        Binding = new CommandBinding(
            Instance,
            ToggleCameraCmdExecuted,
            ToggleCameraCmdCanExecute);
    }

    // Internal

    MainViewModel _vm;

    private void ToggleCameraCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        _vm.IsCameraCapturing = !_vm.IsCameraCapturing;
    }

    private void ToggleCameraCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
}
