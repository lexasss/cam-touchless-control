namespace CameraTouchlessControl;

internal class ZoomPanConfig
{
    public static ZoomPanConfig Instance => _instance ??= new ZoomPanConfig();

    public double ZoomGain { get; set; } = 1.1;
    public double PanGain { get; set; } = 20;

    // Intenral

    static ZoomPanConfig? _instance = null;

    private ZoomPanConfig() { }
}
