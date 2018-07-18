using System;
using System.Globalization;
using System.Threading;

namespace AElf.Network.V2.Connection
{
    public class PeerMessageReceivedArgs : EventArgs
    {
        public NetPeer Peer { get; set; }
        public Message Message { get; set; }
    }
        
    public class NetPeer
    {
        public event EventHandler MessageReceived;

        private readonly MessageReader _messageReader;
        private readonly MessageWriter _messageWriter;

        public int PacketsReceivedCount { get; private set; }
        public int FailedProtocolCount { get; private set; }

        public NetPeer(MessageReader messageReader, MessageWriter messageWriter)
        {
            _messageReader = messageReader;
            _messageWriter = messageWriter;
        }

        public void Initialize()
        {
            // todo should maybe not start their own thread
            _messageReader.Start(); 
            _messageWriter.Start();
            
            // Start listen loop on own thread
            _messageReader.PacketReceived += ClientOnPacketReceived;
        }

        private void ClientOnPacketReceived(object sender, EventArgs eventArgs)
        {
            try
            {
                if (!(eventArgs is PacketReceivedEventArgs a) || a.Message == null)
                {
                    return;
                }

                PacketsReceivedCount++;
            
                FireMessageReceived(a.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void FireMessageReceived(Message p)
        {
            MessageReceived?.Invoke(this, new PeerMessageReceivedArgs { Peer = this, Message = p });
        }

        public void SendMessage(Message data)
        {
            try
            {
                _messageWriter.EnqueueWork(data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}