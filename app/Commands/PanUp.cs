using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class PanUp : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public PanUp(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.Up);

        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.OffsetY -= 20;
}