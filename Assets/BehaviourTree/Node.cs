using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace Sharvey.ECS.BehaviourTree
{
	public enum NodeState : byte
	{
		Inactive,
		Activating,
		Running,
		Complete,
		Failed
	}

	static class NodeStateExt
	{
		public static bool Active(this NodeState state)
		{
			return state == NodeState.Activating || state == NodeState.Running;
		}
	}

	public unsafe interface INode
	{
		int DataSize { get; }

		void Update(float dt, int nodeIndex, TreeDef treeDef, NativeSlice<NodeState> state, NativeSlice<byte> data);
	}

	public abstract class Node : INode
	{
		public int DataSize => 0;

		public void Update(float dt, int nodeIndex, TreeDef treeDef, NativeSlice<NodeState> state, NativeSlice<byte> data)
		{
			int nStates = state.Length / treeDef.Nodes.Length;

			for (int i = 0; i < nStates; ++i)
			{
				var h = new NodeStateHandle(nodeIndex, state.Slice(i * treeDef.Nodes.Length, treeDef.Nodes.Length), treeDef);

				if (!h.State.Active())
					continue;

				if (h.State == NodeState.Activating)
				{
					//Debug.Log("AAAA");
					h.State = Activate(h);
				}

				if (h.State == NodeState.Running)
				{
					//Debug.Log("BBBB");
					h.State = Update(dt, h);
				}
			}
		}

		public abstract NodeState Activate(NodeStateHandle state);
		public abstract NodeState Update(float dt, NodeStateHandle h);
	}

	public abstract class TNode<T> : INode
		where T : struct
	{
		public int DataSize => UnsafeUtility.SizeOf<T>();

		public unsafe void Update(float dt, int nodeIndex, TreeDef treeDef, NativeSlice<NodeState> state, NativeSlice<byte> data)
		{
			Profiler.BeginSample("TNode::Update");
			var arr = data.SliceConvert<T>();
			for (int i = 0; i < arr.Length; ++i)
			{
				var h = new NodeStateHandle(nodeIndex, state.Slice(i * treeDef.Nodes.Length, treeDef.Nodes.Length), treeDef);
				var v = arr[i];
				
				if (h.State == NodeState.Activating)
				{
					h.State = Activate(h, ref v);
				}

				if (h.State == NodeState.Running)
				{
					h.State = Update(dt, h, ref v);
					arr[i] = v;
				}
			}
			Profiler.EndSample();
		}

		public abstract NodeState Activate(NodeStateHandle state, ref T value);
		public abstract NodeState Update(float dt, NodeStateHandle state, ref T value);

		/*public virtual NodeState Activate(NodeStateHandle state, ref T value)
		{
			Debug.Log($"Activate {this}");
			return NodeState.Active;
		}*/

	}

	public struct NodeStateHandle
	{
		public readonly int NodeIndex;
		public NativeSlice<NodeState> TreeState;
		[ReadOnly] public readonly TreeDef Def;

		public NodeStateHandle(int nodeIndex, NativeSlice<NodeState> treeState, TreeDef def)
		{
			NodeIndex = nodeIndex;
			TreeState = treeState;
			Def = def;
		}

		public int ChildCount => Def.Children[NodeIndex].Count;

		public NodeState State
		{
			get => TreeState[NodeIndex];
			set => TreeState[NodeIndex] = value;
		}

		public NodeState ChildState(int i)
		{
			// todo: assert bounds
			return TreeState[Def.Children[NodeIndex].First + i];
		}

		public void ActivateChild(int i)
		{
			// todo: assert bounds
			TreeState[Def.Children[NodeIndex].First + i] = NodeState.Activating;
		}
	}
}
