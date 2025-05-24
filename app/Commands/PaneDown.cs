using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class PaneDown : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public PaneDown(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.Down);

        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.OffsetY += 20;
}