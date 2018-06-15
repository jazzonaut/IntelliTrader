using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using IntelliTrader.Core;

namespace IntelliTrader.Signals.Base
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SignalsService>().As<ISignalsService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.SignalsService).SingleInstance().PreserveExistingDefaults();
        }
    }
}
