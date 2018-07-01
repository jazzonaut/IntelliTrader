using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IWebConfig
    {
        bool Enabled { get; }
        bool DebugMode { get; }
        bool ReadOnlyMode { get; }
        int Port { get; }
        bool SSLEnabled { get; }
        string SSLCertPath { get; set; }
        string SSLCertPassword { get; set; }
    }
}
