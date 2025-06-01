using CameraTouchlessControl.ViewModels;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Camera = CameraTouchlessControl.UsbDevice;

namespace CameraTouchlessControl;

public class MainViewModel : INotifyPropertyChanged
{
    #region Hand tracking props

    public bool IsHandTrackerReady
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandTrackerReady)));
        }
    } = false;

    public bool IsHandTrackerConnected
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandTrackerConnected)));
        }
    } = false;

    public bool IsHandTrackingRunning
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            if (value)
            {
                StartHandTracking();
            }
            else
            {
                StopHandTracking();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandTrackingRunning)));
        }
    } = false;

    public LeapMotionDevice? SelectedHandTracker
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedHandTracker)));

            HasSelectedHandTracker = value != null;
        }
    } = null;

    public bool HasSelectedHandTracker
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelectedHandTracker)));
        }
    }

    public bool HasHandTrackers
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasHandTrackers)));
        }
    } = false;

    public ObservableCollection<LeapMotionDevice> HandTrackers { get; } = [];

    #endregion

    #region Camera props

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

    #endregion

    #region ZoomPan props

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

    #endregion

    public MainLayoutViewModel Layout { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public class RequestViewportSizeEventArgs : EventArgs
    {
        public Size ViewportSize { get; set; } = default;
    }

    public event EventHandler<RequestViewportSizeEventArgs>? RequestViewportSize;

    public MainViewModel(
        HandTrackingService handTrackingService,
        CameraService cameraService,
        ZoomPanService zoomPanService,
        Dispatcher dispatcher)
    {
        _handTrackingService = handTrackingService;
        _cameraService = cameraService;
        _zoomPanService = zoomPanService;
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

        IsHandTrackerReady = _handTrackingService.IsReady;

        _handTrackingService.ConnectionStatusChanged += HandTracker_ConnectionStatusChanged;
        _handTrackingService.DeviceAdded += HandTracker_DeviceAdded;
        _handTrackingService.DeviceRemoved += HandTracker_DeviceRemoved;
        _handTrackingService.HandData += HandTrackingService_HandData;

        foreach (var device in _handTrackingService.Devices)
        {
            HandTrackers.Add(new LeapMotionDevice(device));
        }

        HasHandTrackers = _handTrackingService.Devices.Count > 0;

        EnsureSomeHandTrackerIsSelected();

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

    readonly HandTrackingService _handTrackingService;
    readonly CameraService _cameraService;
    readonly ZoomPanService _zoomPanService;
    readonly Dispatcher _dispatcher;

    private void EnsureSomeHandTrackerIsSelected()
    {
        if (HandTrackers.Count > 0 && SelectedHandTracker == null)
        {
            SelectedHandTracker = HandTrackers.First();
        }
    }

    private void EnsureSomeCameraIsSelected()
    {
        if (SelectedCamera == null && Cameras.Count > 0)
        {
            SelectedCamera = Cameras.FirstOrDefault();
        }
    }

    private void StartHandTracking()
    {
        if (IsHandTrackerConnected)
            return;

        if (_handTrackingService.Devices.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Found no hand tracking devices");
            return;
        }

        if (_handTrackingService.IsConnected)
        {
            IsHandTrackerConnected = true;
        }
        else
        {
            _handTrackingService.Connect();
        }
    }

    private void StopHandTracking()
    {
        IsHandTrackingRunning = false;
    }

    #region Camera events

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

    #endregion

    #region LM events

    private void HandTracker_DeviceRemoved(object? sender, Leap.Device e)
    {
        var device = HandTrackers.FirstOrDefault(d => d.Device.SerialNumber == e.SerialNumber);

        if (device != null)
        {
            _dispatcher.Invoke(() =>
            {
                if (IsHandTrackingRunning)
                {
                    StopHandTracking();
                }

                HandTrackers.Remove(device);
                HasHandTrackers = _handTrackingService?.Devices.Count > 0;
            });

            System.Diagnostics.Debug.WriteLine($"Hand tracking device {e.SerialNumber} was lost");
        }
    }

    private void HandTracker_DeviceAdded(object? sender, Leap.Device e)
    {
        if (HandTrackers.FirstOrDefault(d => d.Device.SerialNumber == e.SerialNumber) == null)
        {
            _dispatcher.Invoke(() =>
            {
                HandTrackers.Add(new LeapMotionDevice(e));
                HasHandTrackers = _handTrackingService?.Devices.Count > 0;
                EnsureSomeHandTrackerIsSelected();
            });

            System.Diagnostics.Debug.WriteLine($"Found hand tracking device {e.SerialNumber}");
        }
    }

    private void HandTracker_ConnectionStatusChanged(object? sender, bool e)
    {
        IsHandTrackerConnected = e;
    }

    private void HandTrackingService_HandData(object? sender, HandLocation e)
    {
        if (!IsHandTrackingRunning)
            return;

        _dispatcher.Invoke(() =>
        {
            _zoomPanService.Feed(e);
        });
    }

    #endregion

    #region ZoomPan events

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

    #endregion
}
