using GitHub.secile.Video;
using System.Windows.Media.Imaging;

namespace HandTracker;

internal class CameraService : IDisposable
{
    public record CameraListLocation(string Name, int Location);

    public event EventHandler<CameraListLocation>? CameraAdded;
    public event EventHandler<string>? CameraRemoved;
    public event EventHandler<BitmapSource>? Frame;

    public CameraService()
    {
        _usbService.Inserted += CameraService_CameraInserted;
        _usbService.Removed += CameraService_CameraRemoved;

        _canUpdateCameraList = true;
    }

    public void Dispose()
    {
        CloseCurrentVideoSource();
        GC.SuppressFinalize(this);
    }

    public void UpdateCameralist()
    {
        _cameras.Clear();

        var videoDevices = UsbCamera.FindDevices();
        AddMissingCameras(videoDevices);
    }

    public bool Open(string name)
    {
        CloseCurrentVideoSource();

        if (_cameras.Count == 0)
            UpdateCameralist();

        var camera = _cameras.FirstOrDefault(c => c.Name == name);
        if (camera == null)
            return false;

        int index = _cameras.IndexOf(camera);

        var reasonableFormats = camera.Formats.Where(f => f.Fps >= 15 && f.Size.Width >= 640);
        if (reasonableFormats.Count() == 0)
            return false;

        var maxFrameWidth = reasonableFormats.Max(f => f.Size.Width);

        var format = camera.Formats.FirstOrDefault(f => f.Size.Width == maxFrameWidth);
        if (format == null)
            return false;

        try
        {
            _camera = new(index, format)
            {
                PreviewCaptured = (bitmapSource) => Frame?.Invoke(this, bitmapSource)
            };

            _camera.Start();

            while (!_camera.IsReady)
            {
                Task.Delay(20);
            }
        }
        catch (Exception err)
        {
            System.Diagnostics.Debug.WriteLine(err);
            return false;
        }

        return true;
    }

    public void CloseCurrentVideoSource()
    {
        if (_camera != null)
        {
            try
            {
                _camera.Stop();
                _camera.Release();
            }
            finally
            {
                _camera = null;
            }
        }
    }

    // Internal

    record class CameraDescriptor(string Name, UsbCamera.VideoFormat[] Formats);

    readonly UsbService _usbService = new();

    UsbCamera? _camera = null;
    List<CameraDescriptor> _cameras = [];
    bool _canUpdateCameraList = false;

    private void AddMissingCameras(string[] videoDevices)
    {
        for (int i = 0; i < videoDevices.Length; i++)
        {
            var name = videoDevices[i];
            if (_cameras.FirstOrDefault(c => c.Name == name) == null)
            {
                //System.Diagnostics.Debug.WriteLine(name);

                UsbCamera.VideoFormat[] formats = UsbCamera.GetVideoFormat(i);

                //for (int j = 0; j < formats.Length; j++)
                //    System.Diagnostics.Debug.WriteLine($"  {j}: {formats[j]}");

                if (formats.Length > 0)
                {
                    _cameras.Insert(i, new CameraDescriptor(name, formats));
                    CameraAdded?.Invoke(this, new CameraListLocation(name, i));
                }
            }
        }
    }

    private void CameraService_CameraInserted(object? sender, UsbService.UsbCamera camera)
    {
        if (!_canUpdateCameraList)
        {
            System.Diagnostics.Debug.WriteLine(camera.Name);
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Inserted: {camera.Name}");

        Task.Run(async () =>
        {
            int maxWaitInterval = 5000;
            var videoDevices = UsbCamera.FindDevices();

            while (maxWaitInterval > 0 && videoDevices.Length <= _cameras.Count)
            {
                _ = Task.Delay(500);
                maxWaitInterval -= 500;
                videoDevices = UsbCamera.FindDevices();
            }

            AddMissingCameras(videoDevices);
        });
    }

    private void CameraService_CameraRemoved(object? sender, UsbService.UsbCamera camera)
    {
        if (!_canUpdateCameraList)
            return;

        System.Diagnostics.Debug.WriteLine($"Removed: {camera.Name}");

        var videoDevices = UsbCamera.FindDevices();

        Dictionary<CameraDescriptor, bool> checkedCameras = _cameras
            .Select(cam => new KeyValuePair<CameraDescriptor, bool>(cam, videoDevices.Contains(cam.Name)))
            .ToDictionary();

        var missingCameras = checkedCameras.Where(kv => !kv.Value);
        foreach (var missingCamera in missingCameras)
        {
            _cameras.Remove(missingCamera.Key);
            CameraRemoved?.Invoke(this, missingCamera.Key.Name);
        }
    }
}
