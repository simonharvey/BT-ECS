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
	public enum NodeState
	{
		Inactive,
		Activating,
		Active,
		Complete,
		Failed
	}

	public class BehaviourTreeAllocator : IDisposable
	{
		private NativeArray<byte> _buffer;
		private int _dataSize;

		public BehaviourTreeAllocator(int dataSize, int capacity=0xFFFF)
		{
			_dataSize = dataSize;
			_buffer = new NativeArray<byte>(capacity, Allocator.Persistent);
		}

		public NativeArray<byte> Create()
		{
			return default(NativeArray<byte>);
		}

		public void Dispose()
		{
			_buffer.Dispose();
		}
	}

	public struct TypedPtr<T>
	{

	}

	public struct TreeData
	{
		[ReadOnly] public int RuntimeDataSize;
		[ReadOnly] public NativeArray<int> Layers;
		[ReadOnly] public NativeArray<GCHandle> Nodes;
		[ReadOnly] public NativeArray<int> NodeDataOffset;
	}

	public struct BehaviourTree : ISharedComponentData, IDisposable
	{
		[ReadOnly] public int RuntimeDataSize;
		[ReadOnly] public NativeArray<int> Layers;
		[ReadOnly] public NativeArray<GCHandle> Nodes;
		[ReadOnly] public NativeArray<int> NodeDataOffset;

		[ReadOnly] public GCHandle Allocator;

		//public NativeArray<byte> Memory;
		//private int _memOffset; // replace with smarter heap allocator. this changes equality...

		public void Dispose()
		{
			/*if (Data.IsCreated)
			Data.Dispose();*/
			//Memory.Dispose();
		}

		public Node GetNode(int layer, int index)
		{
			return null;
		}

		public unsafe NativeArray<byte> CreateRuntimeData()
		{
			return ((BehaviourTreeAllocator)Allocator.Target).Create();
			//var ptr = new IntPtr(Memory.GetUnsafePtr()) + _memOffset;
			////_memOffset += RuntimeDataSize;
			//return ptr;
		}

		/*public unsafe NativeArray<byte> CreateEntityData()
		{
			var data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
				(new IntPtr(Memory.GetUnsafePtr()) + _memOffset).ToPointer(), 
				RuntimeDataSize, 
				Allocator.None);
			//_memOffset += RuntimeDataSize;
			return data;
		}*/
	}
}
