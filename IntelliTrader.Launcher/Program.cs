using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

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
                try
                {
                    process.StartInfo.FileName = "dotnet";
                    process.StartInfo.Arguments = processPath;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.Start();
                    SpinWait.SpinUntil(() =>
                    {
                        return process.MainWindowHandle != IntPtr.Zero;
                    }, TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}{Environment.NewLine}{Environment.NewLine}Please download the latest .NET Core Runtime from{Environment.NewLine}https://www.microsoft.com/net/download", nameof(IntelliTrader));
                }

                try
                {
                    if (!String.IsNullOrWhiteSpace(instanceName))
                    {
                        SetWindowText(process.MainWindowHandle, $"{nameof(IntelliTrader)} - {instanceName}");
                    }
                    else
                    {
                        SetWindowText(process.MainWindowHandle, $"{nameof(IntelliTrader)}");
                    }
                }
                catch { }
            }
            else
            {
                MessageBox.Show($"{processFileName} not found", nameof(IntelliTrader));
            }
        }
    }
}