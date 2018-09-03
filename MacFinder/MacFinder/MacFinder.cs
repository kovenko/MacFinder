using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Manufacturer
{
    static class MacFinder
    {
        private static List<Element> _elements = new List<Element>();
        private static bool done = false;

        public static void Load()
        {
            string filePath = $"{Directory.GetCurrentDirectory()}\\MacAddressToManufacturer.json";
            Console.WriteLine($"Load JSON data from file {filePath}");
            var json = File.ReadAllText(filePath);
            _elements = JsonConvert.DeserializeObject<List<Element>>(json);
            Console.WriteLine("Success read file.");
        }

        public static void Run()
        {
            int listenPort = 37008;
            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            Console.WriteLine("Server manufacturer start");
            try
            {
                while (!done)
                {
                    byte[] bytes = listener.Receive(ref groupEP);
                    Console.WriteLine("Received broadcast from {0} :\n {1}\n", groupEP.ToString(),
                        Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                listener.Close();
            }
            Console.WriteLine("Server manufacturer stopped");
        }
    }
}
