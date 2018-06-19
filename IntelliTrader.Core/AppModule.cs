using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TasksService>().As<ITasksService>().SingleInstance();
            builder.RegisterType<HealthCheckService>().As<IHealthCheckService>().SingleInstance();
            builder.RegisterType<CoreService>().As<ICoreService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.CoreService).SingleInstance();
            builder.RegisterType<LoggingService>().As<ILoggingService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.LoggingService).SingleInstance();
            builder.RegisterType<NotificationService>().As<INotificationService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.NotificationService).SingleInstance();
        }
    }
}
