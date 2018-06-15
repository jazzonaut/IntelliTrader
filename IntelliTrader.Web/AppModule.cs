using Autofac;
using IntelliTrader.Core;

namespace IntelliTrader.Web
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebService>().As<IWebService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.WebService).SingleInstance();
        }
    }
}
