using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class MassSpringCable : Node2D, CablePlotter
{
	private const double TimeoutSeconds = 10.0;
	private string plotName;
	private Color lineColor;

	private float damping;
	private float stiffness;
	private float convergenceThreshold;
	private float massPerNode;
	private bool isReady = false;

	private Vector2[] positions;
	private Vector2[] velocities;
	private Vector2[] forces;

	private int segmentCount;
	private float[] restLengths;

	private Stopwatch realTimeStopwatch = new();
	private double totalProcessingTime;
	private bool converged;

	private Vector2 gravity = new(0, -9.81f);
	private bool isProcessing = false;

	private float maxVelocityEverSeen = 0f;
	private float lastFrameVelocity = 0f;
	
	public MassSpringCable(string plotName, Color lineColor)
	{
		this.plotName = plotName;
		this.lineColor = lineColor;
	}

	public override void _Ready()
	{
		isReady = true;
		InputControlNode.Instance.AddDoubleField("Spring Stiffness (N/m)", 30000.0, (double val) => stiffness = (float)val);
		InputControlNode.Instance.AddDoubleField("Spring Damping Factor", 0.1, (double val) => damping = (float)val);
		InputControlNode.Instance.AddDoubleField("Mass Spring Convergence Threshold (m/s)", 0.01, (double val) => convergenceThreshold = (float)val);
	}

	public bool GetHidden() {
		return !Visible;
	}

	public override void _Process(double delta)
	{
		if (!isProcessing || converged) return;

		// Safety: clamp max timestep
		delta = Math.Min(delta, 0.02);

		QueueRedraw();
		var sw = Stopwatch.StartNew();

		// Clear forces
		for (int i = 0; i < forces.Length; i++)
			forces[i] = Vector2.Zero;

		// Gravity
		for (int i = 0; i < positions.Length; i++)
			forces[i] += massPerNode * gravity;

		// Spring forces (with cap)
		const float maxForce = 1000f;
		for (int i = 0; i < segmentCount; i++)
		{
			Vector2 deltaPos = positions[i + 1] - positions[i];
			float currentLength = deltaPos.Length();
			Vector2 direction = deltaPos.Normalized();
			Vector2 springForce = stiffness * (currentLength - restLengths[i]) * -direction;

			if (springForce.LengthSquared() > maxForce * maxForce)
				springForce = springForce.Normalized() * maxForce;

			if (i > 0) forces[i] -= springForce;
			if (i < segmentCount) forces[i + 1] += springForce;
		}

		// Fixed endpoints
		forces[0] = Vector2.Zero;
		forces[segmentCount] = Vector2.Zero;

		// Integrate motion
		float maxVelocitySq = 0f;

		// Integrate motion using semi-implicit Euler (stable)
		for (int i = 1; i < segmentCount; i++)
		{
			Vector2 accel = forces[i] / massPerNode;
			velocities[i] += accel * (float)delta;
			positions[i] += velocities[i] * (float)delta;
			velocities[i] *= (1 - damping); // Damping after position update

			// NaN/Inf protection
			if (!IsValid(positions[i]))
			{
				GD.PrintErr("MassSpring diverged. Aborting.");
				isProcessing = false;
				converged = true;
				return;
			}

			maxVelocitySq = Mathf.Max(maxVelocitySq, velocities[i].LengthSquared());
		}

		lastFrameVelocity = Mathf.Sqrt(maxVelocitySq);
		maxVelocityEverSeen = Mathf.Max(maxVelocityEverSeen, lastFrameVelocity);

		sw.Stop();
		totalProcessingTime += sw.Elapsed.TotalSeconds;

		if (maxVelocitySq < convergenceThreshold * convergenceThreshold)
		{
			isProcessing = false;
			converged = true;
			realTimeStopwatch.Stop();

			var statsDict = new Godot.Collections.Dictionary<string, string>
			{
				{ "Max Displacement", positions.Max(p => p.Length()).ToString("F3") + " m" },
				{ "Total Internal Force", forces.Sum(f => f.Length()).ToString("F2") + " N" },
				{ "Real Time to Stability", realTimeStopwatch.Elapsed.TotalSeconds.ToString("F4") + " s" },
				{ "Total Processing Time", totalProcessingTime.ToString("F4") + " s" }
			};

			InputControlNode.Instance.StatisticsCallback(this, statsDict);
			GD.Print("MassSpring generated");
		}

		if (realTimeStopwatch.Elapsed.TotalSeconds > TimeoutSeconds)
		{
			isProcessing = false;
			converged = true;
			realTimeStopwatch.Stop();

			InputControlNode.Instance.ShowAlert("Timeout",
				$"{GetPlotName()} simulation timed out after {TimeoutSeconds} seconds.\n\n" +
				"Try lowering the stiffness value for lighter or less rigid cables.");

			return;
		}
	}

	private bool IsValid(Vector2 v)
	{
		return !(float.IsNaN(v.X) || float.IsInfinity(v.X) || float.IsNaN(v.Y) || float.IsInfinity(v.Y));
	}

	public override void _Draw()
	{
		for (int i = 0; i < segmentCount; i++)
		{
			DrawLine(Coordinator.MetersToWorld(positions[i]), Coordinator.MetersToWorld(positions[i + 1]), lineColor, 4f);
			DrawCircle(Coordinator.MetersToWorld(positions[i]),     2f, lineColor);
			DrawCircle(Coordinator.MetersToWorld(positions[i + 1]), 2f, lineColor);
		}
	}

	public void Generate(float nodeMass, Vector2[] meterPoints, float actualLength, List<(int nodeIndex, Vector2 force)> extraForces = null)
	{
		if (!isReady)
		{
			Ready += () => Generate(nodeMass, meterPoints, actualLength, extraForces);
			return;
		}

		try
		{
			segmentCount = meterPoints.Length - 1;
			massPerNode = nodeMass;

			positions = (Vector2[])meterPoints.Clone();
			velocities = new Vector2[positions.Length];
			forces = new Vector2[positions.Length];

			restLengths = new float[segmentCount];
			for (int i = 0; i < segmentCount; i++)
				restLengths[i] = positions[i].DistanceTo(positions[i + 1]);

			realTimeStopwatch.Restart();
			totalProcessingTime = 0;
			isProcessing = true;
			converged = false;
			maxVelocityEverSeen = 0f;
			lastFrameVelocity = 0f;
		}
		catch (Exception ex)
		{
			InputControlNode.Instance.ShowAlert("Error", $"{GetPlotName()} generation failed: {ex.Message}");
		}
	}


	private float PredictProgress(float vNow, float vMax)
	{
		if (vNow < convergenceThreshold)
			return 1f;
		if (vNow >= vMax || vMax <= 0f)
			return 0f;

		float norm = Mathf.Clamp(vNow / vMax, 0.0001f, 1f);
		float logRatio = Mathf.Log(norm);
		float logThreshold = Mathf.Log(convergenceThreshold / vMax);

		return Mathf.Clamp(logRatio / logThreshold, 0f, 1f);
	}

	public float GetProgress()
	{
		if (converged) return 1f;
		if (!isProcessing) return 0f;
		return PredictProgress(lastFrameVelocity, maxVelocityEverSeen);
	}

	public Vector2[] GetFinalPoints()
	{
		if (!converged)
			throw new InvalidOperationException($"{GetPlotName()} has not finished computing. Final points are not available yet.");

		return (Vector2[])positions.Clone();
	}

	public void HidePlot() => Hide();
	public void ShowPlot() => Show();
	public string GetPlotName() => plotName;
	public Color GetColor() => lineColor;
}
