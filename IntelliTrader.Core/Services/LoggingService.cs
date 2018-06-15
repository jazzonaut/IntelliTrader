using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IntelliTrader.Core
{
    internal class LoggingService : ConfigrableServiceBase<LoggingConfig>, ILoggingService
    {
        private int LOG_ENTRIES_MAX_LENGTH = 50000;

        public override string ServiceName => Constants.ServiceNames.LoggingService;

        private Logger logger;
        private StringWriter writer;
        private StringBuilder writerStringBuilder;
        private string logsPath;

        private readonly object syncRoot = new object();

        public LoggingService()
        {
            if (Config.Enabled)
            {
                logger = CreateLogger();
            }
        }

        public void Verbose(string message, Exception exception = null)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Verbose(exception, message);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Verbose(string message, params object[] propertyValues)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Verbose(message, propertyValues);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Debug(string message, Exception exception = null)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Debug(exception, message);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Debug(string message, params object[] propertyValues)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Debug(message, propertyValues);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Info(string message, Exception exception = null)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Information(exception, message);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Info(string message, params object[] propertyValues)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Information(message, propertyValues);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Warning(string message, Exception exception = null)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Warning(exception, message);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Warning(string message, params object[] propertyValues)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Warning(message, propertyValues);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Error(string message, Exception exception = null)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Error(exception, message);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Error(string message, params object[] propertyValues)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Error(message, propertyValues);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Fatal(string message, Exception exception = null)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Fatal(exception, message);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void Fatal(string message, params object[] propertyValues)
        {
            lock (syncRoot)
            {
                if (Config.Enabled)
                {
                    logger.Fatal(message, propertyValues);
                    CleanUpOldLogEntries();
                }
            }
        }

        public void DeleteAllLogs()
        {
            lock(syncRoot)
            {
                logger.Dispose();
                Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), logsPath), true);
                logger = CreateLogger();
            }
        }

        public string[] GetLogEntries()
        {
            lock (syncRoot)
            {
                if (writer != null)
                {
                    writer.Flush();
                    return writer.GetStringBuilder().ToString().Split(new string[] { writer.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    return new string[0];
                }
            }
        }

        protected override void OnConfigReloaded()
        {
            lock (syncRoot)
            {
                logger = CreateLogger();
            }
        }

        private Logger CreateLogger()
        {
            lock (syncRoot)
            {
                string outputTemplate = GetConfigValue("outputTemplate", RawConfig.GetChildren());
                string filterExpression = GetConfigValue("expression", RawConfig.GetChildren());
                string pathFormat = GetConfigValue("pathFormat", RawConfig.GetChildren());
                logsPath = Path.GetDirectoryName(pathFormat);

                writerStringBuilder = new StringBuilder();
                writer = new StringWriter(writerStringBuilder);

                return new LoggerConfiguration()
                    .ReadFrom.ConfigurationSection(RawConfig)
                    .WriteTo.Logger(config => config.WriteTo.Memory(writer, LogEventLevel.Information, outputTemplate).Filter.ByIncludingOnly(filterExpression))
                    .CreateLogger();
            }
        }

        private void CleanUpOldLogEntries()
        {
            lock (syncRoot)
            {
                if (writerStringBuilder.Length > LOG_ENTRIES_MAX_LENGTH)
                {
                    writerStringBuilder.Remove(0, writerStringBuilder.Length - LOG_ENTRIES_MAX_LENGTH);
                }
            }
        }

        private string GetConfigValue(string key, IEnumerable<IConfigurationSection> sections)
        {
            foreach (var section in sections)
            {
                if (section.Key == key)
                {
                    return section.Value;
                }
                else
                {
                    string value = GetConfigValue(key, section.GetChildren());
                    if (value != null)
                    {
                        return value;
                    }
                }
            }
            return null;
        }
    }
}
