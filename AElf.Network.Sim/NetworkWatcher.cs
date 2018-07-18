using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Xsl;
using AElf.Common.ByteArrayHelpers;
using AElf.Network.V2.Connection;

namespace AElf.Network.Sim
{
    public class NetworkWatcher
    {
        private BlockingCollection<Message> _netReceivedQueue = new BlockingCollection<Message>();
        private NetworkManager _netManager;
        
        private Timer _t;
        
        public void Start(string port)
        {
            if (string.IsNullOrWhiteSpace(port))
            {
                throw new ArgumentNullException(nameof(port));
            }

            int listeningPort = int.Parse(port);
            
            _netManager = new NetworkManager();
            _netManager.MessageReceived += NetManagerOnMessageReceived;
            
            Task.Run(() => _netManager.Start(listeningPort)).ConfigureAwait(false);
            Task.Run(() => DoMainLoop()).ConfigureAwait(false);
        }

        private void NetManagerOnMessageReceived(object sender, EventArgs eventArgs)
        {
            if (eventArgs is NetMessageReceivedArgs np && np.Message != null)
            {
                //Console.WriteLine("NetWatcher : Adding on thread : " + Thread.CurrentThread.ManagedThreadId);

                _netReceivedQueue.Add(np.Message);
            }
        }

        private int cnt = 0;
        private void DoMainLoop()
        {

            foreach (var packet in _netReceivedQueue.GetConsumingEnumerable())
            {
                cnt++;
                    
                if (cnt % 100 == 0)
                    Console.WriteLine(cnt);

                Console.WriteLine(packet.Payload.Length);
                
                //Thread.Sleep(TimeSpan.FromSeconds(1));
                
                //Message reply = GetReply();
                //_netManager.EnqueueJob(new NetworkJob {Message = reply});
            }
            
            /*while (true)
            {
                try
                {
                    Packet p = _netReceivedQueue.Take();

                    //Console.WriteLine("MAIN loop, dequeud an packet : " + p.Data + ", reception q : " + _netReceivedQueue.Count + ", on thread: " + Thread.CurrentThread.ManagedThreadId);

                    Interlocked.Increment(ref cnt);
                    
                    if (cnt % 100 == 0)
                        Console.WriteLine(cnt); 

                    //Thread.Sleep(TimeSpan.FromMilliseconds(5));

                    Packet reply = GetReply();
                    _netManager.EnqueueJob(new NetworkJob {Packet = reply});
                }
                catch (Exception e)
                {
                    Console.WriteLine("EX : Dequeue exception");
                }
            }*/
        }
        
        public Message GetReply()
        {
            Message message = new Message
            {
                Type = 1,
                Length = 5,
                Payload = new byte[] { 1,2,3,4,5 }
            };

            return message;
        }
    }
}