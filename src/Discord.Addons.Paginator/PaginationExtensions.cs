using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Discord.Addons.Paginator
{
    public static class PaginationExtensions
    {
        [Obsolete("Addon builders on a client are discouraged, consider IServiceCollection.AddPaginator()")]
        public static DiscordSocketClient UsePaginator(this DiscordSocketClient client, IServiceCollection collection, Func<LogMessage, Task> logger = null)
        {
            collection.AddSingleton(new PaginationService(client, logger));
            return client;
        }
        /// <summary>
        /// Adds a PaginationService to a ServiceCollection
        /// </summary>
        /// <param name="collection">The service collection</param>
        /// <param name="client">The client this paginator will use</param>
        /// <param name="logger">A logging delegate</param>
        /// <returns>The service collection, with the pagiantor appended to it (for fluent patterns)</returns>
        public static IServiceCollection AddPaginator(this IServiceCollection collection, DiscordSocketClient client, Func<LogMessage, Task> logger = null)
        {
            collection.AddSingleton(new PaginationService(client, logger));
            return collection;
        }
        /// <summary>
        /// Adds a PaginationService to a ServiceCollection, assuming a DiscordSocketClient is already present in the collection, and that no logging method is wanted.
        /// </summary>
        /// <param name="collection">The service collection.</param>
        /// <returns>The service collection, with the pagiantor appended to it (for fluent patterns)</returns>
        public static IServiceCollection AddPaginator(this IServiceCollection collection)
        {
            collection.AddSingleton<PaginationService>();
            return collection;
        }
    }
}
