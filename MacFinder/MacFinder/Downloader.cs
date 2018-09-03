using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO;

namespace Manufacturer
{
    class Element
    {
        public ulong AssignmentMin { get; set; }
        public ulong AssignmentMax { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationAddress { get; set; }
    }

    static class Downloader
    {
        private static readonly char[] Indicator = new char[] {'|', '/', '-', '\\'};
        private static int _index;
        private static int _windowTop;
        private static int _windowLeft;
        private static int _cursorTop;
        private static int _cursorLeft;
        private static bool _stop;
        private static string _content;
        private static readonly List<Element> Elements = new List<Element>();

        private static void SavePosition()
        {
            _windowTop = Console.WindowTop;
            _windowLeft = Console.WindowLeft;
            _cursorTop = Console.CursorTop;
            _cursorLeft = Console.CursorLeft;
        }

        private static void RestorePosition()
        {
            Console.WindowTop = _windowTop;
            Console.WindowLeft = _windowLeft;
            Console.CursorTop = _cursorTop;
            Console.CursorLeft = _cursorLeft;
        }

        private static void PutIndicator()
        {
            if (_stop) return;
            RestorePosition();
            Console.Write(Indicator[_index++]);
            if (_index >= Indicator.Length) _index = 0;
            Task.Delay(250).ContinueWith(t => PutIndicator());
        }

        private static void StopIndicator(string msg)
        {
            _stop = true;
            _index = 0;
            RestorePosition();
            Console.Write(msg + "\n");
        }

        private static async Task<string> Download(string url)
        {
            _stop = false;
            Console.Write($"Start download {url} ");
            SavePosition();
            PutIndicator();

            HttpClient client = new HttpClient
            {
                Timeout = new TimeSpan(0, 0, 0, 160)
            };
            _content = await client.GetStringAsync(url);
            return _content;
        }

        public static bool GetUrl(string url)
        {
            var result = Download(url).Wait(15000);
            if (result)
            {
                StopIndicator("success.");
                Parser();
                return true;
            }
            StopIndicator("failure.");
            return false;
        }

        private static void Parser()
        {
            Console.Write("Parsing ");

            // Заполнители
            string minAggregate = "000";
            string maxAggregate = "FFF";

            string pBlockType = "(MA-S|MA-M|MA-L),.+";
            Match mBlockType = Regex.Match(_content, pBlockType);
            if (mBlockType.Groups[1].Value == "MA-M")
            {
                minAggregate += "00";
                maxAggregate += "FF";
            }
            else
            {
                minAggregate += "000";
                maxAggregate += "FFF";
            }

            // Строки в массив
            string[] lines = _content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            lines[0] = ""; // Описание в первой строке не должно подходить под шаблон

            string pattern = "^[^,]+,(?<assignment>[^,]+),(\"(?<organization_name>[^\"]+)\"|(?<organization_name>[^,]+)),(\"(?<organization_address>[^\"]+)\"|(?<organization_address>[^,]+)).*$";

            foreach (var line in lines)
            {
                Match m = Regex.Match(line, pattern);
                if (m.Success)
                {
                    // Заполняем список элементами
                    var e = new Element()
                    {
                        AssignmentMin = Convert.ToUInt64(m.Groups["assignment"].Value + minAggregate, 16),
                        AssignmentMax = Convert.ToUInt64(m.Groups["assignment"].Value + maxAggregate, 16),
                        OrganizationName = m.Groups["organization_name"].Value,
                        OrganizationAddress = m.Groups["organization_address"].Value
                    };
                    Elements.Add(e);
                }
            }
            Console.WriteLine("success.");
        }

        public static void SaveToJson()
        {
            string filePath = $"{Directory.GetCurrentDirectory()}\\MacAddressToManufacturer.json";
            Console.WriteLine($"Save JSON data to file {filePath}");
            var json = JsonConvert.SerializeObject(Elements);
            File.WriteAllText(filePath, json);
            Console.WriteLine("Success write file.");
        }
    };
}
