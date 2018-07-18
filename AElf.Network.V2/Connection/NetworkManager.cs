using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Network.V2.Connection;

namespace AElf.Network.Sim
{
    public class NetMessageReceivedArgs : EventArgs
    {
        public Message Message { get; set; }
    }
    
    public class NetworkManager
    {
        public event EventHandler MessageReceived;
        
        // Packets awaiting to be processed
        //private BlockingCollection<Packet> _packetQueue = new BlockingCollection<Packet>();
        private List<NetPeer> _currentPeers = new List<NetPeer>();

        private ConnectionListner _connectionListener;

        private int _port = 6789;
        private List<int> _bootnodePorts = new List<int> {6788, 6789};

        private BlockingCollection<NetworkJob> _outboundJobs;

        public NetworkManager()
        {
            _outboundJobs = new BlockingCollection<NetworkJob>();
            _connectionListener = new ConnectionListner();
        }

        public void Start(int port)
        {
            // Start listening
            
            try
            {
                _port = port;
                
                // Hook up the event
                _connectionListener.IncomingConnection += ConnectionListenerOnIncomingConnection;
                _connectionListener.ListeningStopped += ConnectionListenerOnListeningStopped;
                
                // Start listening for connections
                Task.Run(() => _connectionListener.StartListening(port)).ConfigureAwait(false);
                
                // Start processing jobs
                Task.Run(() => JobConsumerLoop()).ConfigureAwait(false);

                /*if (isClient)
                {
                    PeerDialer dialer = new PeerDialer(IPAddress.Loopback.ToString(), bootnodePort);
                    TcpClient c = dialer.Dial();
                    
                    NetPeer peer = CreatePeerFromConnection(c);
                    
                    Message p = new Message();
                    p.Type = 1;
                    p.Length = 5;
                    p.Payload = ByteArrayHelpers.RandomFill(200001);

                    for (int i = 0; i < 1; i++)
                    {
                        peer.SendMessage(p);
                    }
                }*/
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        internal async Task ConnectToBootnodes()
        {
            foreach (var bt in _bootnodePorts.Where(b => b != _port))
            {
                PeerDialer dialer = new PeerDialer(IPAddress.Loopback.ToString(), bt);
                await Task.Delay(90);
            }
        }

        private void ConnectionListenerOnListeningStopped(object sender, EventArgs eventArgs)
        {
           Console.WriteLine("Listening stoped");
        }
        
        // Connection added callback, will be executed on a different thread
        private void ConnectionListenerOnIncomingConnection(object sender, EventArgs eventArgs)
        {
            if (eventArgs is IncomingConnectionArgs inc && inc.Client != null)
            {
                NetPeer np = CreatePeerFromConnection(inc.Client);
                
                _currentPeers.Add(np);
            }
        }

        private NetPeer CreatePeerFromConnection(TcpClient client)
        {
            MessageReader reader = new MessageReader(client);
            MessageWriter writer = new MessageWriter(client);
            
            NetPeer netPeer = new NetPeer(reader, writer);
            netPeer.Initialize();
                
            netPeer.MessageReceived += NetPeerOnMessageReceived;
                
            _currentPeers.Add(netPeer);

            return netPeer;
        }

        private void NetPeerOnMessageReceived(object sender, EventArgs eventArgs)
        {
            if (eventArgs is PeerMessageReceivedArgs p && p.Peer != null)
            {
                //Console.WriteLine("NetManager : Fireing on thread : " + Thread.CurrentThread.ManagedThreadId);
                MessageReceived?.Invoke(this, new NetMessageReceivedArgs { Message = p.Message});
            }
        }

        /*public void StartDequeingJob()
        {
            Task.Run(() => JobConsumerLoop()).ConfigureAwait(false);
        }*/

        // Worker dequing and processing jobs
        private void JobConsumerLoop()
        {
            while (true)
            {
                NetworkJob j = null;

                try
                {
                    //Console.WriteLine("Consumer THREAD : " + Thread.CurrentThread.ManagedThreadId);
                    
                    j = _outboundJobs.Take();
                    
                    //if (_currentPeers == null || !_currentPeers.Any())
                    //    Console.WriteLine("Bad peer list");
                    //else
                    foreach (var p in _currentPeers)
                    {
                        p.SendMessage(j.Message);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("error while dequeuing");
                }
            }

            ;
        }

        public void EnqueueJob(NetworkJob job)
        {
            try
            {
                _outboundJobs.Add(job);
                //Console.WriteLine("enqueue THREAD : " + Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /*public async Task DequeueLoop()
        {
            while (true)
            {
                try
                {
                    Packet p = _packetQueue.Take();
                    //Console.WriteLine("DQed packet : " + Convert.ToBase64String(p.Data) + ", cnt: " + cnt++);
                }
                catch (Exception e)
                {
                    Console.WriteLine("DeQ error");
                }
            }
        }*/
    }
}