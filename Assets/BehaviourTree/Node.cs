using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

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

		void Update(NativeSlice<NodeState> state, NativeSlice<byte> data);
	}

	public abstract class TNode<T> : INode
		where T : struct
	{
		public Type DataType => typeof(T);

		public unsafe void Update(NativeSlice<NodeState> state, NativeSlice<byte> data)
		{
			var arr = data.SliceConvert<T>();
			for (int i = 0; i < arr.Length; ++i)
			{
				var v = arr[i];
				Update(new NodeStateHandle(), ref v);
				arr[i] = v;
			}
		}

		public abstract void Update(NodeStateHandle state, ref T value);
	}

	public struct NodeStateHandle
	{
		[ReadOnly] public readonly NativeSlice<NodeState> State;
		[ReadOnly] public readonly TreeDef Def;

		public int ChildCount => 0;

		public NodeState ChildState(int i)
		{
			return NodeState.Inactive;
		}

		public void ActivateChild(int i)
		{

		}
	}
}
