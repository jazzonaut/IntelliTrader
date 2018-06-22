using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Threading;

namespace IntelliTrader.Launcher
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        static void Main(string[] args)
        {
            string instanceName = args.Length == 1 ? args[0] : null;
            string processFileName = $"{nameof(IntelliTrader)}.dll";
            string processPath = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), processFileName, SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                Process process = new Process();
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = processPath;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.Start();
                SpinWait.SpinUntil(delegate
                {
                    return process.MainWindowHandle != IntPtr.Zero;
                });

                if (!String.IsNullOrWhiteSpace(instanceName))
                {
                    SetWindowText(process.MainWindowHandle, $"{nameof(IntelliTrader)} - {instanceName}");
                }
                else
                {
                    SetWindowText(process.MainWindowHandle, $"{nameof(IntelliTrader)}");
                }
            }
            else
            {
                throw new FileNotFoundException($"{processFileName} not found", processFileName);
            }
        }
    }
}