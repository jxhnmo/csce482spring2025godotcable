using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics.LinearAlgebra;

public partial class FEMCable : Node2D
{

private double E, A, gamma;
	private bool swt;
	private List<double> Areas;
	private List<double> P0;
	private double P;
	private string pointLoadAxis;
	private int nForceIncrements;
	private double convThreshold;
	bool reactionsFlag;


	private double[,] nodes;
	private int[,] members;
	private int[] restrainedIndex;
	private int[] freeDoF;
	private int[] restrainedDoF;
	private int[,] forceLocationData;
	private int nDoF;
	private double[] lengths;
	private Vector<Double> forceVector;
	private Vector<Double> forceVectorInitial;
	private Matrix<double>[] TMs;
	private List<Tuple<int, double>> SW_at_supports;
	private float currentLoadStepProgress = 1.0f;
    private float ppmMultiplier = 1.0f;
	private String[] dataLocations = {
		"Lecture 41/data/", 
		"Lecture 41/data - 3 bar/", 
		"Lecture 41/data - 6 bar/"
	};

	[Export] public System.String PathPrefix = "Lecture 41/data/";
	[Export] public float PixelsPerMeter = 200.0f;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void LoadStructure()
	{
		// Declare variables
		reactionsFlag = false;
		nodes = null;
		members = null;
		restrainedIndex = null;
		freeDoF = null;
		forceLocationData = null;
		nDoF = 0;

		// Load nodal coordinates
		if (File.Exists(PathPrefix + "Vertices.csv"))
		{
			nodes = ReadCsvAsDouble(PathPrefix + "Vertices.csv");
			GD.Print("1. 🟢 Vertices.csv imported");
			GD.Print(nodes);
		}
		else
		{
			GD.Print("1. 🛑 STOP: Vertices.csv not found");
		}

		// Load member definitions
		if (File.Exists(PathPrefix + "Edges.csv"))
		{
			members = ReadCsvAsInt(PathPrefix + "Edges.csv");
			nDoF = members.Cast<int>().Max() * 2; // Total degrees of freedom
			GD.Print("2. 🟢 Edges.csv imported");
		}
		else
		{
			GD.Print("2. 🛑 STOP: Edges.csv not found");
		}

		// Load restraint data
		if (File.Exists(PathPrefix + "Restraint-Data.csv"))
		{
			int[,] restraintData = ReadCsvAsInt(PathPrefix + "Restraint-Data.csv");
			List<int> flatData = restraintData.Cast<int>().Where(x => x != 0).ToList();
			restrainedDoF = flatData.ToArray(); 
			restrainedIndex = flatData.Select(x => x - 1).ToArray();
			freeDoF = Enumerable.Range(0, nDoF).Except(restrainedIndex).ToArray();
			GD.Print("3. 🟢 Restraint-Data.csv imported");
			GD.Print($"     restrainedDoF: [{restrainedDoF.Join(", ")}]");
		}
		else
		{
			GD.Print("3. 🛑 STOP: Restraint-Data.csv not found");
		}

		// Load force location data
		if (File.Exists(PathPrefix + "Force-Data.csv"))
		{
			forceLocationData = ReadCsvAsInt(PathPrefix + "Force-Data.csv");
			GD.Print("4. 🟢 Force-Data.csv imported");
		}
		else
		{
			forceLocationData = new int[0, 0]; // Empty array
			GD.Print("4. ⚠️ Force-Data.csv not found");
		}
	}

	static double[,] ReadCsvAsDouble(string path)
	{
		string[] lines = File.ReadAllLines(path);
		int rows = lines.Length;
		int cols = lines[0].Split(',').Length;
		double[,] data = new double[rows, cols];

		for (int i = 0; i < rows; i++)
		{
			double[] values = lines[i].Split(',').Select(double.Parse).ToArray();
			for (int j = 0; j < cols; j++)
			{
				data[i, j] = values[j];
			}
		}
		return data;
	}

	// Helper function: Read CSV and convert to int array
	static int[,] ReadCsvAsInt(string path)
	{
		string[] lines = File.ReadAllLines(path);
		int rows = lines.Length;
		int cols = lines[0].Split(',').Length;
		int[,] data = new int[rows, cols];

		for (int i = 0; i < rows; i++)
		{
			int[] values = lines[i].Split(',').Select(int.Parse).ToArray();
			for (int j = 0; j < cols; j++)
			{
				data[i, j] = values[j];
			}
		}
		return data;
	}

