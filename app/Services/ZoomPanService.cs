using System.Numerics;
using System.Windows;
using ThrottleDebounce;
using CameraTouchlessControl.Services.HandStateDetectors;

namespace CameraTouchlessControl;

public class ZoomPanService
{
    public event EventHandler<double>? ScaleChanged;
    public event EventHandler<Point>? OffsetChanged;
    public event EventHandler<Vector3>? HandCursorMoved;
    public event EventHandler<HandState>? HandStateChanged;

    public ZoomPanService()
    {
        _handStateDetector = new SpeedBased();

        TimeSpan eventThrottlingInterval = TimeSpan.FromMilliseconds(EVENT_THROTTLING_INTERVAL);
        TimeSpan movementThrottlingInterval = TimeSpan.FromMilliseconds(MOVEMENT_THROTTLING_INTERVAL);

        _scaleChangeNotifyAction = Throttler.Throttle(FireScaleChangedEvent, eventThrottlingInterval);
        _offsetChangeNotifyAction = Throttler.Throttle(FireOffsetChangedEvent, eventThrottlingInterval);
        _handCursorMovedNotifyAction = Throttler.Throttle(FireHandCursorMovedEvent, movementThrottlingInterval);
    }

    public void Feed(HandLocation handLocation)
    {
        HandState newHandState = _handStateDetector.GetStateCandidate(handLocation) ?? _handState;

        if (newHandState != _handState)
        {
            newHandState = UpdateStateCandidateCounter(newHandState);
        }

        if (newHandState != _handState)
        {
            _handState = newHandState;
            HandStateChanged?.Invoke(this, _handState);

            _handStateDetector.UpdateState(_handState);

            _lpfX.Alpha = _handState == HandState.Still ? LOW_PASS_FILTER_ALPHA_STILL : LOW_PASS_FILTER_ALPHA_MOVING;
            _lpfY.Alpha = _handState == HandState.Still ? LOW_PASS_FILTER_ALPHA_STILL : LOW_PASS_FILTER_ALPHA_MOVING;
            _lpfZ.Alpha = _handState == HandState.Still ? LOW_PASS_FILTER_ALPHA_STILL : LOW_PASS_FILTER_ALPHA_MOVING;

            if (_handState == HandState.Still && _reference == null)
            {
                _reference = handLocation;

                _lpfX.Feed(handLocation.Palm.x);
                _lpfY.Feed(handLocation.Palm.y);
                _lpfZ.Feed(handLocation.Palm.z);
            }
            else if (_handState == HandState.Moving || _handState == HandState.Invisible)
            {
                _reference = null;

                _lpfX.Reset();
                _lpfY.Reset();
                _lpfZ.Reset();

                _scale = _adjScale;
                _offsetX = _adjOffsetX;
                _offsetY = _adjOffsetY;
            }

            System.Diagnostics.Debug.WriteLine(newHandState);
        }
        else if (_reference != null && (_handState == HandState.Still || _handState == HandState.Adjusting))
        {
            var x = _lpfX.Feed(handLocation.Palm.x);
            var y = _lpfY.Feed(handLocation.Palm.y);
            var z = _lpfZ.Feed(handLocation.Palm.z);

            _dx = x - _reference.Palm.x;
            _dy = y - _reference.Palm.y;
            _dz = z - _reference.Palm.z;

            _handCursorMovedNotifyAction.Invoke();

            _adjScale = _scale - _dy * ZOOMING_SENSITIVITY;
            _scaleChangeNotifyAction.Invoke();

            _adjOffsetX = _offsetX + _dx * OFFSET_SENSITIVITY;
            _adjOffsetY = _offsetY + _dz * OFFSET_SENSITIVITY;
            _offsetChangeNotifyAction.Invoke();
        }
    }

    // Internal

    const int COUNTER_WIN_THRESHOLD = 10;
    const double LOW_PASS_FILTER_ALPHA_STILL = 0.001;
    const double LOW_PASS_FILTER_ALPHA_MOVING = 0.1;
    const int EVENT_THROTTLING_INTERVAL = 200; // ms
    const int MOVEMENT_THROTTLING_INTERVAL = 40; // ms
    const double ZOOMING_SENSITIVITY = 0.05;
    const double OFFSET_SENSITIVITY = 10;

    readonly IHandStateDetector _handStateDetector;

    readonly Dictionary<HandState, int> _stateCandidateCounters = new()
    {
        { HandState.Invisible, 0 },
        { HandState.Still, 0 },
        { HandState.Adjusting, 0 },
        { HandState.Moving, 0 },
    };

    readonly RateLimitedAction _scaleChangeNotifyAction;
    readonly RateLimitedAction _offsetChangeNotifyAction;
    readonly RateLimitedAction _handCursorMovedNotifyAction;

    readonly LowPassFilterService _lpfX = new(LOW_PASS_FILTER_ALPHA_MOVING);
    readonly LowPassFilterService _lpfY = new(LOW_PASS_FILTER_ALPHA_MOVING);
    readonly LowPassFilterService _lpfZ = new(LOW_PASS_FILTER_ALPHA_MOVING);

    HandState _handState = HandState.Invisible;

    HandLocation? _reference = null;

    double _scale = 1.0;
    double _offsetX = 0;
    double _offsetY = 0;

    double _adjScale = 1.0;
    double _adjOffsetX = 0;
    double _adjOffsetY = 0;

    double _dx = 0;
    double _dy = 0;
    double _dz = 0;

    private void FireScaleChangedEvent()
    {
        if (_handState == HandState.Still ||  _handState == HandState.Adjusting)
        {
            double scale = _adjScale < 1 ? _adjScale : Math.Pow(_adjScale, 1.5);
            ScaleChanged?.Invoke(this, scale);
            System.Diagnostics.Debug.WriteLine($"ZOOM {scale:F2}");
        }
    }

    private void FireOffsetChangedEvent()
    {
        if (_handState == HandState.Still || _handState == HandState.Adjusting)
        {
            OffsetChanged?.Invoke(this, new Point(_adjOffsetX, _adjOffsetY));
            System.Diagnostics.Debug.WriteLine($"OFFSET {_adjOffsetX:F1} {_adjOffsetY:F1}");
        }
    }

    private void FireHandCursorMovedEvent()
    {
        if (_handState != HandState.Invisible)
        {
            HandCursorMoved?.Invoke(this, new Vector3((float)_dx, (float)_dy, (float)_dz));
            //System.Diagnostics.Debug.WriteLine($"OFFSET {_dx:F1} {_dz:F1}");
        }
    }

    private HandState UpdateStateCandidateCounter(HandState newHandState)
    {
        bool wins = false;
        foreach (HandState state in Enum.GetValues(typeof(HandState)))
        {
            var counter = _stateCandidateCounters[state];
            if (state == newHandState)
            {
                _stateCandidateCounters[state] = counter + 1;
                if (_stateCandidateCounters[state] == COUNTER_WIN_THRESHOLD)
                {
                    wins = true;
                    break;
                }
            }
            else
            {
                _stateCandidateCounters[state] = Math.Max(0, counter - 1);
            }
        }

        if (wins)
        {
            foreach (HandState state in Enum.GetValues(typeof(HandState)))
            {
                _stateCandidateCounters[state] = 0;
            }

            return newHandState;
        }

        return _handState;
    }
}
