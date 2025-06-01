using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class PanLeft : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public PanLeft(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.Left);

        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.ZoomPan.OffsetX -= ZoomPanConfig.Instance.PanGain;
}