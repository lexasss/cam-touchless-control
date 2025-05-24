using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class ToggleCamera : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public ToggleCamera(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.C, ModifierKeys.Control);
        
        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.IsCameraCapturing = !_vm.IsCameraCapturing;
}
