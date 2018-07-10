using System;
using AElf.Network.V2.DHT.Node;

namespace AElf.Network.V2.DHT.Helpers
{
    public class Logger
    {
        public static void Log(string method, string message)
        {
            Console.WriteLine("[{0}] {1} {2}",
                DateTime.UtcNow.ToString(),
                method,
                message);
        }
        
        public static void Log(NodeData nodeData, string method, string message)
        {
            Console.WriteLine("[{0}] [{1}] [{2}:{3}] {4}", 
                DateTime.UtcNow.ToString(),
                nodeData.NodeId, 
                nodeData.IpAddress, 
                nodeData.Port, 
                message);
        }
    }
}