	private void Initialize()
	{
		LoadStructure();
		
		E = 70 * Math.Pow(10, 9);  // (N/m^2)
		A = 0.0025;  // (m^2)
		gamma = .1;  // (kg/m) Cable mass per unit length
		swt = true;  // Self-weight into account flag

		int numMembers = (members != null) ? members.GetLength(0) : 0;
		Areas = Enumerable.Repeat(A, numMembers).ToList();  // An array for individual area per member
		P0 = Enumerable.Repeat(1000.0, numMembers).ToList();  // Pre-tension values

		P = -10000;  // (N) Point load magnitude
		pointLoadAxis = "y";  // Global axis for point loads

		nForceIncrements = 1500;  // Number of force increments
		convThreshold = 100;  // (N) Threshold for convergence

		GD.Print($"E: {E}, A: {A}, gamma: {gamma}, swt: {swt}");
		GD.Print($"Point Load: {P} along {pointLoadAxis}");

		//### Add point loads to global force factor ###//
		int totalDoF = nodes.GetLength(0) * 2; // Total degrees of freedom
		forceVector = Vector<double>.Build.Dense(totalDoF, 0); // Zero vector

		if (forceLocationData.GetLength(0) > 0) // Check if there's data
		{
			int columnIndex = (pointLoadAxis == "x") ? 1 : 2; // Select column based on axis
			int[] forceIndices = Enumerable.Range(0, forceLocationData.GetLength(0))
										   .Select(i => forceLocationData[i, columnIndex])
										   .ToArray();

			foreach (int index in forceIndices)
				if (index >= 0 && index < forceVector.Count)
					forceVector[index] = P; // Apply force
		}
		

		//#### Calculate inital length for each member based on initial position. ####//
		lengths = new double[members.GetLength(0)];
		for (int n = 0; n < members.GetLength(0); n++)
		{
			int node_i = members[n, 0] - 1;
			int node_j = members[n, 1] - 1;
			double ix = nodes[node_i, 0], iy = nodes[node_i, 1];
			double jx = nodes[node_j, 0], jy = nodes[node_j, 1];
			double dx = jx - ix;
			double dy = jy - iy;
			lengths[n] = Math.Sqrt(dx * dx + dy * dy);
		}

		GD.Print("# of Members: " + members.GetLength(0));
		GD.Print("Member Lengths: " + string.Join(", ", lengths));

		//#### Calculate and add self-weight to force vector ####//
		SW_at_supports = new List<Tuple<int, double>>();
		if (swt)
		{
			for (int n = 0; n < members.GetLength(0); n++)
			{
				int node_i = members[n, 0];
				int node_j = members[n, 1];
				double length = lengths[n];

				double sw = length * gamma * 9.81; // (N) Self-weight
				double F_node = sw / 2;

				int iy = 2 * node_i - 1;
				int jy = 2 * node_j - 1;

				forceVector[iy] -= F_node;
				forceVector[jy] -= F_node;
				if (restrainedDoF.Contains(iy + 1)) {
					SW_at_supports.Add(Tuple.Create(iy, F_node));
				}

				if (restrainedDoF.Contains(jy + 1)) {
					SW_at_supports.Add(Tuple.Create(jy, F_node));
				}
			}
		}

		forceVectorInitial = forceVector.Clone();
		GD.Print("Force Vector: " + forceVector);
		GD.Print("SW at Supports:");
		foreach (var sw in SW_at_supports)
			GD.Print($"DoF: {sw.Item1}, Force: {sw.Item2}");



		// ////////////////////////////////////
		// /// Test Code For Element Stiffness
		// ////////////////////////////////////
		
		// Vector<double> UG = Vector<double>.Build.Dense(nDoF, 0.0);

		// // Call the function on member 0
		// TMs = CalculateTransMatrices(UG);
		// List<Matrix<double>> kQuads = BuildElementStiffnessMatrix(0, UG);

		// // Extract quadrants
		// Matrix<double> K11 = kQuads[0];
		// Matrix<double> K12 = kQuads[1];
		// Matrix<double> K21 = kQuads[2];
		// Matrix<double> K22 = kQuads[3];

		// // Output the results
		// GD.Print("K11:\n" + K11.ToMatrixString());
		// GD.Print("K12:\n" + K12.ToMatrixString());
		// GD.Print("K21:\n" + K21.ToMatrixString());
		// GD.Print("K22:\n" + K22.ToMatrixString());

		// Matrix<double> Ks = BuildStructureStiffnessMatrix(UG);

		// Output the reduced structure stiffness matrix
		// GD.Print("Reduced structure stiffness matrix Ks:\n" + Ks.ToMatrixString());

		RunIncrementalLoadConvergence();
	}


