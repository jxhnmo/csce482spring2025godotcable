using Godot;
using System;
using System.Collections.Generic;

public partial class CableSag : Node2D
{
	[Export] public float LengthMeters { get; set; } = 100f;
	[Export] public float H { get; set; } = 5000f;
	[Export] public float SpanWeightNewtons { get; set; } = 50f;
	[Export] public int NumPoints { get; set; } = 12;
	[Export] public float PixelsPerMeter { get; set; } = 9f;
	
	private List<Vector2> vertices = new List<Vector2>();
	private float YMax = 0f;
	private float LCat = 0f;
	private float V;
	private float TMax;

	public override void _Ready()
	{
		ComputeVertices();
		ComputeForces();
	}


	private void ComputeVertices()
	{
		vertices.Clear();
		LCat = 0f;
		
		for (int i = 0; i < NumPoints; i++)
		{
			float x = (i / (float)(NumPoints - 1)) * LengthMeters;
			float y = -(H / SpanWeightNewtons) * (
				Mathf.Cosh((-SpanWeightNewtons / H) * (x - (LengthMeters / 2))) -
				Mathf.Cosh((SpanWeightNewtons * LengthMeters) / (2 * H))
			);
			
			Vector2 newVertex = new Vector2(x, y);
			if (i > 0)
			{
				float dx = Mathf.Abs(vertices[i - 1].X - newVertex.X);
				float dy = Mathf.Abs(vertices[i - 1].Y - newVertex.Y);
				LCat += Mathf.Sqrt(dx * dx + dy * dy);
			}
			
			vertices.Add(newVertex);
		}

		float xMid = .5f * LengthMeters;
		YMax = -(H / SpanWeightNewtons) * (
			Mathf.Cosh((-SpanWeightNewtons / H) * (xMid - (LengthMeters / 2))) -
			Mathf.Cosh((SpanWeightNewtons * LengthMeters) / (2 * H))
		);
	}

	private void ComputeForces()
	{
		V = 0.5f * (LCat * SpanWeightNewtons);
		TMax = Mathf.Sqrt(H * H + V * V);
	}

	public override void _Draw()
	{
		Color green = new Color(0, 1, 0);
		Color blue = new Color(.6f, .6f, 1);
		Color yellow = new Color(1, 1, 0);
		Color white = new Color(1, 1, 1);
		Color red = new Color(1, 0, 0);

		for (int i = 0; i < vertices.Count - 1; i++)
		{
			DrawLine(vertices[i] * PixelsPerMeter, vertices[i + 1] * PixelsPerMeter, white, 2f);
		}
		
		foreach (var vertex in vertices)
		{
			DrawCircle(vertex * PixelsPerMeter, 3f, red);
		}

		Vector2 frstPoint = vertices[0] * PixelsPerMeter;
		Vector2 midPoint = vertices[vertices.Count / 2] * PixelsPerMeter;
		Vector2 lastPoint = vertices[vertices.Count - 1] * PixelsPerMeter;

		Vector2 H_Vector_px = new Vector2(75, 0);
		Vector2 V_Vector_px = new Vector2(0, -75);
		Vector2 T_Vector_px_1 = new Vector2(-75, -75);
		Vector2 T_Vector_px_2 = T_Vector_px_1;
		T_Vector_px_2.X *= -1;

		float proportion = V / H;
		H_Vector_px.Y *= proportion;
		V_Vector_px.Y *= proportion;
		T_Vector_px_1.Y *= proportion;
		T_Vector_px_2.Y *= proportion;

		DrawLine(frstPoint, frstPoint - H_Vector_px, green, 2f);
		DrawLine(lastPoint, lastPoint + H_Vector_px, green, 2f);
		DrawLine(frstPoint, frstPoint + V_Vector_px, blue, 2f);
		DrawLine(lastPoint, lastPoint + V_Vector_px, blue, 2f);
		DrawLine(frstPoint, frstPoint + T_Vector_px_1, yellow, 2f);
		DrawLine(lastPoint, lastPoint + T_Vector_px_2, yellow, 2f);
		
		Font font = ThemeDB.FallbackFont;
		int vertical_step = 32;
		int font_size = 25;
		DrawString(
			font, midPoint + new Vector2(0, vertical_step), 
			$"V: {V:F2} N", HorizontalAlignment.Left, -1, font_size, blue
		);
		DrawString(
			font, midPoint + new Vector2(0, vertical_step * 2), 
			$"H: {H:F2} N", HorizontalAlignment.Left, -1, font_size, green
		);
		DrawString(
			font, midPoint + new Vector2(0, vertical_step * 3), 
			$"TMax: {TMax:F2} N", HorizontalAlignment.Left, -1, font_size, yellow
		);
		DrawString(
			font, midPoint + new Vector2(0, vertical_step * 4), 
			$"Catenary Length: {LCat:F2} m", HorizontalAlignment.Left, -1, font_size, white
		);
		DrawString(
			font, midPoint + new Vector2(0, vertical_step * 5), 
			$"Max Displacement: {YMax:F2} m", HorizontalAlignment.Left, -1, font_size, red
		);
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}
}
