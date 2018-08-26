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
		NativeArray<byte> NodeData;
		public NativeArray<NodeState> StateData;
		private int Capacity;
		private GCHandle _allocHandle;

		public static TreeRuntime Create(TreeDef tree, int capacity = 10)
		{
			var rt = new TreeRuntime
			{
				Def = tree,
				Capacity = capacity,
			};
			rt.NodeData = new NativeArray<byte>(tree.Nodes.Sum(n => n.DataSize) * capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			rt.StateData = new NativeArray<NodeState>(tree.Nodes.Length * capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
			rt.NodeDataOffset = new NativeArray<int>(tree.Nodes.Length, Allocator.Persistent);
			rt._allocHandle = GCHandle.Alloc(new AllocState());
			int off = 0;
			for (int i = 0; i < tree.Nodes.Length; ++i)
			{
				rt.NodeDataOffset[i] = off;
				off += tree.Nodes[i].DataSize * capacity;
			}

			return rt;
		}

		public NativeSlice<byte> GetData(int nodeIndex)
		{
			return NodeData.Slice(NodeDataOffset[nodeIndex], Def.Nodes[nodeIndex].DataSize * Capacity);
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
			StateData[e.Index * Def.Nodes.Length] = NodeState.Activating;
		}

		public void Dispose()
		{
			NodeData.Dispose();
			NodeDataOffset.Dispose();
			StateData.Dispose();
			_allocHandle.Free();
		}
	}
}