	private void DrawInitial() 
	{
		Color nodeColor = Colors.Red;
		Color memberColor = Colors.White;
		Color forceColor = Colors.Red;
		float forceThreshold = 0.5f;

		int numNodes = nodes.GetLength(0);
		int numMembers = members.GetLength(0);

		// === Draw Members (Undeformed) ===
		for (int i = 0; i < numMembers; i++)
		{
			int node_i = members[i, 0] - 1;
			int node_j = members[i, 1] - 1;

			Vector2 start = new Vector2((float)nodes[node_i, 0], -(float)nodes[node_i, 1]) * PixelsPerMeter * ppmMultiplier;
			Vector2 end = new Vector2((float)nodes[node_j, 0], -(float)nodes[node_j, 1]) * PixelsPerMeter * ppmMultiplier;

			DrawLine(start, end, memberColor, 2f, true);
		}

		// === Compute Max Force Magnitude for Normalization ===
		float maxForceMag = 0;
		for (int nodeIndex = 0; nodeIndex < numNodes; nodeIndex++)
		{
			float forceX = (float)forceVectorInitial[nodeIndex * 2];
			float forceY = (float)forceVectorInitial[nodeIndex * 2 + 1];
			float forceMag = MathF.Sqrt(forceX * forceX + forceY * forceY);
			maxForceMag = MathF.Max(maxForceMag, forceMag);
		}

		float maxArrowLength = 50f; // Define max arrow length for visualization

		// === Draw External Forces (Green) ===
		for (int nodeIndex = 0; nodeIndex < numNodes; nodeIndex++)
		{
			float forceX = (float)forceVectorInitial[nodeIndex * 2];
			float forceY = (float)forceVectorInitial[nodeIndex * 2 + 1];
			float forceMag = MathF.Sqrt(forceX * forceX + forceY * forceY);

			if (forceMag > forceThreshold)
			{
				Vector2 basePos = new Vector2((float)nodes[nodeIndex, 0], -(float)nodes[nodeIndex, 1]) * PixelsPerMeter * ppmMultiplier;
				Vector2 forceVec = new Vector2(forceX, -forceY);

				if (forceMag > 1e-6) // Normalize force arrow length
				{
					forceVec = forceVec.Normalized() * (forceMag / maxForceMag * maxArrowLength);
				}

				Vector2 endPos = basePos + forceVec;
				DrawLine(basePos, endPos, Colors.Green, 2f);
				ForceLabel(endPos, forceMag, Colors.Green);
			}
		}

		// === Draw Nodes with Restraint Markers ===
		for (int i = 0; i < numNodes; i++)
		{
			Vector2 pos = new Vector2((float)nodes[i, 0], -(float)nodes[i, 1]) * PixelsPerMeter * ppmMultiplier;

			int i_hor = i * 2;
			int i_ver = i * 2 + 1;
			bool restrainedX = restrainedIndex.Contains(i_hor);
			bool restrainedY = restrainedIndex.Contains(i_ver);
			
			if (restrainedX) {
				DrawLine(pos + new Vector2(-25, 0), pos + new Vector2(25, 0), Colors.Black, 3f);
			}
			if (restrainedY) {
				DrawLine(pos + new Vector2(0, -25), pos + new Vector2(0, 25), Colors.Black, 3f);
			}

			DrawCircle(pos, 4f, nodeColor);
		}
	}

	

	private void ForceLabel(Vector2 location, float newtons, Color color)
	{
		float kN = newtons / 1000.0f;

		string forceText = $"{Math.Round(kN, 2)} kN";

		Vector2 textOffset = new Vector2(10, -10);

		int fontSize = 25;

		DrawString(
			ThemeDB.FallbackFont, 
			location + textOffset, 
			forceText, 
			HorizontalAlignment.Left, 
			-1, 
			fontSize, 
			color
		);
	}

	// Untested
	private static Matrix<double> CalculateTransMatrix(double ix, double iy, double jx, double jy)
	{
		double dx = jx - ix;
		double dy = jy - iy;
		double length = Math.Sqrt(dx * dx + dy * dy);

		if (length == 0)
			throw new ArgumentException("Node I and Node J cannot occupy the same position.");

		double lp = dx / length;
		double mp = dy / length;
		double lq = -mp;
		double mq = lp;
		return Matrix<double>.Build.DenseOfArray(new double[,]
		{
			{ -lp, -mp, lp, mp },
			{ -lq, -mq, lq, mq }
		});
	}


	// Untested
	private Matrix<double>[] CalculateTransMatrices(Vector<double> UG)
	{
		int numMembers = members.GetLength(0);
		Matrix<double>[] transformationMatrices = new Matrix<double>[numMembers];

		for (int n = 0; n < numMembers; n++)
		{
			int node_i = members[n, 0];
			int node_j = members[n, 1];

			// DOF indices (0-based)
			int ia = 2 * (node_i - 1);     // x DOF for node_i
			int ib = 2 * (node_i - 1) + 1; // y DOF for node_i
			int ja = 2 * (node_j - 1);     // x DOF for node_j
			int jb = 2 * (node_j - 1) + 1; // y DOF for node_j

			// Current positions = original + displacement
			double ix = nodes[node_i - 1, 0] + UG[ia];
			double iy = nodes[node_i - 1, 1] + UG[ib];
			double jx = nodes[node_j - 1, 0] + UG[ja];
			double jy = nodes[node_j - 1, 1] + UG[jb];

			// Recalculate transformation matrix for this member
			Matrix<double> TM = CalculateTransMatrix(ix, iy, jx, jy);

			// Store the matrix
			transformationMatrices[n] = TM;
		}

		return transformationMatrices;
	}


