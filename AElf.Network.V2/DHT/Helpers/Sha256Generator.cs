using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Network.V2.DHT.Helpers
{
    public class Sha256Generator : IHasher
    {
        private readonly SHA256 _hasher;

        public Sha256Generator()
        {
            _hasher = SHA256.Create();
        }

        public uint Hash(string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentNullException();

            using (MemoryStream stream = GenerateStreamFromString(value))
            {
                var hashBytes = _hasher.ComputeHash(stream);
                return BitConverter.ToUInt32(hashBytes, 0);
            }
        }

        private MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }
    }
}