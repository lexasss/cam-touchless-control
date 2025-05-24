using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class ZoomOut : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public ZoomOut(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.PageDown);

        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.Scale /= 1.1;
}
