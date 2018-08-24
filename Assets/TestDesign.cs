using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using System;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;

/*interface ITreeNode
{
	Type DataType { get; }
}

class TreeNode<T>
	: ITreeNode
	where T : struct
{
	public Type DataType => typeof(T);
}*/

unsafe interface INode
{
	Type DataType { get; }

	void Update(NativeSlice<byte> data);
}

/*class Node : INode
{
	public Type DataType { get; private set; }
	public Node(Type dataType)
	{
		DataType = dataType;
	}
}*/

abstract class TNode<T> : INode
	where T : struct
{
	public Type DataType => typeof(T);

	public unsafe void Update(NativeSlice<byte> data)
	{
		//var sl = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(data.GetUnsafePtr(), UnsafeUtility.SizeOf<T>(), 1);
		//var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.GetUnsafePtr(), data.Length / UnsafeUtility.SizeOf<T>(), Allocator.Invalid);
		//NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle<byte>(data.));
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

class FooNode : TNode<Vector3Int>
{
	public override void Update(ref Vector3Int value)
	{
		++value.x;
		//Debug.Log($"FooNode::Update {value}");
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

}

struct TreeRuntime : ISharedComponentData, IDisposable
{
	[ReadOnly] public TreeDef Def;
	NativeArray<byte> Data;
	NativeArray<int> NodeDataOffset;
	int Capacity;
	//NativeArray<NativeArray<byte>> Data;

	public static TreeRuntime Create(TreeDef tree, int capacity = 10)
	{
		var rt = new TreeRuntime
		{
			Def = tree,
			Capacity = capacity,
		};
		rt.Data = new NativeArray<byte>(tree.Nodes.Sum(n => UnsafeUtility.SizeOf(n.DataType)) * capacity, Allocator.Persistent);
		rt.NodeDataOffset = new NativeArray<int>(tree.Nodes.Length, Allocator.Persistent);
		int off = 0;
		for (int i=0; i<tree.Nodes.Length; ++i)
		{
			rt.NodeDataOffset[i] = off;
			off += UnsafeUtility.SizeOf(tree.Nodes[i].DataType) * capacity;
		}

		/*rt.Data = new NativeArray<NativeArray<byte>>(tree.NodeTypes.Length, Allocator.Persistent);
		for (int i=0; i<tree.NodeTypes.Length; ++i)
		{
			rt.Data[i] = new NativeArray<byte>(
				UnsafeUtility.SizeOf(tree.NodeTypes[i]) * capacity, 
				Allocator.Persistent);
		}*/
		return rt;
	}

	public NativeSlice<byte> GetData(int nodeIndex)
	{
		return Data.Slice(NodeDataOffset[nodeIndex], UnsafeUtility.SizeOf(Def.Nodes[nodeIndex].DataType) * Capacity);
	}

	/*public NativeArray<T> GetNodeDataArray<T>(int i)
		where T : struct
	{
		return default(NativeArray<T>);
		//NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray()
	}*/

	public void Dispose()
	{
		Data.Dispose();
		NodeDataOffset.Dispose();
	}
}

unsafe class BTSystem : JobComponentSystem
{
	struct Job : IJob//, IJobParallelFor
	{
		public struct ExecutionParams
		{
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
			Debug.Log($"Update {Node.DataType} {UnsafeUtility.SizeOf(Node.DataType)} {Params.NodeData.Length / UnsafeUtility.SizeOf(Node.DataType)}");
			Node.Update(Params.NodeData);
		}

		/*public void Execute(int index)
		{
		
		}*/
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

		for (int treeIdx = 1; treeIdx < _trees.Count; ++treeIdx)
		{
			var tree = _trees[treeIdx];
			_group.SetFilter(tree);

			//Debug.Log($"Tree {tree.Def.Nodes.Length}");
			for (int i=tree.Def.Nodes.Length-1; i>=0; --i)
			{
				/*inputDeps = new Job
				{
					Node = tree.Def.Nodes[i],
				}.Schedule(inputDeps);*/
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
			Nodes = new INode[] {
				new FooNode(),
				new FooNode(),
				new FooNode(),
				new FooNode(),
				new FooNode(),
				new BarNode(),
				/*new Node(typeof(float)),
				new Node(typeof(Vector3Int)),*/
			}
		};

		_runtime = TreeRuntime.Create(def, N);
		var man = World.Active.GetOrCreateManager<EntityManager>();
		for (int i=0; i<N; ++i)
		{
			var e = man.CreateEntity();
			man.AddSharedComponentData(e, _runtime);
		}
	}

	private void OnDestroy()
	{
		_runtime.Dispose();
	}
}
