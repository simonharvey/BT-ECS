using UnityEngine;

namespace Sharvey.ECS.BehaviourTree
{
	public class LoopForever : Node
	{
		public override NodeState Update(float dt, NodeStateHandle h)
		{
			for (int i=0; i<h.ChildCount; ++i)
			{
				if (!h.ChildState(i).Active())
				{
					h.ActivateChild(i);
				}
			}

			return NodeState.Running;
		}
	}

	public class Sequence : TNode<int>
	{
		public override NodeState Activate(NodeStateHandle state, ref int value)
		{
			value = 0;
			state.ActivateChild(0);
			return NodeState.Running;
		}

		public override NodeState Update(float dt, NodeStateHandle state, ref int value)
		{
			var curChildState = state.ChildState(value);

			if (curChildState.Active())
			{
				return NodeState.Running;
			}
			++value;
			if (value < state.ChildCount)
			{
				state.ActivateChild(value);
				return NodeState.Running;
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
			return NodeState.Running;
		}

		public override NodeState Update(float dt, NodeStateHandle state, ref float value)
		{
			value -= dt;
			if (value <= 0.0f)
				return NodeState.Complete;
			return NodeState.Running;
		}
	}

	public class PrintNode : Node
	{
		public readonly string Value;

		public PrintNode(string value)
		{
			Value = value;
		}

		public override NodeState Update(float dt, NodeStateHandle h)
		{
			//Debug.Log(Value);
			return NodeState.Complete;
		}
	}
}
