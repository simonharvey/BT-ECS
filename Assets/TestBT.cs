using Sharvey.ECS.BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class TestBT : MonoBehaviour
{
	BehaviourTree _tree;
	
	private void Start()
	{
		var man = World.Active.GetOrCreateManager<EntityManager>();

		var _tree = new BehaviourTree
		{
			Data = new NativeArray<int>(new[] { 666, 123 }, Allocator.Persistent),
		};
		//var tree2 = new BehaviourTree();

		for (int i=0; i<0xFF; ++i)
		{
			var e = man.CreateEntity();
			man.AddSharedComponentData(e, _tree);
			man.AddComponentData(e, new EntityRuntime
			{

			});
		}
	}

	private void OnDestroy()
	{
		_tree.Dispose();
	}
}
