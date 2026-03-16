using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    public class BehaviorTreeRuntime : ScriptableObject
    {
        [SerializeReference]
        public List<BTRuntimeNode> nodes = new List<BTRuntimeNode>();

        public int rootNodeIndex = -1;

        public T GetNode<T>(int index) where T : BTRuntimeNode
        {
            if (index >= 0 && index < nodes.Count)
                return nodes[index] as T;
            return null;
        }

        public BTRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
                return nodes[index];
            return null;
        }
    }
}
