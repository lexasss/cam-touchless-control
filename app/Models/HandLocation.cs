using Leap;

namespace CameraTouchlessControl;

public class HandLocation(ref readonly Vector palm, ref readonly Vector thumb, ref readonly Vector index, ref readonly Vector middle)
{
    public Vector Palm { get; set; } = palm;
    public Vector Thumb { get; set; } = thumb;
    public Vector Index { get; set; } = index;
    public Vector Middle { get; set; } = middle;

    public bool IsEmpty { get; private set; } = false;

    public HandLocation() : this(in Vector.Zero, in Vector.Zero, in Vector.Zero, in Vector.Zero)
    {
        IsEmpty = true;
    }

    public void CopyTo(HandLocation lhs)
    {
        lhs.Palm = Palm;
        lhs.Thumb = Thumb;
        lhs.Index = Index;
        lhs.Middle = Middle;
        lhs.IsEmpty = IsEmpty;
    }
}