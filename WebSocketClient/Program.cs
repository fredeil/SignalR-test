using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Linq;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace WebSocketClient
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var connections = new List<HubConnection>();
            for (int i = 0; i < 25; i++)
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:18000/fredrik")
                    .WithAutomaticReconnect()
                    .Build();

                connections.Add(connection);
            }

            try
            {
                await Task.WhenAll(from c in connections
                                   select c.StartAsync(cts.Token));

                await Task.WhenAll(from c in connections
                                   select StreamingEcho(c, cts.Token));
            }
            catch (System.OperationCanceledException)
            {
                Console.WriteLine("Cancellation requested..");
            }

            await Task.WhenAll(from c in connections
                               select c.DisposeAsync());

            Console.WriteLine("Goodbye :(");
            return 0;
        }

        public static async Task StreamingEcho(HubConnection connection, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<string>();

            _ = Task.Run(async () =>
            {
                for (var i = 0; i < 5; i++)
                {
                    await channel.Writer.WriteAsync(RandomString(4 * 1024), cancellationToken);
                }
            });

            var outputs = await connection.StreamAsChannelAsync<string>("StreamEcho", channel.Reader, cancellationToken);
            while (await outputs.WaitToReadAsync(cancellationToken))
            {
                while (outputs.TryRead(out var item))
                {
                    Console.WriteLine($"Recv: '{item}'.");
                }
            }
        }

        static string RandomString(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            
            var res = new StringBuilder();
            using var rng = new RNGCryptoServiceProvider();

            byte[] uintBuffer = new byte[sizeof(uint)];
            while (length-- > 0)
            {
                rng.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                res.Append(valid[(int)(num % (uint)valid.Length)]);
            }

            return res.ToString();
        }
    }
}
