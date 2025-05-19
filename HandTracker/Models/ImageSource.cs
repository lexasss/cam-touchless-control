using GitHub.secile.Video;
using System.Windows.Media.Imaging;

namespace HandTracker;

internal class ImageSource : IDisposable
{
    public event EventHandler<string>? CameraAdded;
    public event EventHandler<string>? CameraRemoved;
    public event EventHandler<BitmapSource>? Image;

    public void Dispose()
    {
        CloseCurrentVideoSource();
        GC.SuppressFinalize(this);
    }

    public void EnumCameras()
    {
        _cameras.Clear();

        var videoDevices = UsbCamera.FindDevices();

        for (int j = 0; j < videoDevices.Length; j++)
        {
            var name = videoDevices[j];
            if (_cameras.FirstOrDefault(c => c.Name == name) == null)
            {
                System.Diagnostics.Debug.WriteLine(name);

                UsbCamera.VideoFormat[] formats = UsbCamera.GetVideoFormat(j);
                for (int i = 0; i < formats.Length; i++)
                    System.Diagnostics.Debug.WriteLine($"  {i}: {formats[i]}");

                if (formats.Length > 0)
                {
                    _cameras.Add(new CameraDescriptor(name, formats));
                    CameraAdded?.Invoke(this, name);
                }
            }
        }
    }

    public bool Open(string name)
    {
        CloseCurrentVideoSource();

        if (_cameras.Count == 0)
            EnumCameras();

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
                PreviewCaptured = (b) => Image?.Invoke(this, b)
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

    UsbCamera? _camera = null;
    List<CameraDescriptor> _cameras = [];
}
