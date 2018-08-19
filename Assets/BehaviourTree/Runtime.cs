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

	/*public interface INode
	{
		int DataSize { get; }
	}

	public class BaseNode<T> where T : NodeData
	{

	}*/

	public class Node
	{
		public virtual int DataSize => 0;
		public virtual void Update(NodeRuntimeHandle handle, float dt)
		{
			//Debug.Log("update " + this);
		}
	}

	public class NodeWithData<T> : Node
		where T : struct
	{
		public override int DataSize => UnsafeUtility.SizeOf<T>();
	}

	public class RepeatForever : Node
	{
		public override void Update(NodeRuntimeHandle handle, float dt)
		{
			base.Update(handle, dt);
			for (int i=0; i<handle.ChildCount; ++i)
			{
				if (!handle.ChildState(i).Running())
				{
					handle.ActivateChild(i);
				}
			}
		}
	}

	public class PrintNode : Node
	{
		public readonly string Value;
		public PrintNode(string value)
		{
			this.Value = value;
		}
	}

	public class DelayNode : NodeWithData<float>
	{
		public readonly float Delay;

		public DelayNode(float delay)
		{
			Delay = delay;
		}

		public override void Update(NodeRuntimeHandle handle, float dt)
		{
			base.Update(handle, dt);
		}
	}

	public unsafe class Sequence : NodeWithData<int>
	{
		public override void Update(NodeRuntimeHandle handle, float dt)
		{
			base.Update(handle, dt);
			int* dataPtr = (int*)handle.GetDataPtr();
			if (handle.State == NodeState.Activating)
			{
				*dataPtr = 0;
				handle.State = NodeState.Active;
			}
			else
			{
				*dataPtr = *dataPtr + 1;
				//Debug.Log($"Sequence {*dataPtr}");
			}
		}
	}
	
	public struct BehaviourTreeRuntime : IComponentData
	{
		public IntPtr Data;
	}

	public unsafe struct NodeRuntimeHandle
	{
		public BehaviourTreeRuntime Runtime;
		public BehaviourTree Tree;
		public int NodeIndex;

		public NodeState State
		{
			get => *(NodeState*)(Runtime.Data + Tree.StateOffset(NodeIndex));
			set => *(NodeState*)(Runtime.Data + Tree.StateOffset(NodeIndex)) = value;
		}
		// this is just to bypass temporary struct errors when calling Tree.GetHandle(rt, i).State
		public void SetState(NodeState state)
		{
			this.State = state;
		}

		public int ChildCount
		{
			get => Tree.Structure[NodeIndex].ChildCount;
		}

		public int ChildIndex(int idx)
		{
			return Tree.Structure[NodeIndex].FirstChildIndex + idx;
		}

		public NodeState ChildState(int idx)
		{
			var statePtr = Runtime.Data + Tree.StateOffset(ChildIndex(idx));
			return *(NodeState*)statePtr;
		}

		public void ActivateChild(int idx)
		{
			var nodeIdx = ChildIndex(idx);
			var ptr = Runtime.Data + Tree.StateOffset(nodeIdx);
			*((NodeState*)ptr) = NodeState.Activating;
		}

		public IntPtr GetDataPtr()
		{
			return Runtime.Data + Tree.NodeDataOffset[NodeIndex];
		}
	}

	public class BehaviourTreeSystem : JobComponentSystem
	{
		private struct UpdateLayerJob : IJobParallelFor
		{
			[ReadOnly] public BehaviourTree Tree;
			[ReadOnly] public int StartNode;//, EndNode;
			[ReadOnly] public float Dt;
			[ReadOnly] public ComponentDataArray<BehaviourTreeRuntime> Runtime;

			public void Execute(int index)
			{
				//Debug.Log($"Execute: {StartNode} {index}");

				var node = (Node)Tree.Nodes[StartNode + index].Target;
				var dataOffset = Tree.NodeDataOffset[index];
				for (int i = 0; i < Runtime.Length; ++i)
				{
					var r = Runtime[i];
					var handle = Tree.GetHandle(r, StartNode + index);
					var s = handle.State;
					if (s.Running())
					{
						node.Update(handle, Dt);
					}
				}
			}
		}

		private ComponentGroup _group;
		List<BehaviourTree> _trees = new List<BehaviourTree>();

		protected override void OnCreateManager(int capacity)
		{
			base.OnCreateManager(capacity);
			_group = GetComponentGroup(
				ComponentType.ReadOnly(typeof(BehaviourTree)),
				typeof(BehaviourTreeRuntime)
			);
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			_trees.Clear();
			EntityManager.GetAllUniqueSharedComponentDatas<BehaviourTree>(_trees);
			var dt = Time.deltaTime;

			for (int treeIdx=1; treeIdx<_trees.Count; ++treeIdx)
			{
				var tree = _trees[treeIdx];
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
						Tree = _trees[treeIdx],
						StartNode = start,
						Dt = dt,
						Runtime = _group.GetComponentDataArray<BehaviourTreeRuntime>(),
					}.Schedule(end - start, 1, inputDeps);

					end = start;
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
