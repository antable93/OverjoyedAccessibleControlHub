namespace OverjoyedVersion3;

public class AxisInput
{
    public string InputId { get; }
    public Action<float>? Move1D { get; }
    public Action<float, float>? Move2D { get; }

    public AxisInput(string inputId, Action<float>? move1D = null,
        Action<float, float>? move2D = null)
    {
        InputId = inputId;
        Move1D = move1D;
        Move2D = move2D;
    }
}
