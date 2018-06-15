using Autofac;
using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Trading
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TradingService>().As<ITradingService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.TradingService).SingleInstance();
        }
    }
}
