using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AElf.Network.V2.Connection
{
    public class Connection
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;

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
                /*while (true)
                {*/
                    byte[] sizePrefixe = await ReadBytesAsync(2);
                    ushort packetLength = BitConverter.ToUInt16(sizePrefixe, 0);

                    Console.WriteLine("received : " + packetLength);
                    byte[] packetData = await ReadBytesAsync(packetLength);
                
                    Console.WriteLine("Read" + Convert.ToBase64String(packetData));
                
                //}
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

        public void WriteBytes(byte[] bytes)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }
        
    }
}