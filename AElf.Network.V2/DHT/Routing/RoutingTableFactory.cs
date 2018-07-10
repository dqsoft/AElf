using System;
using System.Collections.Generic;
using System.IO;
using AElf.Network.V2.DHT.Helpers;
using AElf.Network.V2.DHT.Node;

namespace AElf.Network.V2.DHT.Routing
{
    public class RoutingTableFactory
    {
        public static IRoutingTable FromFile(string filepath)
        {
            string[] lines = File.ReadAllLines(filepath);
            List<NodeData> nodes = new List<NodeData>();

            foreach (var line in lines)
            {
                var tokens = line.Split(' ');
                uint nodeId = UInt32.Parse(tokens[0]);
                string ipAddress = tokens[1];
                int port = int.Parse(tokens[2]);

                NodeData nodeData = new NodeData()
                {
                    NodeId = nodeId,
                    IpAddress = ipAddress,
                    Port = port
                };
                
                nodes.Add(nodeData);
            }
            
            return new RoutingTable(new Sha256Generator(), nodes);
        }
    }
}