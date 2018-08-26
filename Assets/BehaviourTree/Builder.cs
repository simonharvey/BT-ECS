using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Sharvey.ECS.BehaviourTree
{
	public class NodeBuilder
	{
		public readonly INode Node;
		public readonly NodeBuilder Parent;
		public List<NodeBuilder> Children;
		public int ChildCount => Children?.Count ?? 0;
		public int TreeCount
		{
			get
			{
				if (ChildCount == 0)
					return 1;

				int count = 1;
				foreach (var c in Children)
				{
					count += c.TreeCount;
				}

				return count;
			}
		}

		public NodeBuilder(INode node, NodeBuilder parent = null)
		{
			Node = node;
			Parent = parent;
		}

		public NodeBuilder CreateChild<T>(T child) where T : INode
		{
			if (Children == null)
			{
				Children = new List<NodeBuilder>();
			}

			var c = new NodeBuilder(child, this);
			Children.Add(c);

			return c;
		}

		public NodeBuilder End()
		{
			return Parent ?? this;
		}
	}

	public class Builder
	{
		public static TreeDef Compile(NodeBuilder root)
		{
			var layers = new List<int>();

			var open = new List<NodeBuilder>();
			var nodes = new INode[root.TreeCount];
			var children = new TreeDef.Span[root.TreeCount];
			
			open.Add(root);
			int nodeIdx = 0;

			while (open.Count > 0)
			{
				layers.Add(open.Count);

				var layerNodes = open.ToArray();
				open.Clear();

				var childPos = nodeIdx + layerNodes.Length;

				foreach (var n in layerNodes)
				{
					nodes[nodeIdx] = n.Node;
					children[nodeIdx] = new TreeDef.Span(n.ChildCount > 0 ? childPos : -1, n.ChildCount);

					++nodeIdx;

					if (n.ChildCount > 0)
					{
						childPos += n.ChildCount;
						open.AddRange(n.Children);
					}
				}
			}

			var bt = new TreeDef
			{
				Nodes = nodes,
				Children = children,
			};

			return bt;
		}
	}
}

