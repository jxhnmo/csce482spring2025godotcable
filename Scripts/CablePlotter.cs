using Godot;

public interface CablePlotter
{
	void Generate(Vector2 startPoint, Vector2 endPoint, float mass, float length, int segmentCount);
	void HidePlot();
	void ShowPlot();
}
