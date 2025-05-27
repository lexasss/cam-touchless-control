using System.Numerics;
using System.Windows;
using ThrottleDebounce;

namespace CameraTouchlessControl.Services;

public class ZoomPanService
{
    public enum HandState
    {
        Invisible,
        Moving,
        Adjusting,
        Still
    }

    public event EventHandler<double>? ScaleChanged;
    public event EventHandler<Point>? OffsetChanged;
    public event EventHandler<Vector3>? HandCursorMoved;
    public event EventHandler<HandState>? HandStateChanged;

    public ZoomPanService()
    {
        TimeSpan eventThrottlingInterval = TimeSpan.FromMilliseconds(EVENT_THROTTLING_INTERVAL);
        TimeSpan movementThrottlingInterval = TimeSpan.FromMilliseconds(MOVEMENT_THROTTLING_INTERVAL);

        _scaleChangeNotification = Throttler.Throttle(FireScaleChangedEvent, eventThrottlingInterval);
        _offsetChangeNotification = Throttler.Throttle(FireOffsetChangedEvent, eventThrottlingInterval);
        _handCursorMovedNotification = Throttler.Throttle(FireHandCursorMovedEvent, movementThrottlingInterval);
    }

    public void Feed(HandLocation handLocation)
    {
        Store(handLocation);

        HandState newHandState = _handState;

        var speed = GetSpeed();
        if (speed == 0)
        {
            newHandState = HandState.Invisible;
        }
        else if (speed < _stateExitDownThreshold || speed > _stateExitUpThreshold)
        {
            newHandState = speed switch
            {
                0 => HandState.Invisible,
                < ADJUSTMENT_THRESHOLD => HandState.Still,
                < MOVEMENT_THRESHOLD => HandState.Adjusting,
                _ => HandState.Moving
            };
        }

        if (newHandState != _handState)
        {
            newHandState = UpdateStateCandidateCounter(newHandState);
        }

        if (newHandState != _handState)
        {
            _handState = newHandState;
            HandStateChanged?.Invoke(this, _handState);

            (_stateExitUpThreshold, _stateExitDownThreshold) = _handState switch
            {
                HandState.Invisible => (1e8, ADJUSTMENT_THRESHOLD / 1.2),
                HandState.Still => (ADJUSTMENT_THRESHOLD * 1.2, 0),
                HandState.Adjusting => (MOVEMENT_THRESHOLD * 1.2, ADJUSTMENT_THRESHOLD / 1.5),
                HandState.Moving => (1e8, MOVEMENT_THRESHOLD / 1.2),
                _ => throw new NotImplementedException()
            };

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

            _handCursorMovedNotification.Invoke();

            _adjScale = _scale - _dy * ZOOMING_SENSITIVITY;
            _scaleChangeNotification.Invoke();

            _adjOffsetX = _offsetX + _dx * OFFSET_SENSITIVITY;
            _adjOffsetY = _offsetY + _dz * OFFSET_SENSITIVITY;
            _offsetChangeNotification.Invoke();
        }
    }

    // Internal

    const int BUFFER_SIZE = 20;
    const double ADJUSTMENT_THRESHOLD = 0.003; // cm per frame, the hand is "still" below this threshold, and "adjusting" when above it
    const double MOVEMENT_THRESHOLD = 0.01;    // cm per frame, the hand is "adjusting" below this threshold, and "moving" when above it
    const int COUNTER_WIN_THRESHOLD = 10;
    const double LOW_PASS_FILTER_ALPHA = 0.6;
    const int EVENT_THROTTLING_INTERVAL = 200; // ms
    const int MOVEMENT_THROTTLING_INTERVAL = 40; // ms
    const double ZOOMING_SENSITIVITY = 0.05;
    const double OFFSET_SENSITIVITY = 10;
    const float HAND_CUSOR_MOVEMENT_SCALE = 10;

    readonly HandLocation?[] _buffer = new HandLocation?[BUFFER_SIZE];
    readonly Dictionary<HandState, int> _stateCandidateCounters = new()
    {
        { HandState.Invisible, 0 },
        { HandState.Still, 0 },
        { HandState.Adjusting, 0 },
        { HandState.Moving, 0 },
    };

    readonly RateLimitedAction _scaleChangeNotification;
    readonly RateLimitedAction _offsetChangeNotification;
    readonly RateLimitedAction _handCursorMovedNotification;

    readonly LowPassFilterService _lpfX = new LowPassFilterService(LOW_PASS_FILTER_ALPHA);
    readonly LowPassFilterService _lpfY = new LowPassFilterService(LOW_PASS_FILTER_ALPHA);
    readonly LowPassFilterService _lpfZ = new LowPassFilterService(LOW_PASS_FILTER_ALPHA);

    HandState _handState = HandState.Invisible;
    double _stateExitUpThreshold = 1e8;
    double _stateExitDownThreshold = ADJUSTMENT_THRESHOLD / 1.2;

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
            ScaleChanged?.Invoke(this, _adjScale);
            System.Diagnostics.Debug.WriteLine($"ZOOM {_adjScale:F2}");
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
            HandCursorMoved?.Invoke(this, new Vector3(
                (float)_dx * HAND_CUSOR_MOVEMENT_SCALE,
                (float)_dy * HAND_CUSOR_MOVEMENT_SCALE,
                (float)_dz * HAND_CUSOR_MOVEMENT_SCALE
            ));
            //System.Diagnostics.Debug.WriteLine($"OFFSET {_dx:F1} {_dz:F1}");
        }
    }

    private void Store(HandLocation handLocation)
    {
        for (int i = 1; i < BUFFER_SIZE; i++)
        {
            _buffer[i] = _buffer[i - 1];
        }

        _buffer[0] = handLocation;
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

    private double GetSpeed()
    {
        double dx = 0;
        double dy = 0;
        double dz = 0;

        int count = 0;

        for (int i = 1; i < BUFFER_SIZE; i++)
        {
            var handLoc1 = _buffer[i];
            var handLoc2 = _buffer[i - 1];

            if (handLoc1 == null || handLoc2 == null ||
                handLoc1.IsEmpty || handLoc2.IsEmpty)
                continue;

            Leap.Vector palm1 = handLoc1.Palm;
            Leap.Vector palm2 = handLoc2.Palm;

            dx += palm1.x - palm2.x;
            dy += palm1.y - palm2.y;
            dz += palm1.z - palm2.z;

            count += 1;
        }

        dx /= count > 0 ? count : 1;
        dy /= count > 0 ? count : 1;
        dz /= count > 0 ? count : 1;

        var result = Math.Sqrt(dx * dx + dy * dy + dz * dz);

        //System.Diagnostics.Debug.WriteLine(result);
        return result;
    }
}
