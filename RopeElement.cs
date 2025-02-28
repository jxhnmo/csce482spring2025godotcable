using Godot;
using System;
using System.Collections.Generic;

// Represents an element (segment) between two RopeNodes
public class RopeElement
{
	public RopeNode NodeA { get; private set; }
	public RopeNode NodeB { get; private set; }
	public float RestLength { get; private set; }
	public float Stiffness { get; set; } = .6f; // Example stiffness value
	public float Damping { get; set; } = 0.01f;  // Example damping value

	public RopeElement(RopeNode nodeA, RopeNode nodeB)
	{
		NodeA = nodeA;
		NodeB = nodeB;
		RestLength = nodeA.Position.DistanceTo(nodeB.Position);
	}

	public void ApplyForces()
	{
		Vector2 deltaP = NodeB.Position - NodeA.Position;
		float currentLength = deltaP.Length();
		float forceMag = (currentLength - RestLength) * Stiffness;
		Vector2 forceDir = deltaP.Normalized();
		Vector2 force = forceDir * forceMag;
		
		Vector2 relativeVelocity = NodeB.GetVelocity() - NodeA.GetVelocity();
		Vector2 dampingForce = -relativeVelocity * Damping;
		
		GD.Print("Damping Force: ", dampingForce);
		GD.Print("        Force: ", force);
		
		NodeA.ApplyForce(force - dampingForce);
		NodeB.ApplyForce(-force + dampingForce);
	}
}