	// Untested
	private Vector<double> InitPretension()
	{
		// Initialize internal force vector
		var F_pre = Vector<double>.Build.Dense(forceVector.Count, 0.0);

		for (int n = 0; n < members.GetLength(0); n++)
		{
			int node_i = members[n, 0];
			int node_j = members[n, 1];

			// Global DOF indices for this member
			int ia = 2 * (node_i - 1);     // horizontal DoF at node i
			int ib = 2 * (node_i - 1) + 1; // vertical DoF at node i
			int ja = 2 * (node_j - 1);     // horizontal DoF at node j
			int jb = 2 * (node_j - 1) + 1; // vertical DoF at node j

			// Transformation matrix TMs[n] should be a 2x4 matrix
			Matrix<double> TM = TMs[n]; // Assumes TMs is Matrix<double>[] (2x4 each)

			// Axial direction in local coords
			var AAp = Vector<double>.Build.DenseOfArray(new double[] { 1.0, 0.0 });

			double P = P0[n]; // Axial pre-tension force for member n

			// F_pre_global = TM^T * AAp * P
			Vector<double> F_pre_global = TM.TransposeThisAndMultiply(AAp) * P;

			// Add member contribution to global force vector
			F_pre[ia] += F_pre_global[0];
			F_pre[ib] += F_pre_global[1];
			F_pre[ja] += F_pre_global[2];
			F_pre[jb] += F_pre_global[3];
		}

		return F_pre;
	}

	private List<Matrix<double>> BuildElementStiffnessMatrix(int n, Vector<double> UG)
	{
		// Get node indices
		int node_i = members[n, 0];
		int node_j = members[n, 1];

		// DOF indices (0-based)
		int ia = 2 * (node_i - 1);
		int ib = 2 * (node_i - 1) + 1;
		int ja = 2 * (node_j - 1);
		int jb = 2 * (node_j - 1) + 1;

		// Displacements
		double d_ix = UG[ia];
		double d_iy = UG[ib];
		double d_jx = UG[ja];
		double d_jy = UG[jb];

		// Transformation matrix for this member (2x4)
		Matrix<double> TM = TMs[n];

		// Local displacement vector [d_ix, d_iy, d_jx, d_jy]^T
		Vector<double> globalDisp = Vector<double>.Build.DenseOfArray(new double[] { d_ix, d_iy, d_jx, d_jy });

		// localDisp = TM * globalDisp
		Vector<double> localDisp = TM.Multiply(globalDisp);
		double u = localDisp[0];
		double v = localDisp[1];

		// Extension e
		double Lo = lengths[n];
		double e = Math.Sqrt(Math.Pow(Lo + u, 2) + v * v) - Lo;

		// Matrix [AA]
		double a1 = (Lo + u) / (Lo + e);
		double a2 = v / (Lo + e);
		Matrix<double> AA = Matrix<double>.Build.DenseOfArray(new double[,] { { a1, a2 } });

		// Axial force P
		double A = Areas[n];
		double P = P0[n] + (E * A / Lo) * e;

		// Matrix [d]
		double d11 = P * v * v;
		double d12 = -P * v * (Lo + u);
		double d21 = d12;
		double d22 = P * Math.Pow(Lo + u, 2);
		double denominator = Math.Pow(Lo + e, 3);
		Matrix<double> d = Matrix<double>.Build.DenseOfArray(new double[,]
		{
			{ d11, d12 },
			{ d21, d22 }
		}) / denominator;

		// Nonlinear axial stiffness matrix: [AA]^T * EA/Lo * [AA] + d
		Matrix<double> NL = AA.TransposeThisAndMultiply(AA) * (E * A / Lo) + d;

		// Full element stiffness matrix in global coords: TM^T * NL * TM
		Matrix<double> k = TM.TransposeThisAndMultiply(NL.Multiply(TM));

		// Extract quadrants
		Matrix<double> K11 = k.SubMatrix(0, 2, 0, 2);
		Matrix<double> K12 = k.SubMatrix(0, 2, 2, 2);
		Matrix<double> K21 = k.SubMatrix(2, 2, 0, 2);
		Matrix<double> K22 = k.SubMatrix(2, 2, 2, 2);

		return new List<Matrix<double>> { K11, K12, K21, K22 };
	}


