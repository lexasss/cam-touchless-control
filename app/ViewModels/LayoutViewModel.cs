using System.ComponentModel;
using System.Windows;

namespace CameraTouchlessControl;

public class LayoutViewModel : INotifyPropertyChanged
{
    #region Layout

    public int ControlsRow
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ControlsRow)));
        }
    } = 0;

    public int ControlsColumn
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ControlsColumn)));
        }
    } = 1;

    public int CameraControlRow
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraControlRow)));
        }
    } = 0;

    public int CameraControlColumn
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraControlColumn)));
        }
    } = 1;

    public double ControlsMaxWidth
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ControlsMaxWidth)));
        }
    } = 960;

    #endregion

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(Size windowSize)
    {
        var aspect = windowSize.Width / windowSize.Height;
        var newLayoutMode = windowSize.Width > 960 && aspect > 1.78 ? LayoutMode.Wide : LayoutMode.Narrow;

        if (newLayoutMode == _layoutMode)
            return;

        _layoutMode = newLayoutMode;


        ControlsRow = _layoutMode == LayoutMode.Narrow ? 0 : 1;
        ControlsColumn = _layoutMode == LayoutMode.Narrow ? 1 : 0;
        CameraControlRow = _layoutMode == LayoutMode.Narrow ? 0 : 1;
        CameraControlColumn = _layoutMode == LayoutMode.Narrow ? 1 : 0;
        ControlsMaxWidth = _layoutMode == LayoutMode.Narrow ? 960 : 500;
    }

    // Internal

    enum LayoutMode
    {
        Narrow,
        Wide
    }

    LayoutMode _layoutMode = LayoutMode.Narrow;

}
