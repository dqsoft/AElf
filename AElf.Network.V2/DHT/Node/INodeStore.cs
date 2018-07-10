namespace AElf.Network.V2.DHT.Node
{
    public interface INodeStore
    {
        bool ContainsKey(string key);
        string GetValue(string key);
        bool AddValue(string key, string value);
        bool RemoveValue(string key);
    }
}