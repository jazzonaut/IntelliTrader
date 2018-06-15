using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public abstract class ConfigrableServiceBase<TConfig> : IConfigurableService
        where TConfig : class
    {
        private const double DELAY_BETWEEN_CONFIG_RELOADS_MILLISECONDS = 500;

        public abstract string ServiceName { get; }

        public TConfig Config
        {
            get
            {
                lock (syncRoot)
                {
                    if (config == null)
                    {
                        config = RawConfig.Get<TConfig>();
                        PrepareConfig();
                    }
                    return config;
                }
            }
        }

        public IConfigurationSection RawConfig
        {
            get
            {
                lock (syncRoot)
                {
                    if (rawConfig == null)
                    {
                        rawConfig = Application.ConfigProvider.GetSection(ServiceName, OnRawConfigChanged);
                    }
                    return rawConfig;
                }
            }
        }

        private TConfig config;
        private IConfigurationSection rawConfig;
        private DateTimeOffset lastReloadDate;
        private object syncRoot = new object();

        protected virtual void PrepareConfig() { }
        protected virtual void OnConfigReloaded() { }

        private void OnRawConfigChanged(IConfigurationSection changedRawConfig)
        {
            lock (syncRoot)
            {
                rawConfig = changedRawConfig;
                config = null;
            }

            if ((DateTimeOffset.Now - lastReloadDate).TotalMilliseconds > DELAY_BETWEEN_CONFIG_RELOADS_MILLISECONDS)
            {
                lastReloadDate = DateTimeOffset.Now;
                PrepareConfig();
                OnConfigReloaded();
                Application.Resolve<ILoggingService>().Info($"{ServiceName} configuration reloaded");
            }
        }
    }
}
