namespace CameraTouchlessControl;

public enum HandState
{
    Invisible,
    Moving,
    Adjusting,
    Still
}

internal interface IHandStateDetector
{
    HandState? GetStateCandidate(HandLocation handLocation);
    void UpdateState(HandState state);
}
