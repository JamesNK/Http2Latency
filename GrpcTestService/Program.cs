﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace GrpcTestService
{
    internal class Program
    {
        internal class Startup
        {
            private const int GrpcMaxReceiveMessageSizeInMB = 1024 * 1024;

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddGrpc(options =>
                {
                    options.MaxReceiveMessageSize = GrpcMaxReceiveMessageSizeInMB;
                });

                // Filter out Asp Net Core's default Info logging and just display warnings
                services.AddLogging(
                builder =>
                {
                    builder.AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                            .AddConsole();
                });
            }

            // This code configures Web API. The Startup class is specified as a type
            // parameter in the WebAppBuilder Start method.
            public virtual void Configure(IApplicationBuilder appBuilder)
            {
                appBuilder.UseRouting();
                appBuilder.UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<TestProxyService>();
                });
            }
        }

        private static void Main(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder()
                       .UseStartup<Startup>()
                       .UseKestrel(options =>
                       {
                           options.Listen(
                               IPAddress.Any,
                               81,
                               listenOptions =>
                               {
                                   listenOptions.Protocols = HttpProtocols.Http2;
                                   //listenOptions.UseHttps();
                               });
                           //options.Limits.Http2.InitialStreamWindowSize = 4 * 1024 * 1024;
                           //options.Limits.Http2.InitialConnectionWindowSize = 4 * 1024 * 1024;
                       })
                       .Build();

            webHost.Run();
        }
    }
}
