using Sharvey.ECS.BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public struct TestBlackboard : IComponentData
{
	public Entity CurrentTarget;
}

class FooNode : TNode<Vector3Int>
{
	public override NodeState Activate(NodeStateHandle state, ref Vector3Int value)
	{
		return NodeState.Running;
	}

	public override NodeState Update(float dt, NodeStateHandle state, ref Vector3Int value)
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
		return NodeState.Running;
	}

	public override NodeState Update(float dt, NodeStateHandle state, ref int value)
	{
		++value;
		return NodeState.Complete;
	}
}

public class TestBT : MonoBehaviour
{
	TreeRuntime _runtime;
	[SerializeField] private GameObject _enemyPrefab;

	private void Start()
	{
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

		var N = 1000;

		var def = Builder.Compile(new NodeBuilder(new LoopForever())
			.CreateChild(new Sequence())
				.CreateChild(new PrintNode("A")).End()
				.CreateChild(new PrintNode("B")).End()
				.CreateChild(new PrintNode("C")).End()
				.CreateChild(new Delay(2.0f, 5.0f)).End()
			.End()
		.End());
		_runtime = TreeRuntime.Create(def, N, new TestBlackboard());

		var man = World.Active.GetOrCreateManager<EntityManager>();
		for (int i = 0; i < N; ++i)
		{
			var e = man.Instantiate(_enemyPrefab);
			man.SetComponentData(e, new Position { Value = Random.insideUnitSphere * 50.0f });
			//man.AddComponentData(e, new Blackboard { Map = new Unity.Collections.NativeHashMap<uint, uint>() });
			//var e = man.CreateEntity();
			_runtime.Register(man, e);
		}
	}

	private void OnDestroy()
	{
		_runtime.Dispose();
	}
}