	private Matrix<double> BuildStructureStiffnessMatrix(Vector<double> UG)
	{
		// Initialize the global primary stiffness matrix
		var Kp = Matrix<double>.Build.Dense(nDoF, nDoF, 0.0);

		for (int n = 0; n < members.GetLength(0); n++)
		{
			int node_i = members[n, 0];
			int node_j = members[n, 1];

			// Compute nonlinear element stiffness matrix quadrants
			var elementStiffness = BuildElementStiffnessMatrix(n, UG);
			var K11 = elementStiffness[0];
			var K12 = elementStiffness[1];
			var K21 = elementStiffness[2];
			var K22 = elementStiffness[3];

			// Indices of degrees of freedom
			int ia = 2 * (node_i - 1);
			int ib = 2 * (node_i - 1) + 1;
			int ja = 2 * (node_j - 1);
			int jb = 2 * (node_j - 1) + 1;

			// Add element stiffness matrix into global stiffness matrix
			Kp.SetSubMatrix(ia, 2, ia, 2, Kp.SubMatrix(ia, 2, ia, 2).Add(K11));
			Kp.SetSubMatrix(ia, 2, ja, 2, Kp.SubMatrix(ia, 2, ja, 2).Add(K12));
			Kp.SetSubMatrix(ja, 2, ia, 2, Kp.SubMatrix(ja, 2, ia, 2).Add(K21));
			Kp.SetSubMatrix(ja, 2, ja, 2, Kp.SubMatrix(ja, 2, ja, 2).Add(K22));
		}

		// Reduce to structure stiffness matrix by removing rows & columns for restrained DoFs
		var freeIndex = Enumerable.Range(0, nDoF).Except(restrainedIndex).ToArray();
		var Ks = Matrix<double>.Build.Dense(freeIndex.Length, freeIndex.Length);

		for (int i = 0; i < freeIndex.Length; i++)
		{
			for (int j = 0; j < freeIndex.Length; j++)
			{
				Ks[i, j] = Kp[freeIndex[i], freeIndex[j]];
			}
		}

		return Ks;
	}


	// Untested
	private Vector<double> SolveDisplacements(Matrix<double> Ks, Vector<double> F_inequilibrium)
	{
		// Remove entries at restrained DOFs to form reduced force vector
		var freeIndex = Enumerable.Range(0, nDoF).Except(restrainedIndex).ToArray();
		var forceVectorRed = Vector<double>.Build.Dense(freeIndex.Length);

		for (int i = 0; i < freeIndex.Length; i++)
		{
			forceVectorRed[i] = F_inequilibrium[freeIndex[i]];
		}

		// Solve the system: Ks * U = F
		Vector<double> U = Ks.Solve(forceVectorRed);

		// Build the global displacement vector UG
		var UG = Vector<double>.Build.Dense(nDoF);
		int c = 0;
		for (int i = 0; i < nDoF; i++)
		{
			if (restrainedIndex.Contains(i))
			{
				UG[i] = 0.0; // Restrained DOF
			}
			else
			{
				UG[i] = U[c]; // Free DOF
				c++;
			}
		}

		return UG;
	}


	// Untested
	private Vector<double> UpdateInternalForceSystem(Vector<double> UG)
	{
		// Initialize internal force vector
		var F_int = Vector<double>.Build.Dense(nDoF, 0.0);

		for (int n = 0; n < members.GetLength(0); n++)
		{
			int node_i = members[n, 0];
			int node_j = members[n, 1];

			// DOF indices (0-based)
			int ia = 2 * (node_i - 1);
			int ib = ia + 1;
			int ja = 2 * (node_j - 1);
			int jb = ja + 1;

			// Displacements
			double d_ix = UG[ia];
			double d_iy = UG[ib];
			double d_jx = UG[ja];
			double d_jy = UG[jb];

			// Current transformation matrix (2x4)
			Matrix<double> TM = TMs[n];

			// Compute local displacements: TM * [d_ix, d_iy, d_jx, d_jy]^T
			var globalDisp = Vector<double>.Build.DenseOfArray(new double[] { d_ix, d_iy, d_jx, d_jy });
			Vector<double> localDisp = TM * globalDisp;
			double u = localDisp[0];
			double v = localDisp[1];

			// Extension e
			double Lo = lengths[n];
			double e = Math.Sqrt(Math.Pow(Lo + u, 2) + v * v) - Lo;

			// Local direction matrix [AA]
			double a1 = (Lo + u) / (Lo + e);
			double a2 = v / (Lo + e);
			Matrix<double> AA = Matrix<double>.Build.DenseOfArray(new double[,] { { a1, a2 } });

			// Axial force due to displacement
			double A = Areas[n];
			double P = (E * A / Lo) * e;

			// Global force vector: TM^T * AA^T * P
			Vector<double> F_global = (TM.TransposeThisAndMultiply(AA.Transpose()) * P).Column(0);

			// Assemble into global internal force vector
			F_int[ia] += F_global[0];
			F_int[ib] += F_global[1];
			F_int[ja] += F_global[2];
			F_int[jb] += F_global[3];
		}

		return F_int;
	}


