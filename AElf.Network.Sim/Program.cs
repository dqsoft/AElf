using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Xsl;
using AElf.Network.V2.DHT.Distance;

namespace AElf.Network.Sim
{
    class Program
    {
        static void Main(string[] args)
        {
            string addr1 = "a4";
            string addr2 = "9c";
            Console.WriteLine("Example address #1 = " + addr1);
            Console.WriteLine("Example address #2 = " + addr2);
            Console.WriteLine("XOR distance = " + Distance.Calculate(addr1, addr2));

            // Start comTester on a new thread
            CommunicationTester comTester = new CommunicationTester();
            Task.Run(() => comTester.Start());

            Console.ReadKey();
        }
    }

    public class CommunicationTester
    {
        public async void Start()
        {
            
        }
    }
}