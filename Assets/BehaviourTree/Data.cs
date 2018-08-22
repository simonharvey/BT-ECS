﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

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

	public unsafe class BehaviourTreeAllocator : IDisposable
	{
		private NativeArray<byte> _buffer;
		private int _dataSize;
		private int _off = 0;

		public BehaviourTreeAllocator(int dataSize, int capacity=0xFFFFFF)
		{
			//Debug.Log($"BehaviourTreeAllocator {dataSize}");
			_dataSize = dataSize;
			_buffer = new NativeArray<byte>(capacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		}

		public IntPtr Create()
		{
			Assert.IsTrue(_buffer.Length >= _off);
			var ptr = new IntPtr(_buffer.GetUnsafePtr()) + _off;
			_off += _dataSize;
			return ptr;
		}

		public void Dispose()
		{
			_buffer.Dispose();
		}
	}

	public unsafe struct BehaviourTree : ISharedComponentData, IDisposable
	{
		[DebuggerDisplay("Node [{FirstChildIndex}, {ChildCount}]")]
		public struct FlatNode
		{
			public readonly int ChildCount;
			public readonly int FirstChildIndex;

			public FlatNode(int childCount, int firstChildIndex)
			{
				ChildCount = childCount;
				FirstChildIndex = firstChildIndex;
			}
		}

		[ReadOnly] public int RuntimeDataSize;
		[ReadOnly] public NativeArray<int> Layers;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public NativeArray<GCHandle> Nodes;
		[ReadOnly] public NativeArray<int> NodeDataOffset;
		[ReadOnly] public NativeArray<FlatNode> Structure;

		[ReadOnly] public GCHandle Allocator;

		public void Dispose()
		{
			var alloc = (BehaviourTreeAllocator)Allocator.Target;
			alloc.Dispose();
			Allocator.Free();

			Layers.Dispose();

			foreach (var node in Nodes)
			{
				node.Free();
			}
			Nodes.Dispose();

			NodeDataOffset.Dispose();
			Structure.Dispose();
		}

		public int StateOffset(int nodeIdx)
		{
			return UnsafeUtility.SizeOf<NodeState>() * nodeIdx;
		}

		public NodeRuntimeHandle GetHandle(BehaviourTreeRuntime btr, int nodeIdx)
		{
			//return default(NodeRuntimeHandle);
			return new NodeRuntimeHandle
			{
				Tree = this,
				NodeIndex = nodeIdx,
				TreeData = btr.Data,
			};
		}

		public BehaviourTreeRuntime Register(EntityManager manager, Entity e)
		{
			var alloc = (BehaviourTreeAllocator)Allocator.Target;
			Assert.IsNotNull(alloc);
			manager.AddSharedComponentData(e, this);
			var btr = new BehaviourTreeRuntime
			{
				Data = alloc.Create(),
			};
			manager.AddComponentData(e, btr);
			return btr;
		}
	}
}