	// Untested
	private (bool notConverged, double maxIneq) TestForConvergence(int it, double threshold, Vector<double> F_inequilibrium)
	{
		bool notConverged = true;
		double maxIneq = 0.0;

		if (it > 0)
		{
			// Select entries from F_inequilibrium corresponding to free DOFs
			double[] values = freeDoF.Select(index => Math.Abs(F_inequilibrium[index])).ToArray();

			maxIneq = values.Length > 0 ? values.Max() : 0.0;

			if (maxIneq < threshold)
			{
				notConverged = false;
			}
		}

		return (notConverged, maxIneq);
	}


	// Untested
	private double[] CalculateMbrForces(Vector<double> UG)
	{
		int numMembers = members.GetLength(0);
		double[] mbrForces = new double[numMembers];

		for (int n = 0; n < numMembers; n++)
		{
			int node_i = members[n, 0];
			int node_j = members[n, 1];

			// DOF indices (0-based)
			int ia = 2 * (node_i - 1);
			int ib = ia + 1;
			int ja = 2 * (node_j - 1);
			int jb = ja + 1;

			// Updated node positions = original + displacement
			double ix = nodes[node_i - 1, 0] + UG[ia];
			double iy = nodes[node_i - 1, 1] + UG[ib];
			double jx = nodes[node_j - 1, 0] + UG[ja];
			double jy = nodes[node_j - 1, 1] + UG[jb];

			// Compute new length
			double dx = jx - ix;
			double dy = jy - iy;
			double newLength = Math.Sqrt(dx * dx + dy * dy);

			// Change in length
			double deltaL = newLength - lengths[n];

			// Axial force = pre-tension + (EA/L) * ΔL
			double force = P0[n] + (deltaL * E * Areas[n] / lengths[n]);
			mbrForces[n] = force;
		}

		return mbrForces;
	}


	private Matrix<double> UG_FINAL;
	private Matrix<double> FI_FINAL;
	private Matrix<double> EXTFORCES;
	private Matrix<double> MBRFORCES;

