using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AElf.Network.V2.Connection
{
    public class PeerDialer
    {
        public const int CONNECTION_TIMEOUT = 3000;
        
        private readonly string _ipAddress;
        private readonly int _port;

        public PeerDialer(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public async Task<Connection> DialAsync()
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                Task timeoutTask = Task.Delay(CONNECTION_TIMEOUT);
                Task connectTask = Task.Factory.StartNew(() => tcpClient.Connect(_ipAddress, _port));

                if (await Task.WhenAny(timeoutTask, connectTask) != timeoutTask && tcpClient.Connected)
                    return new Connection(tcpClient);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during connection - timeout ");
            }

            return null;
        }
    }
}