using System;
using System.Collections.Generic;
using System.Linq;
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
		public NativeArray<int> Data;

		public void Dispose()
		{
			if (Data.IsCreated)
			Data.Dispose();
		}
	}
}
