using System;

namespace AElf.Network.V2.DHT.Exceptions
{
    public class StoreFullException : Exception
    {
        public StoreFullException(string msg, Exception e) : base(msg, e)
        {
        }
    }
}