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
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

		var man = World.Active.GetOrCreateManager<EntityManager>();
		_tree = BehaviourTreeBuilder.Compile(
			new NodeBuilder(new RepeatForever())
				.CreateChild(new Sequence())
					.CreateChild(new PrintNode("A")).End()
					.CreateChild(new PrintNode("B")).End()
					.CreateChild(new PrintNode("C")).End()
					.CreateChild(new PrintNode("D")).End()
					.CreateChild(new PrintNode("E")).End()
				.End()
			.End()
		);

		for (int i = 0; i < 10000; ++i)
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
