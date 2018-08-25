using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharvey.ECS.BehaviourTree
{
	public class TreeDef
	{
		public class Span
		{
			public int First, Count;
		}

		public INode[] Nodes;
		public Span[] Children;
		public Span[] Layers;
	}
}
