using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace Sharvey.ECS.BehaviourTree
{
	public enum NodeState
	{
		Inactive,
		Activating,
		Active,
		Complete,
		Failed
	}

	public struct BehaviourTree : ISharedComponentData, IDisposable
	{
		//public NativeArraySharedValues<int> What;
		[ReadOnly] public int NodeDataSize;
		//[ReadOnly] public NativeArray<int> Data;
		[ReadOnly] public NativeArray<int> Layers;
		[ReadOnly] public NativeArray<GCHandle> Nodes;

		public void Dispose()
		{
			/*if (Data.IsCreated)
			Data.Dispose();*/
		}

		public Node GetNode(int layer, int index)
		{
			return null;
		}
	}
}
