using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Sharvey.ECS.BehaviourTree
{
	struct TreeRuntimeComponentData : IComponentData
	{
		public int Index;
	}

	struct TreeRuntime : ISharedComponentData, IDisposable
	{
		private class AllocState
		{
			public int NextIndex;
		}

		[ReadOnly] public TreeDef Def;
		NativeArray<int> NodeDataOffset;
		// data and state should be in sync: index of an entity in the data has to be the same in the state array
		NativeArray<byte> Data;
		NativeArray<NodeState> State;
		private int Capacity;
		private GCHandle _allocHandle;

		public static TreeRuntime Create(TreeDef tree, int capacity = 10)
		{
			var rt = new TreeRuntime
			{
				Def = tree,
				Capacity = capacity,
			};
			rt.Data = new NativeArray<byte>(tree.Nodes.Sum(n => UnsafeUtility.SizeOf(n.DataType)) * capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			rt.State = new NativeArray<NodeState>(tree.Nodes.Length * capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			rt.NodeDataOffset = new NativeArray<int>(tree.Nodes.Length, Allocator.Persistent);
			rt._allocHandle = GCHandle.Alloc(new AllocState());
			int off = 0;
			for (int i = 0; i < tree.Nodes.Length; ++i)
			{
				rt.NodeDataOffset[i] = off;
				off += UnsafeUtility.SizeOf(tree.Nodes[i].DataType) * capacity;
			}

			return rt;
		}

		public NativeSlice<byte> GetData(int nodeIndex)
		{
			return Data.Slice(NodeDataOffset[nodeIndex], UnsafeUtility.SizeOf(Def.Nodes[nodeIndex].DataType) * Capacity);
		}

		public TreeRuntimeComponentData Register(EntityManager manager, Entity e)
		{
			var rt = new TreeRuntimeComponentData
			{
				Index = ((AllocState)_allocHandle.Target).NextIndex++,
			};
			manager.AddSharedComponentData(e, this);
			manager.AddComponentData(e, rt);
			Activate(rt);
			return rt;
		}

		private void Activate(TreeRuntimeComponentData e)
		{
			State[e.Index] = NodeState.Activating;
		}

		public void Dispose()
		{
			Data.Dispose();
			NodeDataOffset.Dispose();
			State.Dispose();
			_allocHandle.Free();
		}
	}
}
