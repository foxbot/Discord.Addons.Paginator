using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Discord.Addons.Paginator
{
    public class PaginationService
    {
        const string FIRST = "⏮";
        const string BACK = "◀";
        const string NEXT = "▶";
        const string END = "⏭";
        const string JUMP = "🔢";
        const string STOP = "⏹";
        const string INFO = "ℹ";

        internal readonly Log Log = new Log("Paginator");
        internal readonly Func<LogMessage, Task> WriteLog;

        private readonly Dictionary<ulong, PaginatedMessage> _messages;
        private readonly DiscordSocketClient _client;

        public PaginationService(DiscordSocketClient client, Func<LogMessage, Task> logger = null)
        {
            WriteLog = logger ?? new Func<LogMessage, Task>((m) => Task.CompletedTask);
            WriteLog(Log.Debug("Creating new service"));
            _messages = new Dictionary<ulong, PaginatedMessage>(); 
            _client = client;
            _client.ReactionAdded += OnReactionAdded;
            WriteLog(Log.Debug("client.ReactionAdded hooked"));
        }

        /// <summary>
        /// Sends a paginated message (with reaction buttons)
        /// </summary>
        /// <param name="channel">The channel the message should be sent to.</param>
        /// <param name="pages">A collection of pages to send to the channel. Each element in this collection represents one page.</param>
        /// <param name="user">If set, this limits the paginated message to only be accessible by a given user.</param>
        /// <param name="language">If set, this highlights the code block the page is encapsulated in with the given language.</param>
        /// <exception cref="Net.HttpException">Thrown if the bot user cannot send a message or add reactions.</exception>
        /// <returns>The paginated message.</returns>
        public async Task<IUserMessage> SendPaginatedMessage(IMessageChannel channel, IReadOnlyCollection<string> pages, IUser user = null, string language = "")
        {
            await WriteLog(Log.Info($"Sending message to {channel}"));
            var paginated = new PaginatedMessage(pages, user, language);

            var message = await channel.SendMessageAsync(paginated.ToString());

            await message.AddReactionAsync(FIRST);
            await message.AddReactionAsync(BACK);
            await message.AddReactionAsync(NEXT);
            await message.AddReactionAsync(END);
            //await message.AddReactionAsync(JUMP);
            await message.AddReactionAsync(STOP);
            //await message.AddReactionAsync(INFO);

            _messages.Add(message.Id, paginated);
            await WriteLog(Log.Debug("Listening to message with id {id}"));

            return message;
        }

        internal async Task OnReactionAdded(ulong id, Optional<SocketUserMessage> messageParam, SocketReaction reaction)
        {
            PaginatedMessage page;
            var message = messageParam.GetValueOrDefault();
            if (message == null)
            {
                await WriteLog(Log.Verbose($"Dumped message (not in cache) with id {id}"));
                return;
            }
            if (!reaction.User.IsSpecified)
            {
                await WriteLog(Log.Verbose($"Dumped message (invalid user) with id {id}"));
                return;
            }
            if (_messages.TryGetValue(message.Id, out page))
            {
                if (reaction.UserId == _client.CurrentUser.Id) return;
                if (page.User != null && reaction.UserId != page.User.Id)
                {
                    await WriteLog(Log.Verbose($"ignoring reaction from user {reaction.UserId}"));
                    var _ = message.RemoveReactionAsync(reaction.Emoji.Name, reaction.User.Value);
                    return;
                }
                await message.RemoveReactionAsync(reaction.Emoji.Name, reaction.User.Value);
                await WriteLog(Log.Verbose($"handling reaction {reaction.Emoji.Name}"));
                switch (reaction.Emoji.Name)
                {
                    case FIRST:
                        if (page.CurrentPage == 1) break;
                        page.CurrentPage = 1;
                        await message.ModifyAsync(x => x.Content = page.ToString());
                        break;
                    case BACK:
                        if (page.CurrentPage == 1) break;
                        page.CurrentPage--;
                        await message.ModifyAsync(x => x.Content = page.ToString());
                        break;
                    case NEXT:
                        if (page.CurrentPage == page.Count) break;
                        page.CurrentPage++;
                        await message.ModifyAsync(x => x.Content = page.ToString());
                        break;
                    case END:
                        if (page.CurrentPage == page.Count) break;
                        page.CurrentPage = page.Count;
                        await message.ModifyAsync(x => x.Content = page.ToString());
                        break;
                    case JUMP:
                        break;
                    case STOP:
                        await message.DeleteAsync();
                        _messages.Remove(message.Id);
                        return;
                    case INFO:
                    default:
                        break;
                }
            }
        }
    }

    internal class PaginatedMessage
    {
        internal PaginatedMessage(IReadOnlyCollection<string> pages, IUser user, string language)
        {
            Pages = pages;
            Language = language;
            User = user;
            CurrentPage = 1;
        }

        internal IReadOnlyCollection<string> Pages { get; }
        internal IUser User { get; }
        internal string Language { get; }
        internal int CurrentPage { get; set; }
        internal int Count => Pages.Count;

        public override string ToString() => string.Concat($"```{Language}\n", Pages.ElementAtOrDefault(CurrentPage - 1), Suffix, "```");
        internal string Suffix => $"\n\nPage {CurrentPage}/{Count}";
    }
}
