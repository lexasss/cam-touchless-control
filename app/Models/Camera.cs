using OpenCvSharp;

namespace CameraTouchlessControl;

public class Camera(int id, string name)
{
    public int ID => id;
    public string Name => name;
    public override string ToString() => name;

    public static Mat WhiteImage { get; } = new Mat(480, 640, MatType.CV_8UC3, Scalar.White);
}
