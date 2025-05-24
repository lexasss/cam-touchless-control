using OpenCvSharp;

namespace CameraTouchlessControl;

public class DShowCamera
{
    public VideoCaptureAPIs Api { get; }
    public int ID { get; }
    public UsbDevice Device { get; }

    public DShowCamera(VideoCaptureAPIs api, int id, UsbDevice device)
    {
        Api = api;
        ID = id;
        Device = device;

        System.Diagnostics.Debug.WriteLine($"{device.Name} {device.ID} {api} {id}");
    }

    public override string ToString() => Device.Name;
}
