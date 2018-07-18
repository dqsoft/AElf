using System;
using System.Net;
using System.Net.Sockets;

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
        public event EventHandler PeerUnreachable;

        private MessageReader _messageReader;
        private MessageWriter _messageWriter;

        public int PacketsReceivedCount { get; private set; }
        public int FailedProtocolCount { get; private set; }

        private TcpClient _client;
        //private readonly int _port;
        
        public bool IsAvailable { get; set; }

        /// <summary>
        /// This method set the peers underliying tcp client. This method is intended to be
        /// called when the internal state is clean - meaning that either the object has just
        /// been contructed or has just been closed. 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        public void Initialize(TcpClient client)
        {
            if (_messageReader != null || _messageWriter != null || _client != null)
            {
                Console.WriteLine("Could not initialize, some components aren't cleared.");
            }
            
            try
            {
                _client = client;
            
                var stream = client.GetStream();
            
                MessageReader reader = new MessageReader(stream);
                _messageReader = reader;
            
                MessageWriter writer = new MessageWriter(stream);
                _messageWriter = writer;
            
                _messageReader.Start(); 
                _messageWriter.Start();
            
                // Start listen loop on own thread
                _messageReader.PacketReceived += ClientOnPacketReceived;
                _messageReader.StreamClosed += MessageReaderOnStreamClosed;

                IsAvailable = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while initializing the connection");
            }
        }

        /// <summary>
        /// Called when the underlying stream has be closed, an attempt to reconnect
        /// will be made.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private async void MessageReaderOnStreamClosed(object sender, EventArgs eventArgs)
        {
            Reset();
            
            PeerDialer p = new PeerDialer(IPAddress.Loopback.ToString(), 6789);
            TcpClient client = await p.DialWithRetryAsync();

            if (client != null)
            {
                Initialize(client);
            }
            else
            {
                PeerUnreachable?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Reset()
        {
            if (_messageReader != null)
            {
                _messageReader.PacketReceived -= ClientOnPacketReceived;
                _messageReader.StreamClosed -= MessageReaderOnStreamClosed;
            }
            
            _messageReader?.Close();
            _messageWriter = null;
            
            // todo handle the _message writer
            //_messageWriter.Close();
            _messageWriter = null;
            
            _client?.Close();
            _client = null;
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

        public void Disconnect()
        {
            Reset();
        }
    }
}