using PiServerLite.Http;
using PiServerLite.Http.Content;
using System;
using System.Reflection;

namespace PiServerLite.Sample
{
    internal static class Program
    {
        private static HttpReceiver receiver;


        static int Main()
        {
            var context = new HttpReceiverContext {
                ListenerPath = "/piServer",
            };

            context.ContentDirectories.Add(new ContentDirectory {
                DirectoryPath = ".\\Content",
                UrlPath = "/Content/",
            });

            context.Views.AddFolderFromExternal(".\\Views");

            if (!StartReceiver(context)) {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return 1;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Http Server is running. Press any key to stop...");
            Console.WriteLine();
            Console.ReadKey(true);
            Console.ResetColor();
            Console.WriteLine();

            StopReceiver();

            Console.ResetColor();
            return 0;
        }

        private static bool StartReceiver(HttpReceiverContext context)
        {
            Console.ResetColor();
            Console.WriteLine("Starting Http Server...");

            receiver = new HttpReceiver(context);
            receiver.AddPrefix("http://+:80/piServer/");

            try {
                receiver.Routes.Scan(Assembly.GetExecutingAssembly());

                receiver.Start();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("  Http Server started successfully.");
                return true;
            }
            catch (Exception error) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Failed to start Http Server! {error}");
                return false;
            }
        }

        private static void StopReceiver()
        {
            Console.ResetColor();
            Console.WriteLine("Stopping Http Server...");

            try {
                receiver.Dispose();

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("  Http Server stopped.");
            }
            catch (Exception error) {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"  Error while stopping Http Server! {error}");
            }
        }
    }
}
