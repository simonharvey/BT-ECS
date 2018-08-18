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

		/*var _tree = new BehaviourTree
		{
			Data = new NativeArray<int>(new[] { 666, 123 }, Allocator.Persistent),
		};*/

		var alloc = new BehaviourTreeAllocator();

		_tree = BehaviourTreeBuilder.Compile(
			new NodeBuilder(new RepeatForever())
				.CreateChild(new Sequence())
					.CreateChild(new PrintNode()).End()
					.CreateChild(new PrintNode()).End()
					.CreateChild(new PrintNode()).End()
					.CreateChild(new PrintNode()).End()
					.CreateChild(new PrintNode()).End()
				.End()
			.End()
		);

		for (int i=0; i<10000; ++i)
		{
			var e = man.CreateEntity();
			man.AddSharedComponentData(e, _tree);
			man.AddComponentData(e, new EntityRuntime
			{
				//Data = _tree.CreateRuntimeData(),
			});
		}

		//var bt = BehaviourTreeBuilder.Compile(
		//	new NodeBuilder(new RepeatForever())
		//	.CreateChild(new Sequence())
		//		.CreateChild(new PrintNode()).End()
		//		.CreateChild(new PrintNode()).End()
		//	.End()
		//);
	}

	private void OnDestroy()
	{
		_tree.Dispose();
	}
}
