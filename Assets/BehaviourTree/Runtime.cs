using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Sharvey.ECS.BehaviourTree
{
	public class Node
	{
		public virtual void Update()
		{
			Debug.Log(this.GetType());
		}
	}

	public class RepeatForever : Node
	{

	}

	public class PrintNode : Node
	{
		
	}

	/*public class Sequence : Node
	{

	}

	public class LambdaNode : Node
	{
		
	}*/

	public struct EntityRuntime : IComponentData
	{
		public IntPtr Data;
	}

	public class BehaviourTreeSystem : JobComponentSystem
	{
		private struct UpdateLayerJob : IJobParallelFor
		{
			[ReadOnly] public BehaviourTree Tree;
			[ReadOnly] public int StartNode, EndNode;
			public ComponentDataArray<EntityRuntime> Runtime;

			//public unsafe void Execute()
			//{
			//	Debug.Log($"wtf {Tree.Data[0]}");
			//}

			public void Execute(int index)
			{
				Debug.Log($"wtf {Tree.Layers[index]}");
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

			for (int treeIdx=1; treeIdx<trees.Count; ++treeIdx)
			{
				var tree = trees[treeIdx];
				_group.SetFilter(tree);

				inputDeps = new UpdateLayerJob
				{
					Tree = trees[treeIdx],
					StartNode = 0,
					Runtime = _group.GetComponentDataArray<EntityRuntime>(),
				}.Schedule(tree.Layers.Length, 0xFF, inputDeps);
			}

			return base.OnUpdate(inputDeps);
		}
	}
}
