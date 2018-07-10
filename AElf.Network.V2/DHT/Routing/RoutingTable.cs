using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Network.V2.DHT.Helpers;
using AElf.Network.V2.DHT.Node;

namespace AElf.Network.V2.DHT.Routing
{
    public class RoutingTable : IRoutingTable
    {
        private readonly IHasher _hasher;

        public RoutingTable(IHasher hasher) : this(hasher, new List<NodeData>())
        {
        }

        public RoutingTable(IHasher hasher, IList<NodeData> nodes)
        {
            if (hasher == null || nodes == null)
                throw new ArgumentNullException();

            _hasher = hasher;
            this.Nodes = nodes;
        }

        private IList<NodeData> SortedNodes
        {
            get
            {
                return Nodes.OrderBy(n => n.NodeId).ToList(); 
            }
        }

        public IList<NodeData> Nodes { get; set; }

        public NodeData FindNode(string key)
        {
            uint partitionKey = _hasher.Hash(key);

            // find the last node which has an id smaller than the partition key
            NodeData partitionNode = SortedNodes.LastOrDefault(n => n.NodeId <= partitionKey);

            // if no node was found, we'll give the load to the last node
            if (partitionNode == null)
            {
                partitionNode = SortedNodes.Last();
            }

            return partitionNode;
        }
    }
}