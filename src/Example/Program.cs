using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Paginator;
using Discord.Commands;
using Discord.WebSocket;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        private DiscordSocketClient client;

        public async Task Start()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });

            string token = Environment.GetEnvironmentVariable("discord-foxboat-token");

            client.Log += Log;

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
            client.UsePaginator(map, Log);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
