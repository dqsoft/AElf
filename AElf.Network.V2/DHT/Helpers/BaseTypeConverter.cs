using System;

namespace AElf.Network.V2.DHT.Helpers
{
    public class BaseTypeConverter
    {
        /// <summary>
        /// Converts a hexadecimal value to a binary string.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>Binary string</returns>
        public static string HexToBin(string hex)
        {
            return Convert.ToString(Convert.ToInt32(hex, 16), 2);
        }
        
        /// <summary>
        /// Converts a binary value to a 32-bit integer.
        /// </summary>
        /// <param name="binary"></param>
        /// <returns>Integer</returns>
        public static int BinToInt(string binary)
        {
            return Convert.ToInt32(binary, 2);
        }
    }
}