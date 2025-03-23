using Godot;
using System;
using System.Collections.Generic;

// Manages an entire rope structure
public partial class Rope : Node2D 
{
	private readonly List<RopeNode> nodes = new List<RopeNode>();
	private readonly List<RopeElement> elements = new List<RopeElement>();

	public void AddNode(RopeNode node)
	{
		nodes.Add(node);
		AddChild(node);
	}

	public void ConnectNodes(RopeNode nodeA, RopeNode nodeB)
	{
		RopeElement element = new RopeElement(nodeA, nodeB);
		elements.Add(element);
	}
	
	public void AppendNode(RopeNode nodeB)
	{
		AddNode(nodeB);
		if (nodes.Count > 1) 
		{
			ConnectNodes(nodes[nodes.Count - 1], nodes[nodes.Count - 2]);
		}
	}

	public override void _Process(double delta)
	{
		foreach (var element in elements)
		{
			element.ApplyForces();
		}
		QueueRedraw();
	}
	
	public override void _Draw()
	{
		foreach (var element in elements)
		{
			DrawLine(element.NodeA.Position - Position, element.NodeB.Position - Position, Colors.White, 2.0f);
		}
	}
}
