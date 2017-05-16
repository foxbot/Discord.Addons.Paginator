using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Addons.Paginator
{
    public class PaginationService
    {
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

            await message.AddReactionAsync(paginated.Options.EmoteFirst);
            await message.AddReactionAsync(paginated.Options.EmoteBack);
            await message.AddReactionAsync(paginated.Options.EmoteNext);
            await message.AddReactionAsync(paginated.Options.EmoteLast);
            await message.AddReactionAsync(paginated.Options.EmoteStop);

            _messages.Add(message.Id, paginated);
            await WriteLog(Log.Debug("Listening to message with id {id}"));

            if (paginated.Options.Timeout != TimeSpan.Zero)
                paginated.TimeoutTimer = new Timer(RemoveMessageFromList, Tuple.Create(message, paginated.Options.TimeoutAction), (int)paginated.Options.Timeout.TotalMilliseconds, Timeout.Infinite);

            return message;
        }

        private async void RemoveMessageFromList(object state)
        {
            var tuple = (Tuple<IUserMessage, PageStopAction>)state;
            if (tuple.Item2 == PageStopAction.DeleteMessage)
                await tuple.Item1.DeleteAsync();
            else if (tuple.Item2 == PageStopAction.StopListeningAndDeleteReactions)
                await tuple.Item1.RemoveAllReactionsAsync();
            _messages.Remove(tuple.Item1.Id);
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
                if (reaction.Emote.Name == page.Options.EmoteFirst.Name)
                {
                    if (page.CurrentPage != 1)
                    {
                        page.CurrentPage = 1;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                    }
                }
                else if (reaction.Emote.Name == page.Options.EmoteBack.Name)
                {
                    if (page.CurrentPage != 1)
                    {
                        page.CurrentPage--;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                    }
                }
                else if (reaction.Emote.Name == page.Options.EmoteNext.Name)
                {
                    if (page.CurrentPage != page.Count)
                    {
                        page.CurrentPage++;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                    }
                }
                else if (reaction.Emote.Name == page.Options.EmoteLast.Name)
                {
                    if (page.CurrentPage != page.Count)
                    {
                        page.CurrentPage = page.Count;
                        await message.ModifyAsync(x => x.Embed = page.GetEmbed());
                    }
                }
                else if (reaction.Emote.Name == page.Options.EmoteStop.Name)
                {
                    if (page.Options.EmoteStopAction == PageStopAction.DeleteMessage)
                        await message.DeleteAsync();
                    else if (page.Options.EmoteStopAction == PageStopAction.StopListeningAndDeleteReactions)
                        await message.RemoveAllReactionsAsync();
                    _messages.Remove(message.Id);
                    if (page.TimeoutTimer != null)
                        page.TimeoutTimer.Dispose();
                }
            }
        }
    }

    public class PaginatedMessage
    {
        public PaginatedMessage(IReadOnlyCollection<string> pages, string title = "", Color? embedColor = null, IUser user = null, PaginatedMessageOptions options = null)
        {
            List<Embed> _pages = new List<Embed>();
            int i = 1;
            foreach (string page in pages)
            {
                EmbedBuilder embed = new EmbedBuilder()
                .WithColor(embedColor ?? Color.Default)
                .WithTitle(title)
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
            Options = options ?? new PaginatedMessageOptions();
            CurrentPage = 1;
        }
        public PaginatedMessage(IReadOnlyCollection<PageBuilder> pages, string title = "", Color? embedColor = null, IUser user = null, PaginatedMessageOptions options = null)
        {
            List<Embed> _pages = new List<Embed>();
            int i = 1;
            foreach (PageBuilder page in pages)
            {
                EmbedBuilder embed = new EmbedBuilder()
                .WithColor(embedColor ?? Color.Default)
                .WithTitle(title)
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
            Options = options ?? new PaginatedMessageOptions();
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
        internal PaginatedMessageOptions Options { get; }
        internal int CurrentPage { get; set; }
        internal int Count => Pages.Count;
        internal Timer TimeoutTimer { get; set; }
    }

    public class PaginatedMessageOptions
    {
        const string FIRST = "⏮";
        const string BACK = "◀";
        const string NEXT = "▶";
        const string LAST = "⏭";
        const string STOP = "⏹";

        public IEmote EmoteFirst { get; set; } = new Emoji(FIRST);
        public IEmote EmoteBack { get; set; } = new Emoji(BACK);
        public IEmote EmoteNext { get; set; } = new Emoji(NEXT);
        public IEmote EmoteLast { get; set; } = new Emoji(LAST);
        public IEmote EmoteStop { get; set; } = new Emoji(STOP);
        public PageStopAction EmoteStopAction { get; set; } = PageStopAction.DeleteMessage;
        public TimeSpan Timeout { get; set; } = TimeSpan.Zero;
        public PageStopAction TimeoutAction { get; set; } = PageStopAction.DeleteMessage;
    }

    public enum PageStopAction
    {
        DeleteMessage,
        StopListening,
        StopListeningAndDeleteReactions
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
