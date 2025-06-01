namespace CameraTouchlessControl.Services.HandStateDetectors;

internal class SpeedBased : IHandStateDetector
{
    public HandState? GetStateCandidate(HandLocation handLocation)
    {
        HandState? newHandState = null;

        Store(handLocation);

        var speed = GetSpeed();
        if (speed == 0)
        {
            newHandState = HandState.Invisible;
        }
        else if (speed < _stateExitDownSpeedThreshold || speed > _stateExitUpSpeedThreshold)
        {
            newHandState = speed switch
            {
                0 => HandState.Invisible,
                < ADJUSTMENT_THRESHOLD => HandState.Still,
                < MOVEMENT_THRESHOLD => HandState.Adjusting,
                _ => HandState.Moving
            };
        }

        return newHandState;
    }

    public void UpdateState(HandState state)
    {
        (_stateExitUpSpeedThreshold, _stateExitDownSpeedThreshold) = state switch
        {
            HandState.Invisible => (1e8, ADJUSTMENT_THRESHOLD / 1.2),
            HandState.Still => (ADJUSTMENT_THRESHOLD * 1.2, 0),
            HandState.Adjusting => (MOVEMENT_THRESHOLD * 1.2, ADJUSTMENT_THRESHOLD / 1.5),
            HandState.Moving => (1e8, MOVEMENT_THRESHOLD / 1.2),
            _ => throw new NotImplementedException()
        };
    }

    // Internal

    const int BUFFER_SIZE = 20;

    const double ADJUSTMENT_THRESHOLD = 0.003; // cm per frame, the hand is "still" below this threshold, and "adjusting" when above it
    const double MOVEMENT_THRESHOLD = 0.01;    // cm per frame, the hand is "adjusting" below this threshold, and "moving" when above it

    readonly HandLocation?[] _buffer = new HandLocation?[BUFFER_SIZE];

    double _stateExitUpSpeedThreshold = 1e8;
    double _stateExitDownSpeedThreshold = ADJUSTMENT_THRESHOLD / 1.2;

    private void Store(HandLocation handLocation)
    {
        for (int i = 1; i < BUFFER_SIZE; i++)
        {
            _buffer[i] = _buffer[i - 1];
        }

        _buffer[0] = handLocation;
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
