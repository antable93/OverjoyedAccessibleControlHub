namespace OverjoyedVersion3;

/// <summary>
/// Code representation of a single quadrant (slice, or wedge) of the dial.
/// </summary>
public class DialQuadrant
{
    public float StartAngle { get; }
    public float SweepAngle { get; }
    
    public DialQuadrant(float startAngle, float sweepAngle)
    {
        StartAngle = startAngle;
        SweepAngle = sweepAngle;
    }
}
