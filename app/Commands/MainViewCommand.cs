using System.Windows.Input;

namespace CameraTouchlessControl.Commands;

internal abstract class MainViewCommand
{
    public CommandBinding Binding { get; }
    public KeyBinding? KeyBinding { get; protected set; } = null;

    public MainViewCommand(MainViewModel vm, RoutedCommand cmd)
    {
        _vm = vm;

        Binding = new CommandBinding(
            cmd,
            (s, e) => Execute(e.Parameter),
            (s, e) => e.CanExecute = CanExecute(e.Parameter));
    }

    // Internal

    protected readonly MainViewModel _vm;

    protected virtual bool CanExecute(object? parameter) => true;

    protected abstract void Execute(object? parameter);
}
