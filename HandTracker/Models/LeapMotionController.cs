using Leap;

namespace HandTracker;

internal class LeapMotionController : IDisposable
{
    /// <summary>
    /// Reports hand location as 3 vectors: palm, infdex finger tip and middle finger tip.
    /// The coordinate system is as it used to be in the original Leap Motion:
    /// X = left, Y = forward, Z = down
    /// </summary>
    public event EventHandler<HandLocation>? Data;

    public bool IsReady => _lm != null;

    /// <summary>
    /// Maximum distance for the hand to be tracked, in cm
    /// </summary>
    public double MaxDistance { get; set; } = 80;
    
    public LeapMotionController()
    {
        try
        {
            _lm = new LeapMotion();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to connect to LeapMotion: {e.Message}");
        }

        if (_lm != null)
        {
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
        }
    }

    public void Run()
    {
        if (_lm == null || _isConnected)
            return;

        if (_lm.Devices.Count == 0)
        {
            Console.WriteLine("[LM] Found no devices");
        }

        if (_lm.IsConnected)
        {
            _isConnected = true;
        }
        else
        {
            _lm.StartConnection();
        }

        _isRunning = true;
    }

    public void Dispose()
    {
        _lm?.Dispose();
        _lm = null;

        GC.SuppressFinalize(this);
    }

    LeapMotion? _lm = null;
    bool _isConnected = false;
    bool _isRunning = false;

    private void Lm_Disconnect(object? sender, ConnectionLostEventArgs e)
    {
        _isConnected = false;
    }

    private void Lm_Connect(object? sender, ConnectionEventArgs e)
    {
        _isConnected = true;
    }

    private void Lm_FrameReady(object? sender, FrameEventArgs e)
    {
        if (!_isRunning)
            return;

        if (!_isConnected)
            _isConnected = true;

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
                Data?.Invoke(this, new HandLocation(in palm, in thumb, in index, in middle));
            }
        }

        if (!handDetected)
        {
            Data?.Invoke(this, new HandLocation());
        }

        // e.frame.Hands[0].Fingers[0..4].TipPosition.x;
        // e.frame.Hands[0].PalmVelocity.Magnitude);
        // e.frame.Hands[0].Fingers[x].TipPosition.DistanceTo(e.frame.Hands[0].Fingers[x + 1].TipPosition);
        // e.frame.Hands[0].PalmNormal.x
        // e.frame.Hands[iTrackHand].GrabAngle
        // e.frame.Hands[iTrackHand].PinchDistance
    }

    private void Lm_DeviceFailure(object? sender, DeviceFailureEventArgs e)
    {
        Console.WriteLine($"[LM] Device {e.DeviceSerialNumber} failure: {e.ErrorMessage} ({e.ErrorCode})");
    }

    private void Lm_DeviceLost(object? sender, DeviceEventArgs e)
    {
        Console.WriteLine($"[LM] Device {e.Device.SerialNumber} was lost");
    }

    private void Lm_Device(object? sender, DeviceEventArgs e)
    {
        Console.WriteLine($"[LM] Found device {e.Device.SerialNumber}");
    }
}
