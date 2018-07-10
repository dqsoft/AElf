using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Network.V2.DHT.Exceptions;

namespace AElf.Network.V2.DHT.Node
{
    public class NodeStore : INodeStore
    {
        private readonly IDictionary<string, string> _store;

        public NodeStore()
        {
            this._store = new ConcurrentDictionary<string, string>();
        }
        
        public bool ContainsKey(string key)
        {
            return _store.ContainsKey(key);
        }

        public string GetValue(string key)
        {
            _store.TryGetValue(key, out string value);

            return value;
        }

        public bool AddValue(string key, string value)
        {
            if (_store.ContainsKey(key))
            {
                throw new DuplicateKeyException(key, null);
            }

            if (_store.Count >= KademliaOptions.BucketSize)
            {
                throw new StoreFullException(key, null);
            }

            try
            {
                _store.Add(key, value);
            }
            catch
            {
                ;
            }

            return true;
        }

        public bool RemoveValue(string key)
        {
            return _store.Remove(key);
        }
    }
}