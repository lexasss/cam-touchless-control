using System.Windows;

namespace HandTracker;

public partial class App : Application, IDisposable
{
    public Leap.LeapMotion? LeapMotion { get; }

    public App() : base()
    {
        try
        {
            LeapMotion = new Leap.LeapMotion();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to connect to LeapMotion: {e.Message}");
        }
    }

    public void Dispose()
    {
        LeapMotion?.Dispose();
        GC.SuppressFinalize(this);
    }
}
