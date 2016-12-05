using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Paginator;
using Discord.Commands;
using Discord.WebSocket;

namespace Example.Modules.Test
{
    public class TestModule : ModuleBase
    {
        private readonly PaginationService paginator;

        public TestModule(PaginationService paginationService)
        {
            paginator = paginationService;
        }

        [Command("paginate")]
        [RequireBotPermission(ChannelPermission.ManageMessages | ChannelPermission.AddReactions)]
        public async Task Paginate()
        {
            var pages = new List<string>
            {
                "page one - aaaaa\n1\n2\n3\n4\aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaah\n\n",
                "1 2 3 4 5\n501\n12312312\n126adsafoadsf\naaa",
                "1.\n2.\n3.\n4.\n5.\n6\n.7\n.",
                "please do not do this",
                "abal! !!",
            };

            await paginator.SendPaginatedMessage(Context.Channel, pages, Context.User, "md");
        }
    }
}
