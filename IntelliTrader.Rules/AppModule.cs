using Autofac;
using IntelliTrader.Core;
using System;

namespace IntelliTrader.Rules
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RulesService>().As<IRulesService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.RulesService).SingleInstance();
        }
    }
}
