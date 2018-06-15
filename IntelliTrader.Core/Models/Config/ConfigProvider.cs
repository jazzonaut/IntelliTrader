using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;

namespace IntelliTrader.Core
{
    internal class ConfigProvider : IConfigProvider
    {
        private const string ROOT_CONFIG_DIR = "config";
        private const string PATHS_CONFIG_PATH = "paths.json";
        private const string PATHS_SECTION_NAME = "Paths";
        private IConfigurationSection paths;

        public ConfigProvider()
        {
            IConfigurationRoot pathsConfig = GetConfig(PATHS_CONFIG_PATH, changedPathsConfig =>
            {
                paths = changedPathsConfig.GetSection(PATHS_SECTION_NAME);
            });
            paths = pathsConfig.GetSection(PATHS_SECTION_NAME);
        }

        public string GetSectionJson(string sectionName)
        {
            try
            {
                string configPath = paths.GetValue<string>(sectionName);
                var fullConfigPath = Path.Combine(Directory.GetCurrentDirectory(), ROOT_CONFIG_DIR, configPath);
                return File.ReadAllText(fullConfigPath);
            }
            catch (Exception ex)
            {
                Application.Resolve<ILoggingService>().Error($"Unable to load config section {sectionName}", ex);
                return null;
            }
        }

        public void SetSectionJson(string sectionName, string definition)
        {
            try
            {
                string configPath = paths.GetValue<string>(sectionName);
                var fullConfigPath = Path.Combine(Directory.GetCurrentDirectory(), ROOT_CONFIG_DIR, configPath);
                File.WriteAllText(fullConfigPath, definition);
            }
            catch (Exception ex)
            {
                Application.Resolve<ILoggingService>().Error($"Unable to save config section {sectionName}", ex);
            }
        }

        public T GetSection<T>(string sectionName, Action<T> onChange = null)
        {
            IConfigurationSection configSection = GetSection(sectionName, changedConfigSection =>
            {
                onChange?.Invoke(changedConfigSection.Get<T>());
            });
            return configSection.Get<T>();
        }

        public IConfigurationSection GetSection(string sectionName, Action<IConfigurationSection> onChange = null)
        {
            string configPath = paths.GetValue<string>(sectionName);
            IConfigurationRoot configRoot = GetConfig(configPath, changedConfigRoot =>
            {
                onChange?.Invoke(changedConfigRoot.GetSection(sectionName));
            });
            return configRoot.GetSection(sectionName);
        }

        private IConfigurationRoot GetConfig(string configPath, Action<IConfigurationRoot> onChange)
        {
            var fullConfigPath = Path.Combine(Directory.GetCurrentDirectory(), ROOT_CONFIG_DIR);

            var configBuilder = new ConfigurationBuilder()
                 .SetBasePath(fullConfigPath)
                 .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                 .AddEnvironmentVariables();

            var configRoot = configBuilder.Build();
            ChangeToken.OnChange(configRoot.GetReloadToken, () => onChange(configRoot));
            return configRoot;
        }
    }
}
