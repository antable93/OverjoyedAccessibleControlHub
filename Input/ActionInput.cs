namespace OverjoyedVersion3;

public class ActionInput
{
    public string InputId { get; }
    public Action? Down { get; }
    public Action? Up { get; }

    public ActionInput(string inputId, Action? down, Action? up)
    {
        InputId = inputId;
        Down = down;
        Up = up;
    }
}
