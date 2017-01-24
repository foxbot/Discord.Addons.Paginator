using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Paginator;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        private DiscordSocketClient client;

        public async Task Start()
        {
            var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.ColoredConsole(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] ({SourceContext}) {Message}{NewLine}{Exception}")
                .CreateLogger();
            Log.Logger = log;

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });

            string token = Environment.GetEnvironmentVariable("discord-foxboat-token");

            client.Log += (msg) =>
            {
                // (this is a bad example, don't copy this: )
                Log.Write(LogEventLevel.Information, msg.ToString());
                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.Bot, token);
            await client.ConnectAsync();

            var map = new DependencyMap();
            ConfigureServices(map);
            await new CommandHandler().Install(map);

            await Task.Delay(-1);
        }

        public void ConfigureServices(IDependencyMap map)
        {
            map.Add(client);
            map.Add(new PaginationService(client));
        }
    }
}
