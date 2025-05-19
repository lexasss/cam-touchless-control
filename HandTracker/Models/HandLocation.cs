using Leap;

namespace HandTracker;

public class HandLocation(ref readonly Vector palm, ref readonly Vector thumb, ref readonly Vector index, ref readonly Vector middle)
{
    public Vector Palm { get; set; } = palm;
    public Vector Thumb { get; set; } = thumb;
    public Vector Index { get; set; } = index;
    public Vector Middle { get; set; } = middle;

    public HandLocation() : this(in Vector.Zero, in Vector.Zero, in Vector.Zero, in Vector.Zero) { }

    public void CopyTo(HandLocation rhs)
    {
        rhs.Palm = Palm;
        rhs.Thumb = Thumb;
        rhs.Index = Index;
        rhs.Middle = Middle;
    }
}