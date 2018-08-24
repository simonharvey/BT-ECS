﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Sharvey.ECS.BehaviourTree
{
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
			Debug.Log($"Num trees {_trees.Count - 1}");
			
			for (int treeIdx = 1; treeIdx < _trees.Count; ++treeIdx)
			{
				var tree = _trees[treeIdx];
				_group.SetFilter(tree);

				for (int i = tree.Def.Nodes.Length - 1; i >= 0; --i)
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
}
