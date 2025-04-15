using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FEMLineNewer : FEMLine
{
	public FEMLineNewer(Color lineColor) : base(lineColor) {}

	public override string GetPlotName() {
		return "FEM Line Newer";
	}

	public override void RunFEMComputation(float nodeMass, Vector2[] meterPoints, float actualLength, List<(int nodeIndex, Vector2 force)> extraForces = null)
	{
		SetProgress(0.01f); // Start

		gamma = nodeMass * meterPoints.Length / actualLength;
		n = meterPoints.Length - 1;
		nDoF = 2 * (n + 1);

		if (n < 1)
			throw new InvalidOperationException("Number of cable segments must be at least 1.");

		SetProgress(0.05f); // Validated input

		nodes = (Vector2[])meterPoints.Clone();

		// Initialize arrays
		UG_FINAL = new double[nDoF, 0];
		FI_FINAL = new double[nDoF, 0];
		EXTFORCES = new double[nDoF, 0];
		MBRFORCES = new double[n, 0];
		Areas = Enumerable.Repeat(A, n).ToArray();
		P0 = Enumerable.Repeat(1.0, n).ToArray();
		forceVector = new double[nDoF, 1];
		lengths = new double[n];

		SetProgress(0.10f); // Initialized arrays

		// Build member connectivity
		members = new int[n][];
		for (int i = 0; i < n; i++)
			members[i] = new int[] { i + 1, i + 2 };

		restrainedIndex = new int[] { 0, 1, 2 * n, 2 * n + 1 };
		restrainedDoF = new int[] { 1, 2, 2 * n + 1, 2 * n + 2 };
		freeDoF = Enumerable.Range(2, 2 * n - 2).ToArray();

		SetProgress(0.15f); // Built connectivity and DOFs

		// Compute member lengths
		for (int z = 0; z < n; z++)
		{
			int i = members[z][0] - 1;
			int j = members[z][1] - 1;

			if (i < 0 || j >= nodes.Length)
				throw new IndexOutOfRangeException($"Invalid member reference: {i} to {j}");

			lengths[z] = (nodes[j] - nodes[i]).Length();
		}

		// Self-weight
		if (swt)
		{
			foreach (var m in members)
			{
				int i = m[0] - 1, j = m[1] - 1;
				double sw = lengths[Array.IndexOf(members, m)] * gamma * 9.81;
				double half = sw / 2;
				int iy = 2 * i + 1, jy = 2 * j + 1;

				forceVector[iy, 0] -= half;
				forceVector[jy, 0] -= half;
			}
		}

		// External forces
		if (extraForces != null)
		{
			foreach (var (node, force) in extraForces)
			{
				if (node < 0 || node >= nodes.Length) continue;
				forceVector[2 * node, 0] += force.X;
				forceVector[2 * node + 1, 0] += force.Y;
			}
		}

		SetProgress(0.20f); // Forces applied

		// Displacements
		UG = new double[nDoF, 1];
		TMs = CalculateTransMatrices(UG, nodes, members);
		F_pre = InitPretension(forceVector, members, TMs, P0);
		UG_inc = CloneColumn(UG);
		F_inc = CloneColumn(F_pre);

		SetProgress(0.25f); // Initial transformation and pretension

		// Convergence setup
		forceIncrement = new double[nDoF];
		maxForce = new double[nDoF];
		for (int i = 0; i < nDoF; i++)
		{
			maxForce[i] = forceVector[i, 0];
			forceIncrement[i] = forceVector[i, 0] / nForceIncrements;
			forceVector[i, 0] = forceIncrement[i];
		}

		SetProgress(0.30f); // Convergence initialized

		// Iterative solution
		int counter = 0, inc = 0;
		bool notConverged = true;

		while (notConverged && counter < 10000)
		{
			var Fi_total = SumColumns(F_inc);
			var UG_total = SumColumns(UG_inc);
			var F_imbalance = SubtractMatrices(forceVector, Fi_total);
			var Ks = BuildStructureStiffnessMatrix(UG_total);
			var UG_new = SolveDisplacements(Ks, F_imbalance);
			TMs = CalculateTransMatrices(UG_total, nodes, members);
			var F_new = UpdateInternalForceSystem(UG_new);

			UG_inc = AppendColumn(UG_inc, UG_new);
			F_inc = AppendColumn(F_inc, F_new);

			notConverged = TestForConvergence(counter, convThreshold, F_imbalance);
			counter++;

			// Progress during load increments
			double applied = forceVector.Cast<double>().Sum();
			double total = maxForce.Sum();
			if (total != 0)
				SetProgress(0.30f + 0.50f * (float)(applied / total));

			if (!notConverged)
			{
				inc++;
				UG_FINAL = AppendColumn(UG_FINAL, UG_total);
				FI_FINAL = AppendColumn(FI_FINAL, Fi_total);
				EXTFORCES = AppendColumn(EXTFORCES, forceVector);
				MBRFORCES = AppendColumn(MBRFORCES, ToColumnMatrix(CalculateMemberForces(FEMLine.GetLastColumn(UG_FINAL), members, nodes, lengths, P0, E, Areas)));

				if (forceVector.Cast<double>().Sum() < maxForce.Sum())
				{
					counter = 0;
					for (int i = 0; i < nDoF; i++)
						forceVector[i, 0] += forceIncrement[i];
					notConverged = true;
					UG_inc = CloneColumn(UG_total);
					F_inc = CloneColumn(Fi_total);
				}
			}
		}

		SetProgress(0.85f); // After computation

		// Final plot
		if (UG_FINAL.GetLength(0) >= 2 * (n + 1))
		{
			int last = UG_FINAL.GetLength(1) - 1;
			Vector2[] deformed = new Vector2[n + 1];
			for (int i = 0; i <= n; i++)
			{
				deformed[i] = new Vector2(
					nodes[i].X + (float)UG_FINAL[2 * i, last],
					nodes[i].Y + (float)UG_FINAL[2 * i + 1, last]
				);
			}

			var linePoints = new Vector2[deformed.Length];
			for (int i = 0; i < deformed.Length; i++) {
				linePoints[i] = Coordinator.MetersToWorld(deformed[i]);
			}
			CallDeferred("setDeformationPoints", linePoints);
		}

		SetProgress(0.95f); // Right before stats

		// Statistics
		var statsDict = new Godot.Collections.Dictionary<string, string>
		{
			{ "Force Increments", nForceIncrements.ToString() },
			{ "Converged Increments", (UG_FINAL.GetLength(1) - 1).ToString() },
			{ "Max Displacement", UG_FINAL.Cast<double>().Select(Math.Abs).Max().ToString("F3") + " m" },
			{ "Total Internal Force", FI_FINAL.Cast<double>().Sum().ToString("F2") + " N" },
			{ "Convergence Threshold", convThreshold.ToString() + " N" }
		};

		CallDeferred(nameof(postStatistics), statsDict);
		GD.Print($"{GetPlotName()} generation done");
		SetProgress(1f); // Done
	}

	private static double[,] CloneColumn(double[,] src)
	{
		int rows = src.GetLength(0);
		var result = new double[rows, 1];
		for (int i = 0; i < rows; i++)
			result[i, 0] = src[i, 0];
		return result;
	}
}