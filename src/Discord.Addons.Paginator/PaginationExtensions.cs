using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Discord.Addons.Paginator
{
    public static class PaginationExtensions
    {
        public static DiscordSocketClient UsePaginator(this DiscordSocketClient client, IDependencyMap map, Func<LogMessage, Task> logger = null)
        {
            map.Add(new PaginationService(client, logger));
            return client;
        }
    }
}
