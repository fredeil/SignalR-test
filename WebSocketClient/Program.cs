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

namespace WebSocketClient
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var connections = new List<HubConnection>();
            for (int i = 0; i <= 100; i++)
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:18000/fredrik")
                    .WithAutomaticReconnect()
                    .Build();

                connections.Add(connection);

               // await connection.StartAsync();
            }

            await Task.WhenAll(from c in connections
                               select c.StartAsync());

            await Task.WhenAll(from c in connections
                               select StreamingEcho(c));

            return 0;
        }

        public static async Task StreamingEcho(HubConnection connection)
        {
            var channel = Channel.CreateUnbounded<string>();

            _ = Task.Run(async () =>
            {
                for (var i = 0; i < 5; i++)
                {
                    await channel.Writer.WriteAsync(RandomString(4 * 1024));
                }
            });

            var outputs = await connection.StreamAsChannelAsync<string>("StreamEcho", channel.Reader);

            while (await outputs.WaitToReadAsync())
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
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }

            return res.ToString();
        }
    }
}
