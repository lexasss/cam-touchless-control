using OpenCvSharp;
using Camera = CameraTouchlessControl.UsbDevice;

namespace CameraTouchlessControl;

/// <summary>
/// CameraService:
///     1.  manages the list of connected cameras,
///     2.  fires events when a camera is connected or disconnected,
///     2.  opens / closes a camera,
///     3.  fires an event when a frame is received 
/// 
/// Usage:
///
/// var cameraService = new CameraService();
/// cameraService.CameraAdded += CameraService_CameraAdded;
/// cameraService.CameraRemoved += CameraService_CameraRemoved;
/// cameraService.Frame += CameraService_FrameReceived;
///
/// foreach (var camera in _cameraService.Cameras)
/// {
///     Cameras.Add(camera);
/// }
/// 
/// </summary>
public class CameraService : IDisposable
{
    public event EventHandler<Camera>? CameraAdded;
    public event EventHandler<Camera>? CameraRemoved;
    public event EventHandler? CaptureStopped;
    public event EventHandler<Mat>? Frame;

    public Camera[] Cameras => _usbService.Devices.ToArray();
    public Camera? OpenedCamera { get; private set; } = null;

    public CameraService()
    {
        _usbService.Inserted += UsbService_DeviceInserted;
        _usbService.Removed += UsbService_DeviceRemoved;
    }

    public void Dispose()
    {
        ShutdownCapture();
        GC.SuppressFinalize(this);
    }

    public bool Open(Camera camera, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        System.Diagnostics.Debug.WriteLine($"Open called by {caller}");

        if (_capture != null)
            return true;

        int cameraIndex = _usbService.Devices.ToList().IndexOf(camera);

        try
        {
            _capture = new VideoCapture((int)VideoCaptureAPIs.DSHOW + cameraIndex, VideoCaptureAPIs.ANY);
            if (!_capture.IsOpened())
            {
                _capture.Release();
                _capture = null;
                return false;
            }

            OpenedCamera = camera;
            _isBreakRequested = false;
            Task.Run(ProcessFrames);
        }
        catch (Exception err)
        {
            System.Diagnostics.Debug.WriteLine(err);
            return false;
        }

        return true;
    }

    public void ShutdownCapture([System.Runtime.CompilerServices.CallerMemberName]string caller = "")
    {
        if (_capture != null)
        {
            System.Diagnostics.Debug.WriteLine($"ShutdownCapture called by {caller}");

            _isBreakRequested = true;
            OpenedCamera = null;

            lock (_lock)
            {
                _capture?.Release();
                _capture = null;
            }

            CaptureStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    // Internal

    readonly UsbService _usbService = new([
        //new UsbFilter("PNPClass", ["Image", "Camera"])
        new UsbFilter("ClassGuid", ["{6bdd1fc6-810f-11d0-bec7-08002be2092f}"])
    ]);

    readonly Mutex _lock = new();

    VideoCapture? _capture;
    bool _isBreakRequested = false;

    private void UsbService_DeviceInserted(object? sender, Camera camera)
    {
        System.Diagnostics.Debug.WriteLine($"Inserted: {camera.Name}");
        CameraAdded?.Invoke(this, camera);
    }

    private void UsbService_DeviceRemoved(object? sender, Camera camera)
    {
        System.Diagnostics.Debug.WriteLine($"Removed: {camera.Name}");
        CameraRemoved?.Invoke(this, camera);
    }

    private void ProcessFrames()
    {
        var frame = new Mat();

        while (!_isBreakRequested && _capture != null)
        {
            lock (_lock)
            {
                _capture.Read(frame);
            }

            if (frame.Empty())
                break;

            Frame?.Invoke(this, frame.Flip(FlipMode.Y));

            Cv2.WaitKey(30);
        }

        ShutdownCapture();
    }
}