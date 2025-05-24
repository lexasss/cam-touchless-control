using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class ToggleHandTracker : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public ToggleHandTracker(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.T, ModifierKeys.Control);

        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.IsHandTrackingRunning = !_vm.IsHandTrackingRunning;
}
