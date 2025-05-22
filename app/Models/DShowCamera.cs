using OpenCvSharp;

namespace CameraTouchlessControl;

public class DShowCamera(VideoCaptureAPIs api, string apiName, int id, UsbDevice device)
{
    public VideoCaptureAPIs Api => api;
    public string ApiName => apiName;
    public int ID => id;
    public UsbDevice Device => device;
    public override string ToString() => Device.Name;

    public static Mat WhiteImage { get; } = new Mat(480, 640, MatType.CV_8UC3, Scalar.White);
}
