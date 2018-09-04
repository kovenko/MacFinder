using System;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Manufacturer
{
    class Program
    {
        private static TaskCompletionSource<object> taskToWait;

        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                switch (args[0])
                {
                    case "-update":
                        Console.WriteLine("The public IEEE registry list will be uploaded to the json file");
                        if (Downloader.GetUrl("http://standards-oui.ieee.org/oui36/oui36.csv") && Downloader.GetUrl("http://standards-oui.ieee.org/oui28/mam.csv") && Downloader.GetUrl("http://standards-oui.ieee.org/oui/oui.csv"))
                        {
                            Downloader.SaveToJson();
                        }
                        Task.Delay(2000).Wait();
                        break;
                    case "--help":
                        Console.WriteLine("option -update for update MAC address to manufacturer json file");
                        break;
                    default:
                        break;
                }
            }
            else
            {
                taskToWait = new TaskCompletionSource<object>();
                AssemblyLoadContext.Default.Unloading += SigTermEventHandler;
                Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);
                //eventSource.Subscribe(eventSink) or something...
                taskToWait.Task.Wait();
                AssemblyLoadContext.Default.Unloading -= SigTermEventHandler;
                Console.CancelKeyPress -= new ConsoleCancelEventHandler(CancelHandler);

                MacFinder.Load();
                MacFinder.Run();
                Task.Delay(2000).Wait();
            }
        }

        private static void SigTermEventHandler(AssemblyLoadContext obj)
        {
            System.Console.WriteLine("Unloading...");
            taskToWait.TrySetResult(null);
        }

        private static void CancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            System.Console.WriteLine("Exiting...");
            taskToWait.TrySetResult(null);
        }

    }
}
