using Leap;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;

namespace HandTracker;

internal class MainViewModel : INotifyPropertyChanged
{
    public bool IsReady
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReady)));
        }
    } = false;

    public bool IsConnected
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
        }
    } = false;

    public bool IsRunning
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

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        }
    } = false;

    public LeapMotionDevice? SelectedDevice
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDevice)));

            HasSelectedDevice = value != null;
        }
    } = null;

    public bool HasSelectedDevice
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelectedDevice)));
        }
    }

    public bool HasDevices
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDevices)));
        }
    } = false;

    public ObservableCollection<LeapMotionDevice> Devices { get; } = [];

    /// <summary>
    /// Maximum distance for the hand to be tracked, in cm
    /// </summary>
    public double MaxDistance { get; set; } = 50;

    /// <summary>
    /// Reports hand location as 3 vectors: palm, infdex finger tip and middle finger tip.
    /// The coordinate system is as it used to be in the original Leap Motion:
    /// X = left, Y = forward, Z = down
    /// </summary>
    public event EventHandler<HandLocation>? Data;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(LeapMotion? lm, Dispatcher dispatcher)
    {
        _lm = lm;
        _dispatcher = dispatcher;

        if (_lm != null)
        {
            IsReady = true;

            _lm.Connect += Lm_Connect;
            _lm.Disconnect += Lm_Disconnect;
            _lm.FrameReady += Lm_FrameReady;

            _lm.Device += Lm_Device;
            _lm.DeviceLost += Lm_DeviceLost;
            _lm.DeviceFailure += Lm_DeviceFailure;

            // Ask for frames even in the background - this is important!
            _lm.SetPolicy(LeapMotion.PolicyFlag.POLICY_BACKGROUND_FRAMES);
            _lm.SetPolicy(LeapMotion.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);

            _lm.ClearPolicy(LeapMotion.PolicyFlag.POLICY_IMAGES);       // NO images, please

            foreach (var device in _lm.Devices)
            {
                Devices.Add(new LeapMotionDevice(device));
            }

            HasDevices = _lm.Devices.Count > 0;

            EnsureSomeDeviceIsSelected();
        }
    }

    // Internal

    readonly LeapMotion? _lm = null;
    readonly Dispatcher _dispatcher;

    private void EnsureSomeDeviceIsSelected()
    {
        if (Devices.Count > 0 && SelectedDevice == null)
        {
            SelectedDevice = Devices.First();
        }
    }

    private void Run()
    {
        if (_lm == null || IsConnected)
            return;

        if (_lm.Devices.Count == 0)
        {
            Debug.WriteLine("[LM] Found no devices");
            return;
        }

        if (_lm.IsConnected)
        {
            IsConnected = true;
        }
        else
        {
            _lm.StartConnection();
        }
    }

    private void Stop()
    {
        _lm?.StopConnection();
        IsConnected = false;
    }

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
        var device = Devices.FirstOrDefault(d => d.Device.SerialNumber == e.Device.SerialNumber);

        if (device != null)
        {
            _dispatcher.Invoke(() =>
            {
                if (IsRunning)
                {
                    Stop();
                }

                Devices.Remove(device);
                HasDevices = _lm?.Devices.Count > 0;
            });

            Debug.WriteLine($"[LM] Device {e.Device.SerialNumber} was lost");
        }
    }

    private void Lm_Device(object? sender, DeviceEventArgs e)
    {
        if (Devices.FirstOrDefault(d => d.Device.SerialNumber == e.Device.SerialNumber) == null)
        {
            _dispatcher.Invoke(() =>
            {
                Devices.Add(new LeapMotionDevice(e.Device));
                HasDevices = _lm?.Devices.Count > 0;
                EnsureSomeDeviceIsSelected();
            });

            Debug.WriteLine($"[LM] Found device {e.Device.SerialNumber}");
        }
    }

    private void Lm_Disconnect(object? sender, ConnectionLostEventArgs e)
    {
        IsConnected = false;
    }

    private void Lm_Connect(object? sender, ConnectionEventArgs e)
    {
        IsConnected = true;
    }

    private void Lm_FrameReady(object? sender, FrameEventArgs e)
    {
        if (!IsRunning)
            return;

        if (!IsConnected)
            IsConnected = true;

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

            if (Math.Sqrt(palm.x * palm.x + palm.y * palm.y + palm.z * palm.z) < MaxDistance)
            {
                handDetected = true;
                _dispatcher.Invoke(() =>
                {
                    Data?.Invoke(this, new HandLocation(in palm, in thumb, in index, in middle));
                });
            }
        }

        if (!handDetected)
        {
            _dispatcher.Invoke(() =>
            {
                Data?.Invoke(this, new HandLocation());
            });
        }

        // e.frame.Hands[0].Fingers[0..4].TipPosition.x;
        // e.frame.Hands[0].PalmVelocity.Magnitude);
        // e.frame.Hands[0].Fingers[x].TipPosition.DistanceTo(e.frame.Hands[0].Fingers[x + 1].TipPosition);
        // e.frame.Hands[0].PalmNormal.x
    }

    #endregion
}
