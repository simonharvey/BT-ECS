using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Sharvey.ECS.BehaviourTree
{
	public enum NodeState : byte
	{
		Inactive,
		Activating,
		Active,
		Complete,
		Failed
	}

	static class NodeStateExt
	{
		public static bool Running(this NodeState state)
		{
			return state == NodeState.Activating || state == NodeState.Active;
		}
	}

	public unsafe interface INode
	{
		Type DataType { get; }

		void Update(int nodeIndex, TreeDef treeDef, NativeSlice<NodeState> state, NativeSlice<byte> data);
	}

	public abstract class TNode<T> : INode
		where T : struct
	{
		public Type DataType => typeof(T);

		public unsafe void Update(int nodeIndex, TreeDef treeDef, NativeSlice<NodeState> state, NativeSlice<byte> data)
		{
			var arr = data.SliceConvert<T>();
			for (int i = 0; i < arr.Length; ++i)
			{
				var h = new NodeStateHandle(nodeIndex, state.Slice(i * treeDef.Nodes.Length, treeDef.Nodes.Length), treeDef);
				var v = arr[i];
				
				if (h.State == NodeState.Activating)
				{
					h.State = Activate(h, ref v);
				}

				if (h.State == NodeState.Active)
				{
					h.State = Update(h, ref v);
					arr[i] = v;
				}
			}
		}

		public virtual NodeState Activate(NodeStateHandle state, ref T value)
		{
			//Debug.Log($"Activate {this}");
			return NodeState.Active;
		}

		public abstract NodeState Update(NodeStateHandle state, ref T value);
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
