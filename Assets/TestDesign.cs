using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using System;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;
using System.Runtime.InteropServices;

namespace TestDesign
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

unsafe interface INode
{
	Type DataType { get; }

	void Update(NativeSlice<NodeState> state, NativeSlice<byte> data);
}

abstract class TNode<T> : INode
	where T : struct
{
	public Type DataType => typeof(T);

	public unsafe void Update(NativeSlice<NodeState> state, NativeSlice<byte> data)
	{
		var arr = data.SliceConvert<T>();
		for (int i=0; i<arr.Length; ++i)
		{
			var v = arr[i];
			Update(ref v);
			arr[i] = v;
		}
	}

	public abstract void Update(ref T value);
}

struct NodeExecutionHandle
{
	[ReadOnly] public readonly NativeSlice<NodeState> State;
	[ReadOnly] public readonly TreeDef Def;
}

class FooNode : TNode<Vector3Int>
{
	public override void Update(ref Vector3Int value)
	{
		++value.x;
	}
}

class BarNode : TNode<int>
{
	public override void Update(ref int value)
	{
		++value;
	}
}

//

struct TreeDef
{
	public INode[] Nodes;
}

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
		for (int i=0; i<tree.Nodes.Length; ++i)
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

unsafe class BTSystem : JobComponentSystem
{
	struct Job : IJob
	{
		public struct ExecutionParams
		{
			public NativeSlice<NodeState> State;
			public NativeSlice<byte> NodeData;
		}

		[ReadOnly] public readonly INode Node;
		[ReadOnly] public readonly ExecutionParams Params;

		public Job(INode node, ExecutionParams execParams)
		{
			Node = node;
			Params = execParams;
		}

		public void Execute()
		{
			//Debug.Log($"Update {Node.DataType} {UnsafeUtility.SizeOf(Node.DataType)} {Params.NodeData.Length / UnsafeUtility.SizeOf(Node.DataType)}");
			Node.Update(Params.State, Params.NodeData);
		}
	}

	private ComponentGroup _group;
	List<TreeRuntime> _trees = new List<TreeRuntime>();

	protected override void OnCreateManager(int capacity)
	{
		base.OnCreateManager(capacity);
		_group = GetComponentGroup(
			typeof(TreeRuntime)
		);
	}

	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		_trees.Clear();
		EntityManager.GetAllUniqueSharedComponentDatas<TreeRuntime>(_trees);
		//Debug.Log($"Num trees {_trees.Count}");

		for (int treeIdx = 1; treeIdx < _trees.Count; ++treeIdx)
		{
			var tree = _trees[treeIdx];
			_group.SetFilter(tree);

			for (int i=tree.Def.Nodes.Length-1; i>=0; --i)
			{
				inputDeps = new Job(tree.Def.Nodes[i], new Job.ExecutionParams
				{
					NodeData = tree.GetData(i),
				}).Schedule(inputDeps);
			}
		}

		return inputDeps;
	}
}

public class TestDesign : MonoBehaviour
{
	TreeRuntime _runtime;

	private void Start()
	{
		var N = 10000;

		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

		var def = new TreeDef
		{
			Nodes = new INode[] 
			{
				new FooNode(),
				new FooNode(),
				new FooNode(),
				new FooNode(),
				new FooNode(),
				new BarNode(),
			}
		};

		_runtime = TreeRuntime.Create(def, N);
		var man = World.Active.GetOrCreateManager<EntityManager>();
		for (int i=0; i<N; ++i)
		{
			var e = man.CreateEntity();
			_runtime.Register(man, e);
			//man.AddSharedComponentData(e, _runtime);
		}
	}

	private void OnDestroy()
	{
		_runtime.Dispose();
	}
}

}