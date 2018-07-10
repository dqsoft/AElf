using System.Collections;
using System.Collections.Generic;
using AElf.Network.V2.DHT.Node;

namespace AElf.Network.V2.DHT.Routing
{
    public interface IRoutingTable
    {
        IList<NodeData> Nodes { get; set; }
        NodeData FindNode(string key);
    }
}