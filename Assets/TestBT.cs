using Sharvey.ECS.BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

class FooNode : TNode<Vector3Int>
{
	public override void Update(NodeStateHandle state, ref Vector3Int value)
	{
		++value.x;
	}
}

class BarNode : TNode<int>
{
	public override void Update(NodeStateHandle state, ref int value)
	{
		++value;
	}
}

public class TestBT : MonoBehaviour
{
	TreeRuntime _runtime;

	private void Start()
	{
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

		var N = 10000;

		var def = Builder.Compile(new NodeBuilder(new FooNode())
			.CreateChild(new BarNode()).End()
			.CreateChild(new BarNode()).End()
		.End());
		_runtime = TreeRuntime.Create(def, N);

		var man = World.Active.GetOrCreateManager<EntityManager>();
		for (int i = 0; i < N; ++i)
		{
			var e = man.CreateEntity();
			_runtime.Register(man, e);
		}
	}

	private void OnDestroy()
	{
		_runtime.Dispose();
	}
}
