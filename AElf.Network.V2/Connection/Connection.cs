using System.Net.Sockets;

namespace AElf.Network.V2.Connection
{
    public class Connection
    {
        private TcpClient _tcpClient;

        public Connection(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }
    }
}