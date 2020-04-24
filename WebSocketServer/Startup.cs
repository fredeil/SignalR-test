using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebSocketTest
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                // Faster pings for testing
                options.KeepAliveInterval = TimeSpan.FromSeconds(5);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => 
            {
                endpoints.MapHub<Chubby>("/fredrik");
            });
        }
    }

    public class Chubby : Hub
    {
        static int numConnections = 0;

        public override Task OnConnectedAsync() 
        {
            Interlocked.Increment(ref numConnections);
            Console.WriteLine($"New connection({numConnections}): "+ Context.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Interlocked.Decrement(ref numConnections);
            Console.WriteLine("Disconnected: "+ Context.ConnectionId);
            return Task.CompletedTask;
        }

        public ChannelReader<string> StreamEcho(ChannelReader<string> source)
        {
            var output = Channel.CreateUnbounded<string>();

            _ = Task.Run(async () =>
            {
                while (await source.WaitToReadAsync())
                {
                    while (source.TryRead(out var item))
                    {
                        await output.Writer.WriteAsync($"[{Context.ConnectionId.Substring(0, 5)}]: " + item);
                    }
                }

                output.Writer.Complete();
            });

            return output.Reader;
        }
    }
}
