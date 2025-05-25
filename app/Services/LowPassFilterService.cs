namespace CameraTouchlessControl.Services;

internal class LowPassFilterService
{
    public LowPassFilterService(double alpha)
    {
        _alpha = alpha;
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
            double result = (double)(_prevValue + _alpha * (value - _prevValue));
            _prevValue = result;
            return result;
        }
    }

    // Internal

    readonly double _alpha;

    double? _prevValue = null;
}

 /*
internal class LowPassFilter
{
    /// <summary>
    /// Creates a low-pass filter
    /// </summary>
    /// <param name="threshold">Threshold that defines the distance between 
    /// the current value and a received value that separates between strong and weak
    /// influence of this value</param>
    public LowPassFilter(double threshold, double gain = 0.01, double _weightDamping = 0.8)
    {
        _threshold = threshold;
        _gain = gain;
    }

    /// <summary>
    /// Estimated the next value based on the previous value and the received value
    /// </summary>
    /// <param name="value">value</param>
    /// <returns>filtered value</returns>
    public double Feed(double value)
    {
        if (!_pointExists)
        {
            _pointExists = true;
            _filteredPoint = value;
        }
        else
        {
            // We favor new values far to the current value.
            // The more distant the new value is, the larger weight it gets.
            // Note that we are using sigma-function to calculate the weight,
            // where 'threshold' defines its center (w=0.5), and 'gain' defines it steepness.
            double dist = value - _filteredPoint;
            double nextWeight1 = 1.0 / (1.0 + Math.Exp(_gain * (_threshold - dist)));

            // We favor new value close from the previous value
            double rtDist = value - _realTimePoint;
            double nextWeight2 = 1.0 - 1.0 / (1.0 + Math.Exp(_gain * (_threshold - rtDist)));

            // In summary, we set a high weight for the new point that is
            //   a) close to the previous raw point
            //   b) far from the current gaze point
            // This should allow jumping rapidly from one gaze location to another only when there are 
            // at least two consecutive points, both close to each other and far from the current gaze point.

            // In addition, we apply some filtering in any way by slightly pushing down the overall weight
            double nextWeight = nextWeight1 * nextWeight2 * _weightDamping;

            double prevWeight = 1.0 - nextWeight;

            _filteredPoint = _filteredPoint * prevWeight + value * nextWeight;
        }

        _realTimePoint = value;

        return _filteredPoint;
    }

    /// <summary>
    /// Informs the filter about gaze-entered and gaze-left events regarding the interaction space
    /// </summary>
    /// <param name="evt">gaze event to the reacted to</param>
    public void Inform(Plane.Plane.Event evt)
    {
        if (evt == Plane.Plane.Event.Exit)
        {
            _exitTimestamp = Timestamp.Ms;
        }
        else if (evt == Plane.Plane.Event.Enter)
        {
            if ((Timestamp.Ms - _exitTimestamp) > _options.LowPassFilterResetDelay)
            {
                _pointExists = false;
                _screenLogger?.Log("Reset");
            }
        }
    }

    // Internal

    readonly double _threshold;
    readonly double _gain;
    readonly double _weightDamping;

    double _filteredPoint = 0;
    double _realTimePoint = 0;

    bool _pointExists = false;
    long _exitTimestamp = 0;
}
*/