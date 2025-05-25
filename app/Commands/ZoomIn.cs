using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal class ZoomIn : MainViewCommand
{
    public static RoutedCommand Instance = new();

    public ZoomIn(MainViewModel vm) : base(vm, Instance)
    {
        var keyGesture = new KeyGesture(Key.PageUp);

        KeyBinding = new KeyBinding(
            Instance,
            keyGesture);
    }

    protected override void Execute(object? parameter) =>
        _vm.Scale *= ZoomPanConfig.Instance.ZoomGain;
}
