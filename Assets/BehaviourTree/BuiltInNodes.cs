using UnityEngine;

namespace Sharvey.ECS.BehaviourTree
{
	public class Sequence : TNode<int>
	{
		public override NodeState Activate(NodeStateHandle state, ref int value)
		{
			value = 0;
			state.ActivateChild(0);
			return NodeState.Active;
		}

		public override NodeState Update(NodeStateHandle state, ref int value)
		{
			var curChildState = state.ChildState(value);

			if (curChildState.Running())
			{
				return NodeState.Active;
			}
			++value;
			if (value < state.ChildCount)
			{
				state.ActivateChild(value);
				return NodeState.Active;
			}
			else
			{
				return NodeState.Complete;
			}
		}
	}

	public class Delay : TNode<float>
	{
		public readonly float Duration;

		public Delay(float duration)
		{
			Duration = duration;
		}

		public override NodeState Activate(NodeStateHandle state, ref float value)
		{
			value = Duration;
			return NodeState.Active;
		}

		public override NodeState Update(NodeStateHandle state, ref float value)
		{
			value -= 0.1f;
			if (value <= 0.0f)
				return NodeState.Complete;
			return NodeState.Active;
		}
	}

	public class PrintNode : Node
	{
		public readonly string Value;

		public PrintNode(string value)
		{
			Value = value;
		}

		public override NodeState Update(NodeStateHandle h)
		{
			Debug.Log(Value);
			return NodeState.Complete;
		}
	}
}