    private void RunIncrementalLoadConvergence()
	{
		GD.Print("Attempting RunIncrementalLoadConvergence() {");
		// === Initialise data containers to hold converged data for each external load increment ===
		// Container for global displacements at each load increment (nDoF × 0)
		UG_FINAL = Matrix<double>.Build.Dense(nDoF, 0);
		// Container for internal force vectors at each load increment
		FI_FINAL = Matrix<double>.Build.Dense(nDoF, 0);
		// Container for external force vectors at each load increment
		EXTFORCES = Matrix<double>.Build.Dense(nDoF, 0);
		// Container for axial member forces at each load increment
		MBRFORCES = Matrix<double>.Build.Dense(members.GetLength(0), 0);

		// === Initialise data containers used within the convergence loop ===
		// Initialise global displacement vector to zero (undeformed state)
		var UG = Vector<double>.Build.Dense(nDoF, 0.0);
		// Calculate initial transformation matrices based on undeformed shape
		TMs = CalculateTransMatrices(UG);  // TMs is Matrix<double>[]
		// Internal force system due to pretension
		var F_pre = InitPretension(); // Vector<double>
		// Container for incremental displacements per iteration
		var UG_inc = Matrix<double>.Build.Dense(nDoF, 0).Append(UG.ToColumnMatrix());
		// Container for incremental internal forces per iteration
		var F_inc = Matrix<double>.Build.Dense(nDoF, 0).Append(F_pre.ToColumnMatrix());

		// === Break up the external force vector into increments ===
		// Determine the force increment for each convergence test
		var forceIncrement = forceVector.Divide(nForceIncrements);
		// Store the total target force vector
		var maxForce = forceVector.Clone();
		// Start forceVector at just the first increment
		forceVector = forceIncrement.Clone();

		// --- Remnant things? --- 
		// Initialized arbitrarily. To be overwritten. - Thomas
		Vector<double> F_inequilibrium = Vector<double>.Build.Dense(1, 0.0);
		double[] mbrForces = new double[1];


		// Iteration and load increment counters
		int i = 0;
		int inc = 0;
		bool notConverged = true;

		while (notConverged && i < 10000)
		{
			// Sum incremental internal forces and displacements
			Vector<double> Fi_total = F_inc.RowSums();
			Vector<double> UG_total = UG_inc.RowSums();

			// Calculate equilibrium imbalance (external - internal)
			F_inequilibrium = forceVector - Fi_total;

			// Build stiffness matrix based on deformed geometry
			var Ks = BuildStructureStiffnessMatrix(UG_total);

			// Solve for incremental displacements
			UG = SolveDisplacements(Ks, F_inequilibrium);

			// Update transformation matrices based on total displacement
			TMs = CalculateTransMatrices(UG_total);

			// Calculate internal forces due to new incremental displacements
			var F_int = UpdateInternalForceSystem(UG);

			// Append new increments to running matrices
			UG_inc = UG_inc.Append(UG.ToColumnMatrix());
			F_inc = F_inc.Append(F_int.ToColumnMatrix());

			// Test for convergence
			(notConverged, double maxIneq) = TestForConvergence(i, convThreshold, F_inequilibrium);
			i++;

			// If converged for this load increment
			if (!notConverged)
			{
				inc++;
				GD.Print($"System has converged for load increment {inc} after {i - 1} iterations");

				// Save converged displacements and forces
				UG_FINAL = UG_FINAL.Append(UG_total.ToColumnMatrix());
				FI_FINAL = FI_FINAL.Append(Fi_total.ToColumnMatrix());

				// Prepare increment containers for next load step
				UG_inc = Matrix<double>.Build.Dense(nDoF, 0).Append(UG_total.ToColumnMatrix());
				F_inc = Matrix<double>.Build.Dense(nDoF, 0).Append(Fi_total.ToColumnMatrix());

				// Calculate member axial forces
				mbrForces = CalculateMbrForces(UG_total);
				MBRFORCES = MBRFORCES.Append(Vector<double>.Build.DenseOfArray(mbrForces).ToColumnMatrix());

				// Record external force for this increment
				EXTFORCES = EXTFORCES.Append(forceVector.ToColumnMatrix());

				// Determine if there’s more load to apply
				if (Math.Abs(forceVector.Sum()) < Math.Abs(maxForce.Sum()))
				{
					i = 0;
					forceVector += forceIncrement;
					notConverged = true;
				}
			}
		}

		GD.Print("}");


		GD.Print("OUTSTANDING FORCE IMBALANCE");
		for (i = 0; i < nDoF; i++)
		{
			if (!restrainedIndex.Contains(i))
			{
				double imbalance = F_inequilibrium[i] / 1000.0;
				GD.Print($"Remaining force imbalance at DoF {i} is {Math.Round(imbalance, 3)} kN");
			}
		}

		double maxInequality = freeDoF.Select(i => Math.Abs(F_inequilibrium[i]))
									.Max() / 1000.0;
		GD.Print($"(max = {Math.Round(maxInequality, 3)} kN)");
		GD.Print();

		GD.Print("REACTIONS");
		Vector<double> f_int = FI_FINAL.Column(FI_FINAL.ColumnCount - 1);
		for (i = 0; i < restrainedIndex.Length; i++)
		{
			int index = restrainedIndex[i];
			double reaction = f_int[index] / 1000.0;
			GD.Print($"Reaction at DoF {index + 1}: {Math.Round(reaction, 2)} kN");
		}

		GD.Print();
		GD.Print("MEMBER FORCES (incl. any pre-tension)");
		for (int n = 0; n < members.GetLength(0); n++)
		{
			int node_i = members[n, 0];
			int node_j = members[n, 1];
			double force = mbrForces[n] / 1000.0;
			GD.Print($"Force in member {n + 1} (nodes {node_i} to {node_j}) is {Math.Round(force, 2)} kN");
		}

		GD.Print();
		GD.Print("NODAL DISPLACEMENTS");
		Vector<double> ug = UG_FINAL.Column(UG_FINAL.ColumnCount - 1);
		for (int n = 0; n < nodes.GetLength(0); n++)
		{
			int ix = 2 * n;
			int iy = 2 * n + 1;
			double ux = Math.Round(ug[ix], 5);
			double uy = Math.Round(ug[iy], 5);
			GD.Print($"Node {n + 1}: Ux = {ux} m, Uy = {uy} m");
		}

		// SelfWeightSupportCorrection
		reactionsFlag = false;
		if (swt && SW_at_supports.Count > 0)
		{
			reactionsFlag = true;
			foreach (var SW in SW_at_supports)
			{
				int index = SW.Item1;
				double force = SW.Item2;
				for (int col = 0; col < FI_FINAL.ColumnCount; col++)
				{
					FI_FINAL[index, col] += force;
				}
			}
		}
	}


