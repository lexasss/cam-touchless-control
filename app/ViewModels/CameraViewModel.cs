using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Camera = CameraTouchlessControl.UsbDevice;

namespace CameraTouchlessControl;

public class CameraViewModel : INotifyPropertyChanged
{
    public bool IsCameraCapturing
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            if (value)
            {
                if (SelectedCamera != null)
                {
                    Task.Run(async () => {
                        bool wasOpened = _cameraService.Open(SelectedCamera);
                        if (!wasOpened)
                        {
                            await Task.Delay(500);
                            _dispatcher.Invoke(() => IsCameraCapturing = false);
                        }
                    });
                }
            }
            else
            {
                _cameraService.ShutdownCapture();
                CameraFrame = null;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCameraCapturing)));
        }
    } = false;

    public Camera? SelectedCamera
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCamera)));

            HasSelectedCamera = value != null;
        }
    } = null;

    public bool HasSelectedCamera
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelectedCamera)));
        }
    }

    public BitmapSource? CameraFrame
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraFrame)));
        }
    } = null;

    public ObservableCollection<Camera> Cameras { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public CameraViewModel(CameraService cameraService, Dispatcher dispatcher)
    {
        _cameraService = cameraService;
        _dispatcher = dispatcher;


        _cameraService.CameraAdded += CameraService_CameraAdded;
        _cameraService.CameraRemoved += CameraService_CameraRemoved;
        _cameraService.CaptureStopped += CameraService_CaptureStopped;
        _cameraService.Frame += CameraService_FrameReceived;

        foreach (var camera in _cameraService.Cameras)
        {
            Cameras.Add(camera);
        }

        EnsureSomeCameraIsSelected();
    }

    // Internal

    readonly CameraService _cameraService;
    readonly Dispatcher _dispatcher;

    private void EnsureSomeCameraIsSelected()
    {
        if (SelectedCamera == null && Cameras.Count > 0)
        {
            SelectedCamera = Cameras.FirstOrDefault();
        }
    }

    private void CameraService_FrameReceived(object? sender, OpenCvSharp.Mat e)
    {
        _dispatcher.Invoke(() =>
        {
            if (IsCameraCapturing)
                CameraFrame = e.ToBitmapSource();
        });
    }

    private void CameraService_CameraRemoved(object? sender, Camera e)
    {
        _dispatcher.Invoke(() =>
        {
            if (SelectedCamera?.ID == e.ID && IsCameraCapturing)
            {
                IsCameraCapturing = false;
            }

            Cameras.Remove(e);
            EnsureSomeCameraIsSelected();
        });
    }

    private void CameraService_CameraAdded(object? sender, Camera e)
    {
        _dispatcher.Invoke(() =>
        {
            Cameras.Add(e);
            EnsureSomeCameraIsSelected();
        });
    }

    private void CameraService_CaptureStopped(object? sender, EventArgs e)
    {
        IsCameraCapturing = false;
    }
}
