using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class PanRight : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public PanRight(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.Right);

        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.OffsetX += ZoomPanConfig.Instance.PanGain;
}