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

        private readonly Dictionary<ulong, PaginatedMessage> _messages;
        private readonly DiscordSocketClient _client;

        public PaginationService(DiscordSocketClient client)
        {
            _messages = new Dictionary<ulong, PaginatedMessage>(); 
            _client = client;
            _client.ReactionAdded += OnReactionAdded;
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

            return message;
        }

        internal async Task OnReactionAdded(ulong id, Optional<SocketUserMessage> messageParam, SocketReaction reaction)
        {
            PaginatedMessage page;
            var message = messageParam.GetValueOrDefault();
            if (message == null) return;
            if (!reaction.User.IsSpecified) return;
            if (_messages.TryGetValue(message.Id, out page))
            {
                if (reaction.UserId == _client.CurrentUser.Id) return;
                if (page.User != null && reaction.UserId != page.User.Id)
                {
                    var _ = message.RemoveReactionAsync(reaction.Emoji.Name, reaction.User.Value);
                    return;
                }
                await message.RemoveReactionAsync(reaction.Emoji.Name, reaction.User.Value);
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
