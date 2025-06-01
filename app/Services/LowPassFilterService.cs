namespace CameraTouchlessControl;

internal class LowPassFilterService
{
    public double Alpha { get; set; }

    public LowPassFilterService(double alpha)
    {
        Alpha = alpha;
    }

    public void Reset()
    {
        _prevValue = null;
    }

    public double Feed(double value)
    {
        if (_prevValue == null)
        {
            _prevValue = value;
            return value;
        }
        else
        {
            double result = (double)(_prevValue + Alpha * (value - _prevValue));
            _prevValue = result;
            return result;
        }
    }

    // Internal

    double? _prevValue = null;
}
