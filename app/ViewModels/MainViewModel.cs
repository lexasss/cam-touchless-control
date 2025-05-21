using Leap;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CameraTouchlessControl;

internal class MainViewModel : INotifyPropertyChanged
{
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
                Run();
            else
                Stop();

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
                    if (!_cameraService.Open(SelectedCamera))
                    {
                        Task.Run(async () => {
                            await Task.Delay(500);
                            _dispatcher.Invoke(() => IsCameraCapturing = false);
                        });
                    }
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

    public ObservableCollection<LeapMotionDevice> HandTrackers { get; } = [];

    public ObservableCollection<Camera> Cameras { get; } = [];

    /// <summary>
    /// Maximum distance for the hand to be tracked, in cm
    /// </summary>
    public double MaxHandTrackingDistance { get; set; } = 50;

    /// <summary>
    /// Reports hand location as 3 vectors: palm, infdex finger tip and middle finger tip.
    /// The coordinate system is as it used to be in the original Leap Motion:
    /// X = left, Y = forward, Z = down
    /// </summary>
    public event EventHandler<HandLocation>? HandData;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(LeapMotion? handTrackingService, CameraService cameraService, Dispatcher dispatcher)
    {
        _handTrackingService = handTrackingService;
        _cameraService = cameraService;
        _dispatcher = dispatcher;

        _cameraService.CameraAdded += CameraService_CameraAdded;
        _cameraService.CameraRemoved += CameraService_CameraRemoved;
        _cameraService.Frame += CameraService_FrameReceived;

        Task.Run(async () =>
        {
            await _cameraService.UpdateCameralist();
            EnsureSomeCameraIsSelected();
        });

        if (_handTrackingService != null)
        {
            IsHandTrackerReady = true;

            _handTrackingService.Connect += Lm_Connect;
            _handTrackingService.Disconnect += Lm_Disconnect;
            _handTrackingService.FrameReady += Lm_FrameReady;

            _handTrackingService.Device += Lm_Device;
            _handTrackingService.DeviceLost += Lm_DeviceLost;
            _handTrackingService.DeviceFailure += Lm_DeviceFailure;

            // Ask for frames even in the background - this is important!
            _handTrackingService.SetPolicy(LeapMotion.PolicyFlag.POLICY_BACKGROUND_FRAMES);
            _handTrackingService.SetPolicy(LeapMotion.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);

            _handTrackingService.ClearPolicy(LeapMotion.PolicyFlag.POLICY_IMAGES);       // NO images, please

            foreach (var device in _handTrackingService.Devices)
            {
                HandTrackers.Add(new LeapMotionDevice(device));
            }

            HasHandTrackers = _handTrackingService.Devices.Count > 0;

            EnsureSomeHandTrackerIsSelected();
        }
    }

    // Internal

    readonly LeapMotion? _handTrackingService = null;
    readonly CameraService _cameraService;
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
        if (Cameras.Count > 0 && SelectedCamera == null)
        {
            SelectedCamera = Cameras.First();
        }
    }

    private void Run()
    {
        if (_handTrackingService == null || IsHandTrackerConnected)
            return;

        if (_handTrackingService.Devices.Count == 0)
        {
            Debug.WriteLine("[LM] Found no devices");
            return;
        }

        if (_handTrackingService.IsConnected)
        {
            IsHandTrackerConnected = true;
        }
        else
        {
            _handTrackingService.StartConnection();
        }
    }

    private void Stop()
    {
        _handTrackingService?.StopConnection();
        IsHandTrackerConnected = false;
    }

    #region Camera events

    private void CameraService_FrameReceived(object? sender, OpenCvSharp.Mat e)
    {
        _dispatcher.Invoke(() =>
        {
            if (IsCameraCapturing) CameraFrame = e.ToBitmapSource();
        });
    }

    private void CameraService_CameraRemoved(object? sender, Camera e)
    {
        _dispatcher.Invoke(() =>
        {
            if (SelectedCamera == e && IsCameraCapturing)
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

    #endregion

    #region LM events

    private void Lm_DeviceFailure(object? sender, DeviceFailureEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            Debug.WriteLine($"[LM] Device {e.DeviceSerialNumber} failure: {e.ErrorMessage} ({e.ErrorCode})");
        });
    }

    private void Lm_DeviceLost(object? sender, DeviceEventArgs e)
    {
        var device = HandTrackers.FirstOrDefault(d => d.Device.SerialNumber == e.Device.SerialNumber);

        if (device != null)
        {
            _dispatcher.Invoke(() =>
            {
                if (IsHandTrackingRunning)
                {
                    Stop();
                }

                HandTrackers.Remove(device);
                HasHandTrackers = _handTrackingService?.Devices.Count > 0;
            });

            Debug.WriteLine($"[LM] Device {e.Device.SerialNumber} was lost");
        }
    }

    private void Lm_Device(object? sender, DeviceEventArgs e)
    {
        if (HandTrackers.FirstOrDefault(d => d.Device.SerialNumber == e.Device.SerialNumber) == null)
        {
            _dispatcher.Invoke(() =>
            {
                HandTrackers.Add(new LeapMotionDevice(e.Device));
                HasHandTrackers = _handTrackingService?.Devices.Count > 0;
                EnsureSomeHandTrackerIsSelected();
            });

            Debug.WriteLine($"[LM] Found device {e.Device.SerialNumber}");
        }
    }

    private void Lm_Disconnect(object? sender, ConnectionLostEventArgs e)
    {
        IsHandTrackerConnected = false;
    }

    private void Lm_Connect(object? sender, ConnectionEventArgs e)
    {
        IsHandTrackerConnected = true;
    }

    private void Lm_FrameReady(object? sender, FrameEventArgs e)
    {
        if (!IsHandTrackingRunning)
            return;

        if (!IsHandTrackerConnected)
            IsHandTrackerConnected = true;

        bool handDetected = false;

        int handIndex = 0;

        while (handIndex < e.frame.Hands.Count && e.frame.Hands[handIndex].IsLeft)
        {
            handIndex++;
        }

        if (handIndex < e.frame.Hands.Count)
        {
            var palm = e.frame.Hands[handIndex].PalmPosition / 10;
            var fingers = e.frame.Hands[handIndex].Fingers;
            var thumb = fingers[0].TipPosition / 10;
            var index = fingers[1].TipPosition / 10;
            var middle = fingers[2].TipPosition / 10;

            if (Math.Sqrt(palm.x * palm.x + palm.y * palm.y + palm.z * palm.z) < MaxHandTrackingDistance)
            {
                handDetected = true;
                _dispatcher.Invoke(() =>
                {
                    HandData?.Invoke(this, new HandLocation(in palm, in thumb, in index, in middle));
                });
            }
        }

        if (!handDetected)
        {
            _dispatcher.Invoke(() =>
            {
                HandData?.Invoke(this, new HandLocation());
            });
        }

        // e.frame.Hands[0].Fingers[0..4].TipPosition.x;
        // e.frame.Hands[0].PalmVelocity.Magnitude);
        // e.frame.Hands[0].Fingers[x].TipPosition.DistanceTo(e.frame.Hands[0].Fingers[x + 1].TipPosition);
        // e.frame.Hands[0].PalmNormal.x
    }

    #endregion
}
