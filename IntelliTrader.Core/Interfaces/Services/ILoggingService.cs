using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface ILoggingService : IConfigurableService
    {
        void Debug(string message, Exception exception = null);
        void Debug(string message, params object[] propertyValues);
        void Error(string message, Exception exception = null);
        void Error(string message, params object[] propertyValues);
        void Fatal(string message, Exception exception = null);
        void Fatal(string message, params object[] propertyValues);
        void Info(string message, Exception exception = null);
        void Info(string message, params object[] propertyValues);
        void Verbose(string message, Exception exception = null);
        void Verbose(string message, params object[] propertyValues);
        void Warning(string message, Exception exception = null);
        void Warning(string message, params object[] propertyValues);
        void DeleteAllLogs();
        string[] GetLogEntries();
    }
}
