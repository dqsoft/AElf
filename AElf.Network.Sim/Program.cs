using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Xsl;
using AElf.Common.ByteArrayHelpers;
using AElf.Network.V2.Connection;
using AElf.Network.V2.DHT.Distance;

namespace AElf.Network.Sim
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start comTester on a new thread
            NetworkWatcher comTester = new NetworkWatcher();
            
            Task.Run(() => comTester.Start(args[0])).ConfigureAwait(false);

            while (true)
            {
                Console.Write("Input: ");
                
                char c = Console.ReadKey().KeyChar;

                if (c == 'd')
                {
                    comTester.DiconnectAllPeers();
                }
            }            
        }
    }
}