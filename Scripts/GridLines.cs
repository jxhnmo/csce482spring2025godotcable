using Godot;

public partial class GridLines : Node2D
{
    [Export] public float Length = 100000.0f;
    [Export] public Color AxisColor = new Color(1.0f, 0.0f, 0.0f); // Red
    [Export] public float LineWidth = 2.0f;

    public override void _Draw()
    {
        DrawLine(new Vector2(-Length, 0), new Vector2(Length, 0), AxisColor, LineWidth);
        DrawLine(new Vector2(0, -Length), new Vector2(0, Length), AxisColor, LineWidth);
    }

    public override void _Ready()
    {
        QueueRedraw();
    }
}