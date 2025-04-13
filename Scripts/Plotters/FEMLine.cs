using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FEMLine : Node2D, CablePlotter
{
	private bool show;
	private Color lineColor;
	private Line2D catenaryLine;
	private Line2D deformedLine;

	// Catenary Variables (User Input/Adjustable)
	private int n = 12; // Number of segments for the plot

	// FEM Variables/Constants
	private double E = 7e10; // Young's Modulus (Pa)
	private double A = 0.000002; // Cross-sectional Area (m^2)
	private double gamma = 100; // Mass per unit length (kg/m)
	private bool swt = true; // Apply Self-Weight of Cable
	private int P = 0; // (N) Point load magnitude (and direction via sign)
	private char pointLoadAxis = 'y'; //The GLOBAL axis along which point loads are applied
	private int nForceIncrements = 1000; // 
	private double convThreshold = 10.0; // (N) Threshold on average percentage increase in incremental deflection
	

	private int[][] members;
	private int[] restrainedIndex;
	private int[] restrainedDoF;
	private int[] freeDoF;
	private int nDoF;
	private Vector2[] nodes;
	private double[] Areas;
	private double[] P0;

	private double [,] forceVector;
	private double[] lengths;


	// Define global variables
	public static double[,] UG_FINAL;  // Global displacements
	public static double[,] FI_FINAL;  // Internal forces
	public static double[,] EXTFORCES; // External axial forces
	public static double[,] MBRFORCES; // Axial forces per member

	// Global displacement vector (initially zero - undeformed state)
	public static double[,] UG;

	// Transformation matrices for all members based on undeformed position
	public static double[,,] TMs;

	// Internal force system based on pre-tension in members
	public static double[,] F_pre;

	// Containers for incremental displacements and internal forces per iteration
	public static double[,] UG_inc;
	public static double[,] F_inc;

	// Force increment for each convergence test
	public static double[] forceIncrement;

	// Define a vector to store the total external force applied
	public static double[] maxForce;

	public FEMLine(Color lineColor) {
		this.lineColor = lineColor;
	}

	public String GetPlotName() {
		return "FEM Line";
	}

	public float GetProgress() {
		return 1f;
	}
	
	public Color GetColor() {
		return lineColor;
	}

	// Function to divide a vector by a scalar (element-wise division)
	private static double[] DivideVector(double[] vector, int divisor)
	{
		double[] result = new double[vector.Length];
		for (int i = 0; i < vector.Length; i++)
		{
			result[i] = vector[i] / divisor;
		}
		return result;
	}

	public override void _Ready()
	{
		catenaryLine = new Line2D
		{
			Name = "Catenary",
			Width = 4,
			DefaultColor = lineColor,
			Antialiased = true,
			Visible = false // Never visible. Now handled by a separate CablePlotter.
		};
		AddChild(catenaryLine);

		deformedLine = new Line2D
		{
			Name = "CatenaryDeformed",
			Width = 4,
			DefaultColor = lineColor,
			Antialiased = true,
			Visible = show
		};
		AddChild(deformedLine);

		InputControlNode.Instance.AddDoubleField("Young's Modulus (Pa)", E, (double val) =>  E = val);
		// InputControlNode.Instance.AddDoubleField("Cross-sectional Area (m^2)", A, (double val) => A = val);
		InputControlNode.Instance.AddDoubleField("Convergence Threshold N", convThreshold, (double val) => convThreshold = val);
		
	}
	// Ready() ENDS HERE


	public void Generate(float nodeMass, Vector2[] meterPoints, float actualLength, List<(int nodeIndex, Vector2 force)> extraForces = null)
	{
		try
		{
			if (meterPoints == null || meterPoints.Length < 2)
				throw new ArgumentException("At least two meter points are required to define a cable.");

			if (actualLength <= 0)
				throw new ArgumentException("Cable length must be positive.");

			if (catenaryLine == null || deformedLine == null)
			{
				Ready += () => Generate(nodeMass, meterPoints, actualLength, extraForces);
				return;
			}

			gamma = nodeMass * meterPoints.Length / actualLength;
			n = meterPoints.Length - 1;
			nDoF = 2 * (n + 1);

			if (n < 1)
				throw new InvalidOperationException("Number of cable segments must be at least 1.");

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

			// Build member connectivity
			members = new int[n][];
			for (int i = 0; i < n; i++)
				members[i] = new int[] { i + 1, i + 2 };

			restrainedIndex = new int[] { 0, 1, 2 * n, 2 * n + 1 };
			restrainedDoF = new int[] { 1, 2, 2 * n + 1, 2 * n + 2 };
			freeDoF = Enumerable.Range(2, 2 * n - 2).ToArray();

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

			// Displacements
			UG = new double[nDoF, 1];
			TMs = CalculateTransMatrices(UG, nodes, members);
			F_pre = InitPretension(forceVector, members, TMs, P0);
			UG_inc = CloneColumn(UG);
			F_inc = CloneColumn(F_pre);

			// Convergence
			forceIncrement = new double[nDoF];
			maxForce = new double[nDoF];
			for (int i = 0; i < nDoF; i++)
			{
				maxForce[i] = forceVector[i, 0];
				forceIncrement[i] = forceVector[i, 0] / nForceIncrements;
				forceVector[i, 0] = forceIncrement[i];
			}

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

				if (!notConverged)
				{
					inc++;
					UG_FINAL = AppendColumn(UG_FINAL, UG_total);
					FI_FINAL = AppendColumn(FI_FINAL, Fi_total);
					EXTFORCES = AppendColumn(EXTFORCES, forceVector);
					MBRFORCES = AppendColumn(MBRFORCES, ToColumnMatrix(CalculateMemberForces(GetLastColumn(UG_FINAL), members, nodes, lengths, P0, E, Areas)));

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

				deformedLine.ClearPoints();
				foreach (var p in deformed)
					deformedLine.AddPoint(Coordinator.MetersToWorld(p));
			}

			// Statistics
			var statsDict = new Dictionary<string, string>
			{
				{ "Force Increments", nForceIncrements.ToString() },
				{ "Converged Increments", (UG_FINAL.GetLength(1) - 1).ToString() },
				{ "Max Displacement", UG_FINAL.Cast<double>().Select(Math.Abs).Max().ToString("F3") + " m" },
				{ "Total Internal Force", FI_FINAL.Cast<double>().Sum().ToString("F2") + " N" },
				{ "Convergence Threshold", convThreshold.ToString() + " N" }
			};

			InputControlNode.Instance.StatisticsCallback(this, statsDict);
		}
		catch (Exception ex)
		{
			InputControlNode.Instance.ShowAlert("Error", $"FEMLine generation failed: {ex.Message}");
			GD.PushError($"FEMLine generation failed: {ex.Message}");
		}
	}

	private static double[,] CloneColumn(double[,] src)
	{
		int rows = src.GetLength(0);
		var result = new double[rows, 1];
		for (int i = 0; i < rows; i++)
			result[i, 0] = src[i, 0];
		return result;
	}

	private Vector2[] FEM(Vector2[] points){
		Vector2[] nonething = points;

		return nonething;
	}

	private double[,] CalculateTransMatrix(Vector2 posI, Vector2 posJ) 
	{
		/*
		Takes in the position of node I and J and returns the transformation matrix for the member.
		This will need to be recalculated as the structure deflects with each iteration.
		*/

		double[,] T = new double[2, 4];

		double ix = posI.X; // x-coord for node i 
		double iy = posI.Y; // y-coord for node i
		double jx = posJ.X; // x-coord for node j
		double jy = posJ.Y; // y-coord for node j  

		double dx = jx - ix; // x-component of vector along member
		double dy = jy - iy; // y-component of vector along member
		double length = Math.Sqrt(dx * dx + dy * dy); // Magnitude of vector (length of member)

		double lp = dx / length;
		double mp = dy / length;
		double lq = -mp;
		double mq = lp;

		T[0, 0] = -lp; T[0, 1] = -mp; T[0, 2] = lp;  T[0, 3] = mp;
		T[1, 0] = -lq; T[1, 1] = -mq; T[1, 2] = lq;  T[1, 3] = mq;

		return T;
	}

	private double[,,] CalculateTransMatrices(double[,] UG, Vector2[] nodes, int[][] members)
	{
		/*
		Calculate transformation matrices for each member based on the current deformed shape of the structure.
		The current deformed shape is the initial position plus cumulative displacements passed in as UG.
		*/

		int numMembers = members.Length;
		double[,,] TransformationMatrices = new double[numMembers, 2, 4]; // 3D array to store matrices

		for (int n = 0; n < numMembers; n++)
		{
			int node_i = members[n][0]; // Node number for node i of this member
			int node_j = members[n][1]; // Node number for node j of this member

			// Index of DoF for this member
			int ia = 2 * node_i - 2; // Horizontal DoF at node i
			int ib = 2 * node_i - 1; // Vertical DoF at node i
			int ja = 2 * node_j - 2; // Horizontal DoF at node j
			int jb = 2 * node_j - 1; // Vertical DoF at node j

			// New positions = initial position + cumulative displacement
			double ix = nodes[node_i - 1].X + UG[ia, 0];
			double iy = nodes[node_i - 1].Y + UG[ib, 0];
			double jx = nodes[node_j - 1].X + UG[ja, 0];
			double jy = nodes[node_j - 1].Y + UG[jb, 0];

			// Recalculate transformation matrix using updated nodal positions
			double[,] TM = CalculateTransMatrix(new Vector2((float)ix, (float)iy), new Vector2((float)jx, (float)jy));

			// Store transformation matrix for the current member
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					TransformationMatrices[n, i, j] = TM[i, j];
				}
			}
		}

		return TransformationMatrices;
	}

	private double[,] InitPretension(double[,] forceVector, int[][] members, double[,,] TMs, double[] P0)
	{
		/*
		P = axial pre-tension specified for each bar
		Calculate the force vector [F_pre] for each bar: [F_pre] = [T'][AA'][P]
		Combine into an overall vector representing the internal force system and return.
		*/

		int forceVectorSize = forceVector.GetLength(0);
		double[,] F_pre = new double[forceVectorSize, 1]; // Initialize internal force vector

		for (int n = 0; n < members.Length; n++)
		{
			int node_i = members[n][0]; // Node number for node i of this member
			int node_j = members[n][1]; // Node number for node j of this member

			// Index of DoF for this member
			int ia = 2 * node_i - 2; // Horizontal DoF at node i of this member 
			int ib = 2 * node_i - 1; // Vertical DoF at node i of this member
			int ja = 2 * node_j - 2; // Horizontal DoF at node j of this member
			int jb = 2 * node_j - 1; // Vertical DoF at node j of this member 

			// Determine internal pre-tension in global coordinates
			double[,] TM = new double[2, 4];

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					TM[i, j] = TMs[n, i, j];
				}
			}

			double[,] AAp = { { 1 }, { 0 } }; // 2x1 Matrix
			// double[,] AAp = { { -1 }, { 0 } };
			double P = P0[n];

			// Compute F_pre_global = TM.T * AAp * P
			double[,] TM_T = TransposeMatrix(TM);
			double[,] F_pre_global = MultiplyMatrix(TM_T, AAp);

			for (int i = 0; i < F_pre_global.GetLength(0); i++)
			{
				F_pre_global[i, 0] *= P;
			}

			// Add member pre-tension to overall record COMMENTED OUT
			F_pre[ia, 0] += F_pre_global[0, 0];
			F_pre[ib, 0] += F_pre_global[1, 0];
			F_pre[ja, 0] += F_pre_global[2, 0];
			F_pre[jb, 0] += F_pre_global[3, 0];
		}

		return F_pre;
	}

	private (double[,], double[,], double[,], double[,]) BuildElementStiffnessMatrix(int n, double[,] UG)
	{
		/*
		Build element stiffness matrix based on current position and axial force.
		n = member index
		UG = vector of global cumulative displacements
		*/

		// Get node numbers for this member
		int node_i = members[n][0]; // Node number for node i
		int node_j = members[n][1]; // Node number for node j

		// Index of DoF for this member
		int ia = 2 * node_i - 2; // Horizontal DoF at node i
		int ib = 2 * node_i - 1; // Vertical DoF at node i
		int ja = 2 * node_j - 2; // Horizontal DoF at node j
		int jb = 2 * node_j - 1; // Vertical DoF at node j

		// Displacements
		double d_ix = UG[ia, 0];
		double d_iy = UG[ib, 0];
		double d_jx = UG[ja, 0];
		double d_jy = UG[jb, 0];

		// Extract current transformation matrix [T]
		double[,] TM = new double[2, 4];
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				TM[i, j] = TMs[n, i, j];
			}
		}

		// Calculate local displacements [u, v] using global cumulative displacements UG
		double[,] globalDisp = { { d_ix }, { d_iy }, { d_jx }, { d_jy } };
		double[,] localDisp = MultiplyMatrix(TM, globalDisp);
		
		double u = localDisp[0, 0];
		double v = localDisp[1, 0];

		// Calculate extension, e
		double Lo = lengths[n];
		double e = Math.Sqrt(Math.Pow(Lo + u, 2) + Math.Pow(v, 2)) - Lo;

		// Calculate matrix [AA]
		double a1 = (Lo + u) / (Lo + e);
		double a2 = v / (Lo + e);
		double[,] AA = { { a1, a2 } };

		// Calculate axial load, P        
		double P = P0[n] + ((E * Areas[n] / Lo) * e);

		// Calculate matrix [d]
		double d11 = P * Math.Pow(v, 2);
		double d12 = -P * v * (Lo + u);
		double d21 = -P * v * (Lo + u);
		double d22 = P * Math.Pow(Lo + u, 2);
		double denominator = Math.Pow(Lo + e, 3);

		double[,] d = {
			{ d11 / denominator, d12 / denominator },
			{ d21 / denominator, d22 / denominator }
		};

		// Calculate element stiffness matrix
		double[,] NL = AddMatrices(
			MultiplyMatrix(TransposeMatrix(AA), MultiplyScalar(AA, (E * Areas[n] / Lo))),
			d
		);

		double[,] k = MultiplyMatrix(TransposeMatrix(TM), MultiplyMatrix(NL, TM));

		// Return element stiffness matrix in quadrants
		double[,] K11 = SubMatrix(k, 0, 2, 0, 2);
		double[,] K12 = SubMatrix(k, 0, 2, 2, 4);
		double[,] K21 = SubMatrix(k, 2, 4, 0, 2);
		double[,] K22 = SubMatrix(k, 2, 4, 2, 4);

		return (K11, K12, K21, K22);
	}

	private double[,] BuildStructureStiffnessMatrix(double[,] UG)
	{
		/*
		Standard construction of Primary and Structure stiffness matrix
		Construction of non-linear element stiffness matrix handled in a child function
		*/

		double[,] Kp = new double[nDoF, nDoF]; // Initialize the primary stiffness matrix

		for (int z = 0; z < members.Length; z++)
		{
			int node_i = members[z][0]; // Node number for node i of this member
			int node_j = members[z][1]; // Node number for node j of this member

			// Construct (potentially) non-linear element stiffness matrix
			var (K11, K12, K21, K22) = BuildElementStiffnessMatrix(z, UG);

			// Primary stiffness matrix indices associated with each node
			int ia = 2 * node_i - 2; // index 0
			int ib = 2 * node_i - 1; // index 1
			int ja = 2 * node_j - 2; // index 2
			int jb = 2 * node_j - 1; // index 3

			// Update primary stiffness matrix Kp
			AddSubMatrix(Kp, K11, ia, ia);
			AddSubMatrix(Kp, K12, ia, ja);
			AddSubMatrix(Kp, K21, ja, ia);
			AddSubMatrix(Kp, K22, ja, ja);
		}

		// Reduce to structure stiffness matrix by deleting rows and columns for restrained DoF
		double[,] Ks = DeleteRowsAndColumns(Kp, restrainedIndex.ToList());

		return Ks;
	}

	private double[,] SolveDisplacements(double[,] Ks, double[,] F_inequilibrium)
	{
		/*
		Standard solving for structural displacements
		*/

		// Make a copy of F_inequilibrium to modify it without affecting the original
		// double[] forceVectorRed = (double[])F_inequilibrium.Clone();
		double[] forceVectorRed = new double[F_inequilibrium.GetLength(0)];
		for (int i = 0; i < F_inequilibrium.GetLength(0); i++)
		{
			forceVectorRed[i] = F_inequilibrium[i, 0];  // extract column 0
		}

		// Delete rows corresponding to restrained degrees of freedom
		forceVectorRed = RemoveRows(forceVectorRed, restrainedIndex.ToList());

		// Solve for displacements U using matrix inversion
		double[,] U = MultiplyMatrix(InvertMatrix(Ks), ConvertToColumnMatrix(forceVectorRed));

		// Build the global displacement vector including zeros at restrained degrees of freedom
		double[] UG = new double[nDoF]; // Initialize global displacement vector
		int c = 0; // Counter for tracking unrestrained degrees of freedom

		for (int i = 0; i < nDoF; i++)
		{
			if (restrainedIndex.Contains(i))
			{
				UG[i] = 0; // Imposed zero displacement
			}
			else
			{
				UG[i] = U[c, 0]; // Assign actual displacement
				c++;
			}
		}

		
		return ConvertToColumnMatrix(UG);
	}

	private double[,] UpdateInternalForceSystem(double[,] UG)
	{
		/*
		Calculate the vector of internal forces associated with the incremental displacements UG
		[Fn] = [T'][AA'][P]
		*/

		double[,] F_int = new double[nDoF, 1]; // Initialize internal force vector

		// Cycle through each member and calculate nodal forces in global coordinates
		for (int n = 0; n < members.Length; n++)
		{
			int node_i = members[n][0]; // Node number for node i of this member
			int node_j = members[n][1]; // Node number for node j of this member

			// Index of DoF for this member
			int ia = 2 * node_i - 2; // Horizontal DoF at node i
			int ib = 2 * node_i - 1; // Vertical DoF at node i
			int ja = 2 * node_j - 2; // Horizontal DoF at node j
			int jb = 2 * node_j - 1; // Vertical DoF at node j

			// Displacements
			double d_ix = UG[ia, 0];
			double d_iy = UG[ib, 0];
			double d_jx = UG[ja, 0];
			double d_jy = UG[jb, 0];

			// Extract transformation matrix [T]
			double[,] TM = ExtractMatrix(TMs, n);

			// Calculate local displacements [u, v] using global cumulative displacements UG
			double[,] localDisp = MultiplyMatrix(TM, new double[,] { { d_ix }, { d_iy }, { d_jx }, { d_jy } });
			double u = localDisp[0, 0];
			double v = localDisp[1, 0];

			// Calculate extension, e
			double Lo = lengths[n];
			double e = Math.Sqrt(Math.Pow(Lo + u, 2) + Math.Pow(v, 2)) - Lo;

			// Calculate matrix [AA]
			double a1 = (Lo + u) / (Lo + e);
			double a2 = v / (Lo + e);
			double[,] AA = new double[,] { { a1, a2 } };

			// Calculate axial load, P, due to incremental deflections
			double P = (E * Areas[n] / Lo) * e;

			// Determine axial load in global coordinates
			double[,] F_global = MultiplyMatrix(TransposeMatrix(TM), MultiplyMatrix(TransposeMatrix(AA), new double[,] { { P } }));

			// Add member pre-tension to overall record
			F_int[ia, 0] += F_global[0, 0];
			F_int[ib, 0] += F_global[1, 0];
			F_int[ja, 0] += F_global[2, 0];
			F_int[jb, 0] += F_global[3, 0];
		}

		return F_int;
	}

	private bool TestForConvergence(int iteration, double threshold, double[,] F_inequilibrium)
	{
		/*
		Test if the structure has converged by comparing the maximum force in the equilibrium 
		force vector against a threshold for the simulation.
		*/

		bool notConverged = true; // Initialize the convergence flag
		double maxIneq = 0.0;

		if (iteration > 0)
		{
			// Get the maximum absolute force among free degrees of freedom
			maxIneq = freeDoF.Select(index => Math.Abs(F_inequilibrium[index,0])).Max();
			// GD.Print(maxIneq);
			// Check convergence condition
			if (maxIneq < threshold)
			{
				notConverged = false;
			}
		}

		return notConverged;
	}

	public static double[] CalculateMemberForces(double[] UG,
	int[][] members,
	Vector2[] nodes,
	double[] lengths,
	double[] P0,
	double E,
	double[] Areas)
	{
		/*
		Calculates the member forces based on change in length of each member.
		Takes in the cumulative global displacement vector as UG.
		*/

		double[] mbrForces = new double[members.Length]; // Initialize container for axial forces

		for (int n = 0; n < members.Length; n++)
		{
			int node_i = members[n][0]; // Node number for node i of this member
			int node_j = members[n][1]; // Node number for node j of this member   

			// Index of DoF for this member
			int ia = 2 * node_i - 2; // Horizontal DoF at node i
			int ib = 2 * node_i - 1; // Vertical DoF at node i
			int ja = 2 * node_j - 2; // Horizontal DoF at node j
			int jb = 2 * node_j - 1; // Vertical DoF at node j

			// New positions = initial position + cumulative displacement
			double ix = nodes[node_i - 1][0] + UG[ia];
			double iy = nodes[node_i - 1][1] + UG[ib];
			double jx = nodes[node_j - 1][0] + UG[ja];
			double jy = nodes[node_j - 1][1] + UG[jb];

			// Compute displacement components and new member length
			double dx = jx - ix;
			double dy = jy - iy;
			double newLength = Math.Sqrt(dx * dx + dy * dy);

			// Change in length
			double deltaL = newLength - lengths[n];

			// Axial force calculation due to change in length and pre-tension
			double force = P0[n] + (deltaL * E * Areas[n] / lengths[n]);
			mbrForces[n] = force; // Store member force
		}

		return mbrForces;
	}

	// HELPER FUNCTIONS
	// Transpose a 2D matrix
	private double[,] TransposeMatrix(double[,] matrix)
	{
		int rows = matrix.GetLength(0);
		int cols = matrix.GetLength(1);
		double[,] transposed = new double[cols, rows];

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				transposed[j, i] = matrix[i, j];
			}
		}
		return transposed;
	}

	// Matrix multiplication
	private double[,] MultiplyMatrix(double[,] A, double[,] B)
	{
		int rowsA = A.GetLength(0);
		int colsA = A.GetLength(1);
		int rowsB = B.GetLength(0);
		int colsB = B.GetLength(1);

		if (colsA != rowsB)
		{
			throw new Exception("Matrix dimensions do not match for multiplication.");
		}

		double[,] result = new double[rowsA, colsB];

		for (int i = 0; i < rowsA; i++)
		{
			for (int j = 0; j < colsB; j++)
			{
				for (int k = 0; k < colsA; k++)
				{
					result[i, j] += A[i, k] * B[k, j];
				}
			}
		}
		return result;
	}
	// Multiply matrix by a scalar
	private double[,] MultiplyScalar(double[,] matrix, double scalar)
	{
		int rows = matrix.GetLength(0);
		int cols = matrix.GetLength(1);
		double[,] result = new double[rows, cols];

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				result[i, j] = matrix[i, j] * scalar;
			}
		}
		return result;
	}

	// Add two matrices
	private double[,] AddMatrices(double[,] A, double[,] B)
	{
		int rows = A.GetLength(0);
		int cols = A.GetLength(1);
		double[,] result = new double[rows, cols];

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				result[i, j] = A[i, j] + B[i, j];
			}
		}
		return result;
	}

	// Extract a submatrix
	private double[,] SubMatrix(double[,] matrix, int rowStart, int rowEnd, int colStart, int colEnd)
	{
		int numRows = rowEnd - rowStart;
		int numCols = colEnd - colStart;
		double[,] subMatrix = new double[numRows, numCols];

		for (int i = 0; i < numRows; i++)
		{
			for (int j = 0; j < numCols; j++)
			{
				subMatrix[i, j] = matrix[rowStart + i, colStart + j];
			}
		}
		return subMatrix;
	}

	// Adds a submatrix to the main matrix at a specified location
	private void AddSubMatrix(double[,] mainMatrix, double[,] subMatrix, int rowStart, int colStart)
	{
		int rows = subMatrix.GetLength(0);
		int cols = subMatrix.GetLength(1);

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				mainMatrix[rowStart + i, colStart + j] += subMatrix[i, j];
			}
		}
	}

	// Deletes specific rows and columns from a matrix
	private double[,] DeleteRowsAndColumns(double[,] matrix, List<int> indicesToRemove)
	{
		int newSize = matrix.GetLength(0) - indicesToRemove.Count;
		double[,] reducedMatrix = new double[newSize, newSize];

		int newRow = 0;
		for (int i = 0; i < matrix.GetLength(0); i++)
		{
			if (indicesToRemove.Contains(i)) continue;

			int newCol = 0;
			for (int j = 0; j < matrix.GetLength(1); j++)
			{
				if (indicesToRemove.Contains(j)) continue;

				reducedMatrix[newRow, newCol] = matrix[i, j];
				newCol++;
			}
			newRow++;
		}

		return reducedMatrix;
	}

		// Removes specified rows from a vector
	private double[] RemoveRows(double[] vector, List<int> indicesToRemove)
	{
		return vector.Where((val, idx) => !indicesToRemove.Contains(idx)).ToArray();
	}

	// Converts a 1D array to a column matrix (2D array with 1 column)
	private double[,] ConvertToColumnMatrix(double[] vector)
	{
		double[,] matrix = new double[vector.Length, 1];
		for (int i = 0; i < vector.Length; i++)
		{
			matrix[i, 0] = vector[i];
		}
		return matrix;
	}

	// Inverts a square matrix
	private double[,] InvertMatrix(double[,] matrix)
	{
		int n = matrix.GetLength(0);
		var identity = CreateIdentityMatrix(n);
		var augmented = AugmentMatrix(matrix, identity);

		for (int i = 0; i < n; i++)
		{
			double diagElement = augmented[i, i];
			if (diagElement == 0) throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

			for (int j = 0; j < 2 * n; j++)
				augmented[i, j] /= diagElement;

			for (int k = 0; k < n; k++)
			{
				if (k == i) continue;
				double factor = augmented[k, i];
				for (int j = 0; j < 2 * n; j++)
					augmented[k, j] -= factor * augmented[i, j];
			}
		}

		return ExtractRightHalf(augmented);
	}

	// Helper to create an identity matrix
	private double[,] CreateIdentityMatrix(int size)
	{
		double[,] identity = new double[size, size];
		for (int i = 0; i < size; i++)
			identity[i, i] = 1;
		return identity;
	}

	// Helper to augment a matrix with another matrix
	private double[,] AugmentMatrix(double[,] left, double[,] right)
	{
		int n = left.GetLength(0);
		int m = left.GetLength(1);
		double[,] augmented = new double[n, m * 2];

		for (int i = 0; i < n; i++)
			for (int j = 0; j < m; j++)
			{
				augmented[i, j] = left[i, j];
				augmented[i, j + m] = right[i, j];
			}

		return augmented;
	}

	// Extracts the right half of an augmented matrix (after inversion)
	private double[,] ExtractRightHalf(double[,] matrix)
	{
		int n = matrix.GetLength(0);
		int m = matrix.GetLength(1) / 2;
		double[,] result = new double[n, m];

		for (int i = 0; i < n; i++)
			for (int j = 0; j < m; j++)
				result[i, j] = matrix[i, j + m];

		return result;
	}

	// Extracts a 2D slice from a 3D matrix (TMs[n,:,:] equivalent)
	private double[,] ExtractMatrix(double[,,] source, int index)
	{
		int rows = source.GetLength(1);
		int cols = source.GetLength(2);
		double[,] result = new double[rows, cols];

		for (int i = 0; i < rows; i++)
			for (int j = 0; j < cols; j++)
				result[i, j] = source[index, i, j];

		return result;
	}

	private static double[,] SumColumns(double[,] matrix)
	{
		int rows = matrix.GetLength(0);
		int cols = matrix.GetLength(1);
		double[,] result = new double[rows, 1];

		for (int i = 0; i < rows; i++)
		{
			double sum = 0;
			for (int j = 0; j < cols; j++)
			{
				sum += matrix[i, j];
			}
			result[i, 0] = sum;
		}

		return result;
	}

	private static double[,] SubtractMatrices(double[,] A, double[,] B)
	{
		int rows = A.GetLength(0);
		int cols = A.GetLength(1);
		double[,] result = new double[rows, cols];

		for (int i = 0; i < rows; i++)
			for (int j = 0; j < cols; j++)
				result[i, j] = A[i, j] - B[i, j];

		return result;
	}

	private static double[,] AppendColumn(double[,] matrix, double[,] newCol)
	{
		int rows = matrix.GetLength(0);
		int cols = matrix.GetLength(1);

		if (newCol.GetLength(0) != rows || newCol.GetLength(1) != 1)
			throw new ArgumentException("Column dimensions do not match");

		double[,] result = new double[rows, cols + 1];

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
				result[i, j] = matrix[i, j];

			result[i, cols] = newCol[i, 0];
		}

		return result;
	}

	private static double[] GetLastColumn(double[,] matrix)
	{
		int rows = matrix.GetLength(0);
		int lastColIndex = matrix.GetLength(1) - 1;
		double[] result = new double[rows];

		for (int i = 0; i < rows; i++)
			result[i] = matrix[i, lastColIndex];

		return result;
	}

	private static double[,] ToColumnMatrix(double[] vector)
	{
		int length = vector.Length;
		double[,] result = new double[length, 1];
		for (int i = 0; i < length; i++)
			result[i, 0] = vector[i];
		return result;
	}


	public void HidePlot() {
		show = false;
		// if (catenaryLine != null && catenaryLine.IsInsideTree()) {
		// 	catenaryLine.Hide();
		// }
		if (deformedLine != null && deformedLine.IsInsideTree()) {
			deformedLine.Hide();
		}
	}

	public void ShowPlot() {
		show = true;
		// if (catenaryLine != null && catenaryLine.IsInsideTree()) {
		// 	catenaryLine.Show();
		// }
		if (deformedLine != null && deformedLine.IsInsideTree()) {
			deformedLine.Show();
		}
	}
}
