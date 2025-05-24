using System.Windows;

namespace CameraTouchlessControl.Services;

internal class ZoomPanService
{
    public event EventHandler<double>? ScaleChanged;
    public event EventHandler<Point>? OffsetChanged;

    public void Feed(HandLocation handLocation)
    {

    }
}
