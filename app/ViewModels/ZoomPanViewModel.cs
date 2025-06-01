using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace CameraTouchlessControl;

public class ZoomPanViewModel : INotifyPropertyChanged
{
    public double Scale
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Scale)));
        }
    } = 1.0;

    public double OffsetX
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OffsetX)));
        }
    } = 0;

    public double OffsetY
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OffsetY)));
        }
    } = 0;

    public double CursorX
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CursorX)));
        }
    } = -1e8;

    public double CursorY
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CursorY)));
        }
    } = -1e8;

    public double CursorSize
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CursorSize)));
        }
    } = 86;

    public Brush CursorBrush
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CursorBrush)));
        }
    } = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));

    public event PropertyChangedEventHandler? PropertyChanged;

    public class RequestViewportSizeEventArgs : EventArgs
    {
        public Size ViewportSize { get; set; } = default;
    }

    public event EventHandler<RequestViewportSizeEventArgs>? RequestViewportSize;

    public ZoomPanViewModel(ZoomPanService zoomPanService)
    {
        _zoomPanService = zoomPanService;

        _zoomPanService.ScaleChanged += ZoomPanService_ScaleChanged;
        _zoomPanService.OffsetChanged += ZoomPanService_OffsetChanged;
        _zoomPanService.HandCursorMoved += ZoomPanService_HandCursorMoved;
        _zoomPanService.HandStateChanged += ZoomPanService_HandStateChanged;
    }

    // Internal

    const float HAND_CUSOR_MOVEMENT_SCALE = 10;
    const float HAND_CUSOR_SIZE_SCALE = 3;

    readonly Brush StillCursorBrush = new SolidColorBrush(Color.FromArgb(96, 255, 255, 255));
    readonly Brush AdjustingCursorBrush = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
    readonly Brush MovingCursorBrush = new SolidColorBrush(Color.FromArgb(214, 255, 128, 0));

    readonly ZoomPanService _zoomPanService;

    private void ZoomPanService_OffsetChanged(object? sender, System.Windows.Point e)
    {
        OffsetX = e.X;
        OffsetY = e.Y;
    }

    private void ZoomPanService_ScaleChanged(object? sender, double e)
    {
        Scale = e;
    }

    private void ZoomPanService_HandStateChanged(object? sender, HandState e)
    {
        if (e == HandState.Invisible)
        {
            CursorX = -1e8;
            CursorY = -1e8;
        }
        else
        {
            CursorBrush = e switch
            {
                HandState.Still => StillCursorBrush,
                HandState.Adjusting => AdjustingCursorBrush,
                HandState.Moving => MovingCursorBrush,
                _ => CursorBrush
            };
        }
    }

    private void ZoomPanService_HandCursorMoved(object? sender, System.Numerics.Vector3 e)
    {
        var request = new RequestViewportSizeEventArgs();
        RequestViewportSize?.Invoke(this, request);

        CursorSize = Math.Max(16, 86 - e.Y * HAND_CUSOR_SIZE_SCALE);
        CursorX = request.ViewportSize.Width / 2 + e.X * HAND_CUSOR_MOVEMENT_SCALE - CursorSize / 2;
        CursorY = request.ViewportSize.Height / 2 + e.Z * HAND_CUSOR_MOVEMENT_SCALE - CursorSize / 2;
    }
}
