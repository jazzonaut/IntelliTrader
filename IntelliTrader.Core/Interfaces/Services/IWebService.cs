using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IWebService
    {
        IWebConfig Config { get; }
        void Start();
        void Stop();
    }
}
