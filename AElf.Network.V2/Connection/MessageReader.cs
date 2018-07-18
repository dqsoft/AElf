using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;

namespace AElf.Network.V2.Connection
{
    public class Message
    {
        public int Type { get; set; } 
        public int Length { get; set; } 
        
        public byte[] Payload { get; set; }
    }

    public class PartialPacket
    {
        public int Position { get; set; }
        public bool IsEnd { get; set; }
        public int TotalDataSize { get; set; }
        
        public byte[] Data { get; set; }
    }

    public class PacketReceivedEventArgs : EventArgs
    {
        public Message Message { get; set; }
    }
    
    public class MessageReader
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public event EventHandler PacketReceived;

        public readonly List<PartialPacket> _partialPacketBuffer;
        
        public MessageReader(TcpClient tcpClient)
        {
            _partialPacketBuffer = new List<PartialPacket>();
            
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }
        
        public void Start()
        {
            Task.Run(Read).ConfigureAwait(false);
        }

        private const int TYPE_LENGTH = 1;
        private const int INT_LENGTH = 4;
        
        /// <summary>
        /// Reads the bytes from the stream.
        /// </summary>
        public async Task Read()
        {
            try
            {
                Console.WriteLine("Read started !");
                
                while (true)
                {
                    // Read type 
                    int type = await ReadByte();
                    
                    // Is this a partial reception ?
                    bool isBuffered = await ReadBoolean();
                    
                    // Read the size of the data
                    int length = await ReadInt();
                    
                    if (isBuffered)
                    {
                        // If it's a partial packet read the packet info
                        PartialPacket partialPacket = await ReadPartialPacket(length);
                        
                        // todo property control
                        
                        if (!partialPacket.IsEnd)
                        {
                            _partialPacketBuffer.Add(partialPacket);
                            Console.WriteLine($"[Packet reception] partial - type : {type}, isBuffered : {isBuffered}, length : {length}");
                        }
                        else
                        {
                            // This is the last packet
                            // Concat all data 
                            
                            _partialPacketBuffer.Add(partialPacket);

                            byte[] allData =
                                ByteArrayHelpers.Combine(_partialPacketBuffer.Select(pp => pp.Data).ToArray());
                            
                            Console.WriteLine($"[Packet reception] partial - partials : {_partialPacketBuffer.Count}, total length : {allData.Length}");

                            // Clear the buffer for the next partial to receive 
                            _partialPacketBuffer.Clear();
                            
                            Message message = new Message { Type = type, Length = allData.Length, Payload = allData };
                            FireMessageReceivedEvent(message);
                        }
                    }
                    else
                    {
                        // If it's not a partial packet the next "length" bytes should be 
                        // the entire data
                        
                        byte[] packetData = await ReadBytesAsync(length);
                        
                        Console.WriteLine($"[Packet reception] normal - type : {type}, isBuffered : {isBuffered}, length : {length}");
                        
                        Message message = new Message { Type = type, Length = length, Payload = packetData };
                        FireMessageReceivedEvent(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("EX : Reading packet from stream");
            }
        }

        private void FireMessageReceivedEvent(Message message)
        {
            PacketReceivedEventArgs args = new PacketReceivedEventArgs { Message = message };

            PacketReceived?.Invoke(this, args);
        }

        private async Task<int> ReadByte()
        {
            byte[] type = await ReadBytesAsync(1);
            return type[0];
        }

        private async Task<int> ReadInt()
        {
            byte[] intBytes = await ReadBytesAsync(INT_LENGTH);
            return BitConverter.ToInt32(intBytes, 0);
        }

        private async Task<bool> ReadBoolean()
        {
            byte[] isBuffered = await ReadBytesAsync(1);
            return isBuffered[0] != 0;
        }

        private async Task<PartialPacket> ReadPartialPacket(int dataLength)
        {
            PartialPacket partialPacket = new PartialPacket();

            partialPacket.Position = await ReadInt();
            partialPacket.IsEnd = await ReadBoolean();
            partialPacket.TotalDataSize = await ReadInt();
            
            // Read the data
            byte[] packetData = await ReadBytesAsync(dataLength);
            partialPacket.Data = packetData;
            
            return partialPacket;
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
                {
                    Console.WriteLine("Read amount is 0");
                    return new byte[0];
                }
                
                byte[] requestedBytes = new byte[amount];
                
                int receivedIndex = 0;
                while (receivedIndex < amount)
                {
                    int readAmount = await _stream.ReadAsync(requestedBytes, receivedIndex, amount - receivedIndex);
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
    }
}