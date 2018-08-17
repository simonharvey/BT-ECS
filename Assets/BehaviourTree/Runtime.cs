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
	}

	public struct EntityRuntime : IComponentData
	{
		public IntPtr Data;
	}

	public class BehaviourTreeSystem : JobComponentSystem
	{
		private struct UpdateLayerJob : IJobParallelFor
		{
			[ReadOnly] public BehaviourTree Tree;
			[ReadOnly] public int Layer;

			public unsafe void Execute()
			{
				Debug.Log($"wtf {Tree.Data[0]}");
			}

			public void Execute(int index)
			{
				Debug.Log($"wtf {Tree.Data[index]}");
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

			for (int i=1; i<trees.Count; ++i)
			{
				_group.SetFilter(trees[i]);
				Debug.Log($"Length :{_group.CalculateLength()}");
				
				inputDeps = new UpdateLayerJob
				{
					Tree = trees[i],
					Layer = 0,
				}.Schedule(trees[i].Data.Length, 0xFF, inputDeps);
			}

			return base.OnUpdate(inputDeps);
		}
	}
}
