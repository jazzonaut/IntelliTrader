using IntelliTrader.Core;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace IntelliTrader.Web
{
    internal class WebService : ConfigrableServiceBase<WebConfig>, IWebService
    {
        public override string ServiceName => Constants.ServiceNames.WebService;

        IWebConfig IWebService.Config => Config;

        private readonly ILoggingService loggingService;

        private IWebHost webHost;

        public WebService(ILoggingService loggingService)
        {
            this.loggingService = loggingService;
        }

        public void Start()
        {
            loggingService.Info($"Start Web service (Port: {Config.Port})...");

            try
            {
                var contentRoot = Path.GetFullPath(Directory.GetCurrentDirectory() + @"/../IntelliTrader.Web");
#if RELEASE
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "bin");
                }
#endif

                var webHostBuilder = new WebHostBuilder()
                    .UseContentRoot(contentRoot)
                    .UseStartup<Startup>()
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, Config.Port);
                    });

                if (Config.DebugMode)
                {
                    webHostBuilder.UseEnvironment("Development");
                }
                else
                {
                    webHostBuilder.UseEnvironment("Production");
                }

                webHost = webHostBuilder.Build();

                // Suppress WebHost startup messages
                var consOut = Console.Out;
                webHost.Start();
                Console.SetOut(consOut);
            }
            catch (Exception ex)
            {
                loggingService.Error($"Unable to start Web service", ex);
            }

            loggingService.Info($"Web service started");
        }

        public void Stop()
        {
            loggingService.Info($"Stop Web service...");

            try
            {
                webHost.Dispose();
                loggingService.Info($"Web service stopped");
            }
            catch (Exception ex)
            {
                loggingService.Error($"Unable to stop Web service", ex);
            }
        }
    }
}
