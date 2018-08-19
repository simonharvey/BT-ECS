using Sharvey.ECS.BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

class Foo : IComponentData
{

}

public class TestBT : MonoBehaviour
{
	BehaviourTree _tree;

	private void TestMulti()
	{

	}

	private void Start()
	{
		var man = World.Active.GetOrCreateManager<EntityManager>();
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

		for (int i = 0; i < 1; ++i)
		{
			var e = man.CreateEntity();
			var rt = _tree.Register(man, e);
			_tree.GetHandle(rt, 0).SetState(NodeState.Activating);
		}
	}

	private void OnDestroy()
	{
		_tree.Dispose();
	}
}
