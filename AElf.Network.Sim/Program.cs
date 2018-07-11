using System;
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

    public class CommunicationTester
    {
        public async void Start(bool isServer)
        {
            /* Start the server or client on it's own thread. */
            
            if (isServer)
            {
                Server s = new Server();
                await Task.Run(() => s.Start());
                ;
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
        
        public async Task Start()
        {
            await AwaitConnection();
        }

        public async Task AwaitConnection()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 6789);
            tcpListener.Start();
            
            TcpClient client = await tcpListener.AcceptTcpClientAsync();
            
            _client = new Connection(client);
            
            // Start listen loop on own thread
            Task.Run(ListenLoop).ConfigureAwait(false);
            
            // When a connection is accepted, execution continues
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

            byte[] arr = BitConverter.GetBytes((ushort) 5);
            _client.WriteBytes(arr);

            byte[] arrData = ByteArrayHelpers.RandomFill(5);
            _client.WriteBytes(arrData);

            Console.WriteLine("Send" + Convert.ToBase64String(arrData));

        }
        
        /*public async Task WriteLoop()
        {
            
        }*/
    }
}