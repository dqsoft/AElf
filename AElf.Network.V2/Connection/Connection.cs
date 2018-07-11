using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AElf.Network.V2.Connection
{
    public class Packet
    {
        public int Type { get; set; } 
        public ushort Length { get; set; } 
        public byte[] Data { get; set; }
    }

    public class PacketReceivedEventArgs : EventArgs
    {
        public Packet Packet { get; set; }
    }
    
    public class Connection
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public event EventHandler PacketReceived;

        public Connection(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }
        
        /// <summary>
        /// Reads the bytes from the stream.
        /// </summary>
        public async Task Read()
        {
            try
            {
                while (true)
                {
                    byte[] type = await ReadBytesAsync(1);
                    byte typeInt = type[0];
                    
                    byte[] sizePrefixe = await ReadBytesAsync(2);
                    ushort packetLength = BitConverter.ToUInt16(sizePrefixe, 0);

                    byte[] packetData = await ReadBytesAsync(packetLength);
                    
                    Packet packet = new Packet();
                    packet.Type = typeInt;
                    packet.Length = packetLength;
                    packet.Data = packetData;
                    
                    PacketReceivedEventArgs args = new PacketReceivedEventArgs();
                    args.Packet = packet;
                    
                    PacketReceived?.Invoke(this, args);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading packet from stream");
            }
        }
        
        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read bytes.</returns>
        protected async Task<byte[]> ReadBytesAsync(int amount)
        {
            try
            {
                if (amount == 0) 
                    return new byte[0];
            
                byte[] requestedBytes = new byte[amount];
            
                int receivedIndex = 0;
                while (receivedIndex < amount)
                {
                    while (_tcpClient.Available == 0)
                        await Task.Delay(TimeSpan.FromMilliseconds(5));

                    int readAmount = (amount - receivedIndex >= _tcpClient.Available) ? _tcpClient.Available : amount - receivedIndex;
                
                    await _stream.ReadAsync(requestedBytes, receivedIndex, readAmount);
                
                    receivedIndex += readAmount;
                }

                return requestedBytes;
            }
            catch (Exception e)
            {
                return new byte[0];
            }
            
            return new byte[0];
        }

        public void WriteBytes(byte[] bytes)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }
        
    }
}