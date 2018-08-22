using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Unity.Burst;
using System.Diagnostics;

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

	public abstract class Node
	{
		public virtual int DataSize => 0;
		public abstract void Update(NodeRuntimeHandle handle, float dt);
	}

	public abstract class NodeWithData<T> : Node
		where T : struct
	{
		public override int DataSize => UnsafeUtility.SizeOf<T>();
	}

	public class RepeatForever : Node
	{
		public override void Update(NodeRuntimeHandle handle, float dt)
		{
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

		public override void Update(NodeRuntimeHandle handle, float dt)
		{ 
			//UnityEngine.Debug.Log(Value);
			handle.State = NodeState.Complete;
		}
	}

	public unsafe class DelayNode : NodeWithData<float>
	{
		public readonly float Delay;

		public DelayNode(float delay)
		{
			Delay = delay;
		}

		public override void Update(NodeRuntimeHandle handle, float dt)
		{
			float* remaining = (float*)handle.GetDataPtr();
			if (handle.State == NodeState.Activating)
			{
				*remaining = Delay;
				handle.State = NodeState.Active;
			}
			else
			{
				*remaining -= Delay;
				if (*remaining <= 0)
				{
					handle.State = NodeState.Complete;
				}
			}
		}
	}

	public unsafe class Sequence : NodeWithData<int>
	{
		public override void Update(NodeRuntimeHandle handle, float dt)
		{
			var activeIdx = (int*)handle.Data;

			if (handle.State == NodeState.Activating)
			{
				*activeIdx = 0;
				handle.State = NodeState.Active;
				handle.ActivateChild(0);
			}
			else
			{
				var activeChildState = handle.ChildState(*activeIdx);
				if (activeChildState == NodeState.Complete)
				{
					++(*activeIdx);
					if (*activeIdx >= handle.ChildCount)
					{
						handle.State = NodeState.Complete;
					}
					else
					{
						handle.ActivateChild(*activeIdx);
					}
				}
				else
				{
					handle.State = activeChildState;
				}
			}

			/*int* dataPtr = (int*)handle.GetDataPtr();
			if (handle.State == NodeState.Activating)
			{
				*dataPtr = 0;
				handle.State = NodeState.Active;
			}
			else
			{
				*dataPtr = *dataPtr + 1;
			}*/
		}
	}
	
	public struct BehaviourTreeRuntime : IComponentData
	{
		public IntPtr Data;
	}

	public unsafe struct NodeRuntimeHandle
	{
		public IntPtr TreeData;
		public BehaviourTree Tree;
		public int NodeIndex;

		public NodeState State
		{
			get => *(NodeState*)(TreeData + Tree.StateOffset(NodeIndex));
			set => *(NodeState*)(TreeData + Tree.StateOffset(NodeIndex)) = value;
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
			var statePtr = TreeData + Tree.StateOffset(ChildIndex(idx));
			return *(NodeState*)statePtr;
		}

		public void ActivateChild(int idx)
		{
			var nodeIdx = ChildIndex(idx);
			var ptr = TreeData + Tree.StateOffset(nodeIdx);
			*((NodeState*)ptr) = NodeState.Activating;
		}

		public IntPtr Data => TreeData + Tree.NodeDataOffset[NodeIndex];

		public IntPtr GetDataPtr()
		{
			return Data;
		}
	}

	//[BurstCompile]
	public class BehaviourTreeSystem : JobComponentSystem
	{
		//[BurstCompile]
		[DebuggerDisplay("Update Layer {StartNode}")]
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
				var handle = new NodeRuntimeHandle
				{
					NodeIndex = StartNode + index,
					TreeData = IntPtr.Zero,
					Tree = Tree,
				};
				for (int i = 0; i < Runtime.Length; ++i)
				{
					var r = Runtime[i];
					handle.TreeData = r.Data;

					// slowww
					var s = handle.State;
					if (s.Running())
					{
						node.Update(handle, Dt);
					}

					//var handle = Tree.GetHandle(r, StartNode + index);
					//var s = handle.State;
					//if (s.Running())
					//{
					//	node.Update(handle, Dt);
					//}
				}
			}
		}

		public struct NodesUpdateJob : IJobParallelFor
		{
			[ReadOnly] public BehaviourTree Tree;
			[ReadOnly] public float Dt;
			[ReadOnly] public int NodeIndex;
			[ReadOnly] public ComponentDataArray<BehaviourTreeRuntime> Runtimes;
			/*private Node _node;

			public NodesUpdateJob Init()
			{
				_node = (Node)Tree.Nodes[NodeIndex].Target;
				return this;
			}*/

			public unsafe void Execute(int index)
			{
				var handle = new NodeRuntimeHandle
				{
					NodeIndex = NodeIndex,
					TreeData = Runtimes[index].Data,
					Tree = Tree,
				};

				//var r = Runtimes[index];
				//_node.Update(handle, Dt);

				//var h = UnsafeUtility.ReadArrayElement<GCHandle>(Tree.Nodes.GetUnsafePtr(), NodeIndex);
				//((Node)h.Target).Update(handle, Dt);
				
				((Node)Tree.Nodes[NodeIndex].Target).Update(handle, Dt);
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
				var runtimes = _group.GetComponentDataArray<BehaviourTreeRuntime>();
				//Debug.Log(runtimes.Length);
				var end = tree.Nodes.Length;
				int iter = 0;

				var prevLayerDeps = inputDeps;
				
				for (int i = tree.Layers.Length - 1; i >= 0; --i)
				{
					if (iter++ > 10)
						break;

					var start = end - tree.Layers[i];
					var layerDeps = prevLayerDeps;
					for (int j=start; j < end; ++j)
					{
						var h = new NodesUpdateJob
						{
							Dt = dt,
							Runtimes = runtimes,
							Tree = tree,
							NodeIndex = j,
						}.Schedule(runtimes.Length, 64, prevLayerDeps);
						layerDeps = JobHandle.CombineDependencies(layerDeps, h);
					}
					end = start;
					prevLayerDeps = JobHandle.CombineDependencies(prevLayerDeps, layerDeps);
				}

				inputDeps = prevLayerDeps;

				/*for (int i=tree.Layers.Length-1; i>=0; --i)
				{
					if (iter++ > 10)
						break;

					var start = end - tree.Layers[i];

					inputDeps = new UpdateLayerJob
					{
						Tree = tree,
						StartNode = start,
						Dt = dt,
						Runtime = runtimes,
					}.Schedule(end - start, 1, inputDeps);

					end = start;
				}*/
			}

			return inputDeps;//base.OnUpdate(inputDeps);
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
