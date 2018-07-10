using System;

namespace AElf.Network.V2.DHT.Exceptions
{
    public class DuplicateKeyException : Exception
    {
        public DuplicateKeyException(string msg, Exception e) : base(msg, e)
        {
        }
    }
}