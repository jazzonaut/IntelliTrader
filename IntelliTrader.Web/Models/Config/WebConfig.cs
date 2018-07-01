using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Web
{
    internal class WebConfig : IWebConfig
    {
        public bool Enabled { get; set; }
        public bool DebugMode { get; set; }
        public bool ReadOnlyMode { get; set; }
        public int Port { get; set; }
        public bool SSLEnabled { get; set; }
        public string SSLCertPath { get; set; }
        public string SSLCertPassword { get; set; }
    }
}
