using OpenCvSharp;

namespace CameraTouchlessControl;

internal class CameraService : IDisposable
{
    public event EventHandler<Camera>? CameraAdded;
    public event EventHandler<Camera>? CameraRemoved;
    public event EventHandler<Mat>? Frame;

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

    public async Task UpdateCameralist()
    {
        _cameras.Clear();

        await Task.Run(() =>
        {
            var cameras = CameraDeviceEnumerator.Get();
            AddMissingCameras(cameras);
        });
    }

    public bool Open(Camera camera)
    {
        ShutdownCapture();

        int cameraIndex = _cameras.IndexOf(camera);

        try
        {
            _capture = new VideoCapture(cameraIndex);
            if (!_capture.IsOpened())
            {
                _capture.Release();
                _capture = null;
                return false;
            }

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

    public void ShutdownCapture()
    {
        if (_capture != null)
        {
            _isBreakRequested = true;

            lock (_capture)
            {
                _capture?.Release();
                _capture = null;
            }
        }
    }

    // Internal

    readonly UsbService _usbService = new(["Image", "Camera"]);
    readonly List<Camera> _cameras = [];

    //Camera? _camera = null;
    VideoCapture? _capture;
    bool _isBreakRequested = false;

    private void AddMissingCameras(Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            if (_cameras.FirstOrDefault(c => c.Name == camera.Name) == null)
            {
                _cameras.Insert(i, camera);
                CameraAdded?.Invoke(this, camera);
            }
        }
    }

    private void UsbService_DeviceInserted(object? sender, UsbService.UsbDevice camera)
    {
        System.Diagnostics.Debug.WriteLine($"Inserted: {camera.Name}");

        Task.Run(async () =>
        {
            await Task.Delay(2000); // 2 seconds if waiting because same value uses the UsbService listener to update its state

            Camera[] cameras = CameraDeviceEnumerator.Get();
            AddMissingCameras(cameras);
        });
    }

    private void UsbService_DeviceRemoved(object? sender, UsbService.UsbDevice camera)
    {
        System.Diagnostics.Debug.WriteLine($"Removed: {camera.Name}");

        Camera[] cameras = CameraDeviceEnumerator.Get();

        Dictionary<Camera, bool> checkedCameras = _cameras
            .Select(cam => new KeyValuePair<Camera, bool>(cam, cameras.Any(c => c.Name == cam.Name)))
            .ToDictionary();

        var missingCameras = checkedCameras.Where(kv => !kv.Value);
        foreach (var missingCamera in missingCameras)
        {
            _cameras.Remove(missingCamera.Key);
            CameraRemoved?.Invoke(this, missingCamera.Key);
        }
    }

    private void ProcessFrames()
    {
        var frame = new Mat();

        while (!_isBreakRequested && _capture != null)
        {
            lock (_capture)
            {
                _capture.Read(frame);
            }

            if (frame.Empty())
                break;

            Frame?.Invoke(this, frame);

            Cv2.WaitKey(30);
        }

        ShutdownCapture();
    }
}