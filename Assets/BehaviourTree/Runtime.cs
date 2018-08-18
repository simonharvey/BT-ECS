using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Sharvey.ECS.BehaviourTree
{
	public struct NodeData
	{
		NodeState State;
	}

	public class Node
	{
		public virtual int DataSize => 0;
		//public virtual void Update()
	}

	public class NodeWithData<T> : Node
		where T : struct
	{
		public override int DataSize => UnsafeUtility.SizeOf<T>();
	}

	public class RepeatForever : Node
	{

	}

	public class PrintNode : Node
	{
		
	}

	public class Sequence : NodeWithData<int>
	{

	}
	
	public struct EntityRuntime : IComponentData
	{
		public /*NativeArray<byte>*/ IntPtr Data;
	}

	public class BehaviourTreeSystem : JobComponentSystem
	{
		private struct UpdateLayerJob : IJobParallelFor
		{
			[ReadOnly] public BehaviourTree Tree;
			[ReadOnly] public int StartNode, EndNode;
			[ReadOnly] public ComponentDataArray<EntityRuntime> Runtime;

			public void Execute(int index)
			{
				var node = (Node)Tree.Nodes[index].Target;
				var dataOffset = Tree.NodeDataOffset[index];
				//Tree.Allocator
				//Debug.Log($"Execute {StartNode} {EndNode} {node.GetType()}");
				for (int i=0; i<Runtime.Length; ++i)
				{
					var r = Runtime[i];
					//node.Update();	
				}
				//Debug.Log($"wtf {Tree.Layers[index]}");
			}
		}

		private ComponentGroup _group;

		protected override void OnCreateManager(int capacity)
		{
			base.OnCreateManager(capacity);
			_group = GetComponentGroup(
				ComponentType.ReadOnly(typeof(BehaviourTree)),
				typeof(EntityRuntime)
			);
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			List<BehaviourTree> trees = new List<BehaviourTree>();
			EntityManager.GetAllUniqueSharedComponentDatas<BehaviourTree>(trees);
			var dt = Time.deltaTime;

			//Debug.Log(trees.Count);

			for (int treeIdx=1; treeIdx<trees.Count; ++treeIdx)
			{
				var tree = trees[treeIdx];
				_group.SetFilter(tree);

				var end = tree.Nodes.Length;
				int iter = 0;
				for (int i=tree.Layers.Length-1; i>=0; --i)
				{
					if (iter++ > 10)
						break;

					var start = end - tree.Layers[i];

					inputDeps = new UpdateLayerJob
					{
						Tree = trees[treeIdx],
						StartNode = start,
						EndNode = end,
						Runtime = _group.GetComponentDataArray<EntityRuntime>(),
					}.Schedule(tree.Layers.Length, 0xFF, inputDeps);

					end = start - 1;
				}
			}

			return base.OnUpdate(inputDeps);
		}
	}
}

public unsafe struct FixedMemoryAllocator
{
	private NativeArray<byte> _buffer;
	private int _bytesLeft;
	[NativeDisableUnsafePtrRestriction] // todo: naughty
	private IntPtr _ptr;

	public FixedMemoryAllocator(NativeArray<byte> buffer)
	{
		_buffer = buffer;
		_bytesLeft = buffer.Length;
		_ptr = new IntPtr(buffer.GetUnsafePtr());
	}

	public IntPtr Alloc(int size)
	{
		var p = _ptr;
		_ptr += size;
		return p;
	}

	public NativeArray<T> Map<T>(int capacity)
		where T : struct
	{
		var spaceRequired = UnsafeUtility.SizeOf<T>() * capacity;
		Assert.IsTrue(_bytesLeft >= spaceRequired, "Not enough space");
		var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(_ptr.ToPointer(), capacity, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, NativeArrayUnsafeUtility.GetAtomicSafetyHandle(_buffer));
#endif
		_bytesLeft -= spaceRequired;
		_ptr += spaceRequired;
		return result;
	}

	public NativeArray<T> MapRemaining<T>(float capacity = 0f)
		where T : struct
	{
		var bytes = _bytesLeft;
		if (capacity > 0.0f)
		{
			bytes = (int)(bytes * capacity);
		}

		var numElem = bytes / UnsafeUtility.SizeOf<T>();
		return Map<T>(numElem);
	}
}
