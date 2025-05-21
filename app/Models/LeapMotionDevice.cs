namespace HandTracker;

public class LeapMotionDevice(Leap.Device device)
{
    public Leap.Device Device { get; } = device;

    public override string ToString()
    {
        return Device.SerialNumber;
    }
}
