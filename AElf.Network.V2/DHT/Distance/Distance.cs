using AElf.Network.V2.DHT.Helpers;

namespace AElf.Network.V2.DHT.Distance
{
    public class Distance
    {
        /// <summary>
        /// Calculates the distance between two hexademical addresses.
        /// </summary>
        /// <param name="addr1Hex"></param>
        /// <param name="addr2Hex"></param>
        /// <returns>Distance as an integer</returns>
        public static int Calculate(string addr1Hex, string addr2Hex)
        {
            // convert hex addresses to binary
            string addr1Bin = BaseTypeConverter.HexToBin(addr1Hex);
            string addr2Bin = BaseTypeConverter.HexToBin(addr2Hex);
            
            return ComputeXor(addr1Bin, addr2Bin); // compute & return distance
        }

        /// <summary>
        /// Computes the bitwise XOR of its operands.
        /// </summary>
        /// <param name="addr1Bin"></param>
        /// <param name="addr2Bin"></param>
        /// <returns>XOR as an integer</returns>
        private static int ComputeXor(string addr1Bin, string addr2Bin)
        {
            int addr1Int = BaseTypeConverter.BinToInt(addr1Bin);
            int addr2Int = BaseTypeConverter.BinToInt(addr2Bin);

            return addr1Int ^ addr2Int;
        }
    }
}