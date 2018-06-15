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
        public int Port { get; set; }
    }
}