	private void DrawFinal()
	{
		int numNodes = nodes.GetLength(0);
		int numMembers = members.GetLength(0);

		// Pull the final increment data
		int targCol = (int)((UG_FINAL.ColumnCount - 1) * currentLoadStepProgress + 0.5f);
		Vector<double> ug = UG_FINAL.Column(targCol);
		Vector<double> fi = FI_FINAL.Column(targCol);
		Vector<double> extForces = EXTFORCES.Column(targCol);
		Vector<double> mbrForces = MBRFORCES.Column(targCol); // Assumes this is stored as a Matrix<double>

		double maxForceMag = extForces.Select(Math.Abs).Max();

		// === Draw Members ===
		for (int i = 0; i < numMembers; i++)
		{
			int nodeI = members[i, 0] - 1;
			int nodeJ = members[i, 1] - 1;

			// Original positions
			Vector2 posI = new Vector2((float)nodes[nodeI, 0], -(float)nodes[nodeI, 1]);
			Vector2 posJ = new Vector2((float)nodes[nodeJ, 0], -(float)nodes[nodeJ, 1]);

			// Displaced positions
			Vector2 dispI = new Vector2((float)ug[nodeI * 2], -(float)ug[nodeI * 2 + 1]) * PixelsPerMeter * ppmMultiplier;
			Vector2 dispJ = new Vector2((float)ug[nodeJ * 2], -(float)ug[nodeJ * 2 + 1]) * PixelsPerMeter * ppmMultiplier;

			Vector2 defI = (posI * PixelsPerMeter * ppmMultiplier) + dispI;
			Vector2 defJ = (posJ * PixelsPerMeter * ppmMultiplier) + dispJ;

			// Undeformed in dashed color
			DrawLine(posI * PixelsPerMeter * ppmMultiplier, posJ * PixelsPerMeter * ppmMultiplier, Colors.Gray, 1f, true);

			// Deformed member line colored by force
			float force = (float)mbrForces[i];
			Color memberColor = force < 0 ? Colors.Red : Colors.Blue;
			DrawLine(defI, defJ, memberColor, 3f, true);

			// Optional: Label axial force
			if (Math.Abs(force) > 1)
			{
				Vector2 mid = (defI + defJ) / 2f + new Vector2(10, -10);
				string forceText = $"{Math.Round(force / 1000.0, 2)} kN";
				DrawString(ThemeDB.FallbackFont, mid, forceText, HorizontalAlignment.Center, -1, 18, Colors.White);
			}
		}

		// === Draw Nodes, Forces, and Reactions ===
		for (int i = 0; i < numNodes; i++)
		{
			int iHor = i * 2;
			int iVer = i * 2 + 1;

			Vector2 basePos = new Vector2((float)nodes[i, 0], -(float)nodes[i, 1]) * PixelsPerMeter * ppmMultiplier;
			Vector2 disp = new Vector2((float)ug[iHor], -(float)ug[iVer]) * PixelsPerMeter * ppmMultiplier;
			Vector2 newPos = basePos + disp;

			// Draw node circle
			DrawCircle(newPos, 4f, Colors.Red);

			// Draw applied force (green)
			Vector2 appliedForce = new Vector2((float)extForces[iHor], -(float)extForces[iVer]);
			if (appliedForce.Length() > 1)
			{
				Vector2 scaled = appliedForce.Normalized() * (float)(appliedForce.Length() / maxForceMag * 50);
				Vector2 tip = newPos + scaled;
				DrawLine(newPos, tip, Colors.Green, 2f);
				ForceLabel(tip, appliedForce.Length(), Colors.Green);
			}

			// Draw reaction force (red) if restrained
			bool rx = restrainedIndex.Contains(iHor);
			bool ry = restrainedIndex.Contains(iVer);

			Vector2 reaction = new Vector2(rx ? (float)fi[iHor] : 0, ry ? -(float)fi[iVer] : 0);
			if (reaction.Length() > 1)
			{
				Vector2 scaled = reaction.Normalized() * (float)(reaction.Length() / maxForceMag * 50);
				Vector2 tip = newPos + scaled;
				DrawLine(newPos, tip, Colors.Red, 2f);
				ForceLabel(tip, reaction.Length(), Colors.Red);
			}

			// Draw restraint symbols
			if (rx)
				DrawLine(newPos + new Vector2(-25, 0), newPos + new Vector2(25, 0), Colors.Black, 3f);
			if (ry)
				DrawLine(newPos + new Vector2(0, -25), newPos + new Vector2(0, 25), Colors.Black, 3f);
		}
	}


	public override void _Ready()
	{
		Initialize();
	}


	public override void _Draw() {
		if (currentLoadStepProgress == 0.0f) {
			DrawInitial();
		}
		else {
			DrawFinal();
		}
	}


    private void OnLoadStepValueChanged(float value)
    {
        currentLoadStepProgress = value;
        QueueRedraw();
    }

	private void OnZoomValueChanged(float value)
    {
        ppmMultiplier = value;
        QueueRedraw();
    }

	private void OnSceneValueChanged(int index)
    {
        PathPrefix = dataLocations[index];
		Initialize();
        QueueRedraw();
    }

}
