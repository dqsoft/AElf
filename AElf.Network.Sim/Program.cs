using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
            string addr1 = "a4";
            string addr2 = "9c";
            Console.WriteLine("Example address #1 = " + addr1);
            Console.WriteLine("Example address #2 = " + addr2);
            Console.WriteLine("XOR distance = " + Distance.Calculate(addr1, addr2));

            // Start comTester on a new thread
            CommunicationTester comTester = new CommunicationTester();
            Task.Run(() => comTester.Start(args[0] == "1"));

            Console.ReadKey();
        }
    }

    // Node
    public class CommunicationTester
    {
        public async void Start(bool isServer)
        {
            /* Start the server or client on it's own thread. */
            
            if (isServer)
            {
                Server s = new Server();
                Task.Run(() => s.StartListningForConnections()).ConfigureAwait(false);
            }
            else
            {
                Client c = new Client();
                await Task.Run(() => c.Start());
                ;
            }
        }
    }

    public class Server
    {
        private Connection _client;
        private BlockingCollection<Packet> _packetQueue = new BlockingCollection<Packet>();
        
        private List<Connection> _connections = new List<Connection>();
        
        // This method is start on another thread
        public async Task StartListningForConnections()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 6789);
            tcpListener.Start();
            
            while (true)
            {
                await AwaitConnection(tcpListener);
            }
        }

        public async Task AwaitConnection(TcpListener tcpListener)
        {
            TcpClient client = await tcpListener.AcceptTcpClientAsync();
            _client = new Connection(client);
            
            _client.PacketReceived += ClientOnPacketReceived;
            
            // Start listen loop on own thread
            Task.Run(ListenLoop).ConfigureAwait(false);
        }

        private int cnt = 0;
        private void ClientOnPacketReceived(object sender, EventArgs eventArgs)
        {
            if (!(eventArgs is PacketReceivedEventArgs a) || a.Packet == null)
            {
                Console.WriteLine("Packet event problem.");
                return;
            }

            _packetQueue.Add(a.Packet);
            
            Console.WriteLine("Enqueued packet : " + Convert.ToBase64String(a.Packet.Data) + ", cnt: " + cnt++);
        }

        public async Task ListenLoop()
        {
            await _client.Read();
            Console.WriteLine("Finished read");
        }
    }

    public class Client
    {
        private Connection _client;
        
        public void Start()
        {
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Loopback, 6789);
            Console.WriteLine("Client connected");
            
            _client = new Connection(tcpClient);

            byte[] type = { 1 };
            byte[] length = BitConverter.GetBytes((ushort) 5);
            byte[] arrData = ByteArrayHelpers.RandomFill(5);
            
            for (int i = 0; i < 1000; i++)
            {
                _client.WriteBytes(type);
                _client.WriteBytes(length);
                _client.WriteBytes(arrData);

                Console.WriteLine("Send" + Convert.ToBase64String(arrData));
            }
            
            /*
             * byte[] type = await ReadBytesAsync(1);
                    int typeInt = BitConverter.ToInt32(type, 0);
                    
                    byte[] sizePrefixe = await ReadBytesAsync(2);
                    ushort packetLength = BitConverter.ToUInt16(sizePrefixe, 0);

                    Console.WriteLine("received : " + packetLength);
                    byte[] packetData = await ReadBytesAsync(packetLength);
                    */
        }
        
        /*public async Task WriteLoop()
        {
            
        }*/
    }
}