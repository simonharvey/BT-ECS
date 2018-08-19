﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Sharvey.ECS.BehaviourTree
{
	public class NodeBuilder
	{
		public readonly Node Node;
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

		public NodeBuilder(Node node, NodeBuilder parent = null)
		{
			Node = node;
			Parent = parent;
		}

		public NodeBuilder CreateChild<T>(T child) where T : Node
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

	public class BehaviourTreeBuilder
	{
		public static BehaviourTree Compile(NodeBuilder root, int memSize = 0xFFFFFF)
		{
			var layers = new List<int>();

			var open = new List<NodeBuilder>();
			var nodes = new List<GCHandle>();
			var dataOff = new List<int>();
			var structure = new NativeArray<BehaviourTree.FlatNode>(root.TreeCount, Allocator.Persistent);

			open.Add(root);
			var off = UnsafeUtility.SizeOf<NodeState>() * root.TreeCount;
			int nodeIdx = 0;

			while (open.Count > 0)
			{
				layers.Add(open.Count);

				var layerNodes = open.ToArray();
				open.Clear();

				var childPos = nodeIdx + layerNodes.Length;

				foreach (var n in layerNodes)
				{
					dataOff.Add(off);
					off += n.Node.DataSize;
					nodes.Add(GCHandle.Alloc(n.Node));
					structure[nodeIdx] = new BehaviourTree.FlatNode(
						n.ChildCount,
						n.ChildCount > 0 ? childPos : -1
					);
					++nodeIdx;

					if (n.ChildCount > 0)
					{
						childPos += n.ChildCount;
						open.AddRange(n.Children);
					}
				}
			}

			var bt = new BehaviourTree
			{
				Nodes = new NativeArray<GCHandle>(nodes.ToArray(), Allocator.Persistent),
				Layers = new NativeArray<int>(layers.ToArray(), Allocator.Persistent),
				NodeDataOffset = new NativeArray<int>(dataOff.ToArray(), Allocator.Persistent),
				Structure = structure,
				Allocator = GCHandle.Alloc(new BehaviourTreeAllocator(off)),
				RuntimeDataSize = off,
				//Memory = new NativeArray<byte>(memSize, Allocator.Persistent),
			};

			Debug.Log($"Tree RuntimeSize: {bt.RuntimeDataSize}");

			return bt;
		}
	}
}
