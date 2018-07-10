namespace AElf.Network.V2.DHT.Helpers
{
    public interface IHasher
    {
        uint Hash(string value);
    }
}