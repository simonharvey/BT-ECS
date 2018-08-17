using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharvey.ECS.BehaviourTree
{
	public class NodeBuilder
	{
		public readonly Node Node;
		public readonly NodeBuilder Parent;
		public List<NodeBuilder> Children;

		public NodeBuilder(Node node = null, NodeBuilder parent = null)
		{
			Node = node;
			Parent = parent;
		}

		public NodeBuilder CreateChild<T>(T child) where T : Node
		{
			if (Children == null)
			{
				Children = new List<NodeBuilder>();
			}

			var c = new NodeBuilder(child, this);
			Children.Add(c);

			return c;
		}

		public NodeBuilder End()
		{
			return Parent;
		}
	}

	public class BehaviourTreeBuilder
	{
		public BehaviourTree Compile()
		{
			return default(BehaviourTree);
		}
	}
}
