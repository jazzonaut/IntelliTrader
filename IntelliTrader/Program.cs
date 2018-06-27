using Autofac;
using ExchangeSharp;
using IntelliTrader.Core;
using System;
using System.Collections.Generic;

namespace IntelliTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsedArgs = ParseCommandLineArgs(args);
            if (parsedArgs.Count == 0)
            {
                PringWelcome();
                StartCoreService();
            }
            else
            {
                if (parsedArgs.ContainsKey("encrypt") && parsedArgs.ContainsKey("path") &&
                    parsedArgs.ContainsKey("publickey") && parsedArgs.ContainsKey("privatekey"))
                {
                    EncryptKeys(parsedArgs);
                }
                else
                {
                    PrintUsage();
                }
            }
        }

        private static void StartCoreService()
        {
            var coreService = Application.Resolve<ICoreService>();
            coreService.Start();
            Console.ReadLine();
            coreService.Stop();
        }

        private static void PringWelcome()
        {
            var foregroundColorBackup = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine(@"  _____         _          _  _  _  _____                   _             ");
            Console.WriteLine(@"  \_   \ _ __  | |_   ___ | || |(_)/__   \ _ __   __ _   __| |  ___  _ __ ");
            Console.WriteLine(@"   / /\/| '_ \ | __| / _ \| || || |  / /\/| '__| / _` | / _` | / _ \| '__|");
            Console.WriteLine(@"/\/ /_  | | | || |_ |  __/| || || | / /   | |   | (_| || (_| ||  __/| |   ");
            Console.WriteLine(@"\____/  |_| |_| \__| \___||_||_||_| \/    |_|    \__,_| \__,_| \___||_|   ");
            Console.WriteLine();
            Console.WriteLine("Welcome to IntelliTrader, The Intelligent Cryptocurrency Trading Bot.");
            Console.WriteLine("Always use Enter/Return key to exit the program to avoid corrupting the data.");
            Console.WriteLine();
            Console.ForegroundColor = foregroundColorBackup;
        }

        private static void EncryptKeys(Dictionary<string, string> args)
        {
            var path = args["path"];
            var publicKey = args["publickey"];
            var privateKey = args["privatekey"];

            CryptoUtility.SaveUnprotectedStringsToFile(path, new string[] { publicKey, privateKey });
            Console.WriteLine("All done! Press any key to exit...");
            Console.ReadKey();
        }

        private static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet IntelliTrader.dll --encrypt --path=<output_path> --publickey=<public_key> --privatekey=<private_key>");
            Console.WriteLine("The encrypted file is only valid for the current user and only on the computer it is created on.");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static Dictionary<string, string> ParseCommandLineArgs(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string a in args)
            {
                int idx = a.IndexOf('=');
                string key = (idx < 0 ? a.TrimStart('-') : a.Substring(0, idx)).ToLowerInvariant().TrimStart('-');
                string value = (idx < 0 ? string.Empty : a.Substring(idx + 1));
                dict[key] = value;
            }
            return dict;
        }
    }
}
