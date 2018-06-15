using Autofac;
using IntelliTrader.Core;
using IntelliTrader.Signals.Base;

namespace IntelliTrader.Signals.TradingView
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TradingViewCryptoSignalReceiver>().As<ISignalReceiver>().Named<ISignalReceiver>(nameof(TradingViewCryptoSignalReceiver));
        }
    }
}
