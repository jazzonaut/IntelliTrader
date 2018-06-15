using Autofac;
using Autofac.Core;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IntelliTrader.Core
{
    public class Application
    {
        public readonly static IConfigProvider ConfigProvider = new ConfigProvider();

        public static double Speed { get; set; } = 1;

        public static ILifetimeScope Container
        {
            get
            {
                RegisterComponents();
                return container;
            }
        }

        private static IContainer container;

        public static void RegisterComponents(bool repos = true, bool queries = true, bool mappers = true)
        {
            if (Application.container == null)
            {
                var builder = new ContainerBuilder();

                var assemblyPattern = new Regex($"{nameof(IntelliTrader)}.*.dll");
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => assemblyPattern.IsMatch(Path.GetFileName(a.Location)));
                var dynamicAssembliesPath = new Uri(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)).LocalPath;
                var dynamicAssemblies = Directory.EnumerateFiles(dynamicAssembliesPath, "*.dll", SearchOption.AllDirectories)
                           .Where(filename => assemblyPattern.IsMatch(Path.GetFileName(filename)) &&
                           !loadedAssemblies.Any(a => Path.GetFileName(a.Location) == Path.GetFileName(filename)));

                var allAssemblies = loadedAssemblies.Concat(dynamicAssemblies.Select(Assembly.LoadFrom)).Distinct();

                builder.RegisterAssemblyModules(allAssemblies.ToArray());
                Application.container = builder.Build();
            }
        }

        public static TService Resolve<TService>(params Parameter[] parameters) where TService : class
        {
            return Container.Resolve<TService>(parameters);
        }

        public static TService ResolveNamed<TService>(string name, params Parameter[] parameters) where TService : class
        {
            return Container.ResolveNamed<TService>(name, parameters);
        }

        public static TService ResolveOptional<TService>(params Parameter[] parameters) where TService : class
        {
            return Container.ResolveOptional<TService>(parameters);
        }

        public static TService ResolveOptionalNamed<TService>(string name, params Parameter[] parameters) where TService : class
        {
            return Container.ResolveOptionalNamed<TService>(name, parameters);
        }
    }
}
