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
			public readonly int First, Count;
			public Span(int first, int count)
			{
				First = first;
				Count = count;
			}
		}

		public INode[] Nodes;
		public Span[] Children;
		public Span[] Layers;
	}
}
