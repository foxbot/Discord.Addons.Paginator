using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Addons.Paginator
{
    public class PaginationService
    {
        const string FIRST = "⏮";
        const string BACK = "◀";
        const string NEXT = "▶";
        const string END = "⏭";
        const string STOP = "⏹";

        internal readonly Log Log = new Log("Paginator");
        internal readonly Func<LogMessage, Task> WriteLog;

        private readonly Dictionary<ulong, PaginatedMessage> _messages;
        private readonly DiscordSocketClient _client;

        public PaginationService(DiscordSocketClient client, Func<LogMessage, Task> logger = null)
        {
            WriteLog = logger ?? (m => Task.CompletedTask);
            WriteLog(Log.Debug("Creating new service"));
            _messages = new Dictionary<ulong, PaginatedMessage>(); 
            _client = client;
            _client.ReactionAdded += OnReactionAdded;
            WriteLog(Log.Debug("client.ReactionAdded hooked"));
        }

        /// <summary>
        /// Sends a paginated message (with reaction buttons)
        /// </summary>
        /// <param name="channel">The channel this message should be sent to</param>
        /// <param name="paginated">A <see cref="PaginatedMessage">PaginatedMessage</see> containing the pages.</param>
        /// <exception cref="Net.HttpException">Thrown if the bot user cannot send a message or add reactions.</exception>
        /// <returns>The paginated message.</returns>
        public async Task<IUserMessage> SendPaginatedMessageAsync(IMessageChannel channel, PaginatedMessage paginated)
        {
            await WriteLog(Log.Info($"Sending message to {channel}"));

            var message = await channel.SendMessageAsync("", embed: paginated.GetEmbed());

            await message.AddReactionAsync(new Emoji(FIRST));
            await message.AddReactionAsync(new Emoji(BACK));
            await message.AddReactionAsync(new Emoji(NEXT));
            await message.AddReactionAsync(new Emoji(END));
            await message.AddReactionAsync(new Emoji(STOP));

            _messages.Add(message.Id, paginated);
            await WriteLog(Log.Debug("Listening to message with id {id}"));

            return message;
        }

        internal async Task OnReactionAdded(Cacheable<IUserMessage, ulong> messageParam, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await messageParam.GetOrDownloadAsync();
            if (message == null)
            {
                await WriteLog(Log.Verbose($"Dumped message (not in cache) with id {reaction.MessageId}"));
                return;
            }
            if (!reaction.User.IsSpecified)
            {
                await WriteLog(Log.Verbose($"Dumped message (invalid user) with id {message.Id}"));
                return;
            }
            if (_messages.TryGetValue(message.Id, out PaginatedMessage page))
            {
                if (reaction.UserId == _client.CurrentUser.Id) return;
                if (page.User != null && reaction.UserId != page.User.Id)
                {
                    await WriteLog(Log.Verbose($"ignoring reaction from user {reaction.UserId}"));
                    var _ = message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    return;
                }
                await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await WriteLog(Log.Verbose($"handling reaction {reaction.Emote}"));
                switch (reaction.Emote.Name)
                {
                    case FIRST:
                        if (page.CurrentPage == 1) break;
                        page.CurrentPage = 1;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                        break;
                    case BACK:
                        if (page.CurrentPage == 1) break;
                        page.CurrentPage--;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                        break;
                    case NEXT:
                        if (page.CurrentPage == page.Count) break;
                        page.CurrentPage++;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                        break;
                    case END:
                        if (page.CurrentPage == page.Count) break;
                        page.CurrentPage = page.Count;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                        break;
                    case STOP:
                        await message.DeleteAsync();
                        _messages.Remove(message.Id);
                        return;
                    default:
                        break;
                }
            }
        }
    }

    public class PaginatedMessage
    {
        public PaginatedMessage(IReadOnlyCollection<string> pages, string title = "", Color? embedColor = null, IUser user = null)
        {
            List<Embed> _pages = new List<Embed>();
            int i = 1;
            foreach (string page in pages)
            {
                EmbedBuilder embed = new EmbedBuilder()
                .WithColor(EmbedColor)
                .WithTitle(Title)
                .WithDescription(page ?? "")
                .WithFooter(footer =>
                {
                    footer.Text = $"Page {i++}/{pages.Count}";
                });
                _pages.Add(embed.Build());
            }
            Pages = _pages;
            Title = title;
            EmbedColor = embedColor ?? Color.Default;
            User = user;
            CurrentPage = 1;
        }
        public PaginatedMessage(IReadOnlyCollection<PageBuilder> pages, string title = "", Color? embedColor = null, IUser user = null)
        {
            List<Embed> _pages = new List<Embed>();
            int i = 1;
            foreach (PageBuilder page in pages)
            {
                EmbedBuilder embed = new EmbedBuilder()
                .WithColor(EmbedColor)
                .WithTitle(Title)
                .WithDescription(page?.Description ?? "")
                .WithImageUrl(page?.ImageUrl ?? "")
                .WithThumbnailUrl(page?.ThumbnailUrl ?? "")
                .WithFooter(footer =>
                {
                    footer.Text = $"Page {i++}/{pages.Count}";
                });
                if (page.Fields != null)
                    foreach (EmbedFieldBuilder field in page.Fields)
                        embed.AddField(field);
                _pages.Add(embed.Build());
            }
            Pages = _pages;
            Title = title;
            EmbedColor = embedColor ?? Color.Default;
            User = user;
            CurrentPage = 1;
        }

        internal Embed GetEmbed()
        {
            return Pages.ElementAtOrDefault(CurrentPage - 1);
        }

        internal string Title { get; }
        internal Color EmbedColor { get; }
        internal IReadOnlyCollection<Embed> Pages { get; }
        internal IUser User { get; }
        internal int CurrentPage { get; set; }
        internal int Count => Pages.Count;
    }

    public class PageBuilder
    {
        public string Description { get; set; }
        public IReadOnlyCollection<EmbedFieldBuilder> Fields { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }

        public PageBuilder WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public PageBuilder WithFields(IReadOnlyCollection<EmbedFieldBuilder> fields)
        {
            Fields = fields;
            return this;
        }

        public PageBuilder WithImageUrl(string imageUrl)
        {
            ImageUrl = imageUrl;
            return this;
        }

        public PageBuilder WithThumbnailUrl(string thumbnailUrl)
        {
            ThumbnailUrl = thumbnailUrl;
            return this;
        }
    }
}
