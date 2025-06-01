using System.Windows.Threading;

namespace CameraTouchlessControl;

public class MainViewModel
{
    public HandTrackerViewModel HandTracker { get; }
    public CameraViewModel Camera { get; }
    public ZoomPanViewModel ZoomPan { get; }
    public LayoutViewModel Layout { get; } = new();

    public MainViewModel(
        HandTrackingService handTrackingService,
        CameraService cameraService,
        ZoomPanService zoomPanService,
        Dispatcher dispatcher)
    {
        _zoomPanService = zoomPanService;
        _dispatcher = dispatcher;

        HandTracker = new(handTrackingService, dispatcher);
        Camera = new(cameraService, dispatcher);
        ZoomPan = new(zoomPanService);

        handTrackingService.HandData += HandTrackingService_HandData;
    }

    // Internal

    readonly ZoomPanService _zoomPanService;
    readonly Dispatcher _dispatcher;

    private void HandTrackingService_HandData(object? sender, HandLocation e)
    {
        if (!HandTracker.IsHandTrackingRunning)
            return;

        _dispatcher.Invoke(() =>
        {
            _zoomPanService.Feed(e);
        });
    }
}
