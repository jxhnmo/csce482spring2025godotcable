using Godot;
using System;

// Factory class to instantiate ropes
public static class RopeFactory
{
	public static Rope CreateRope(float mass, float length, int segmentCount, Vector2 startPosition)
	{
		Rope rope = new Rope();
		float segmentLength = length / segmentCount;
		int nodeCount = segmentCount + 1;

		for (int i = 0; i < nodeCount; i++)
		{
			RopeNode node = new RigidNode { Position = startPosition + new Vector2(i * segmentLength, 0) };
			if (i == 0) {
				node.SetFixed(true);
			}
			rope.AppendNode(node);
		}
		return rope;
	}
}
