using Sharvey.ECS.BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

class FooNode : TNode<Vector3Int>
{
	public override NodeState Activate(NodeStateHandle state, ref Vector3Int value)
	{
		return NodeState.Active;
	}

	public override NodeState Update(NodeStateHandle state, ref Vector3Int value)
	{
		Debug.Log("Update foo");
		++value.x;
		return NodeState.Complete;
	}
}

class BarNode : TNode<int>
{
	public override NodeState Activate(NodeStateHandle state, ref int value)
	{
		value = 0;
		return NodeState.Active;
	}

	public override NodeState Update(NodeStateHandle state, ref int value)
	{
		++value;
		return NodeState.Complete;
	}
}

public class TestBT : MonoBehaviour
{
	TreeRuntime _runtime;

	private void Start()
	{
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

		var N = 10;

		var def = Builder.Compile(new NodeBuilder(new Sequence())
			.CreateChild(new PrintNode("A")).End()
			.CreateChild(new PrintNode("B")).End()
			.CreateChild(new PrintNode("C")).End()
			//.CreateChild(new BarNode()).End()
			//.CreateChild(new BarNode()).End()
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
