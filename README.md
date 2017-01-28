[![Build Status](https://travis-ci.org/foxbot/Discord.Addons.Paginator.svg?branch=master)](https://travis-ci.org/foxbot/Discord.Addons.Paginator)

# Discord.Addons.Paginator
An addon for [Discord.Net 1.0](https://github.com/RogueException/Discord.Net). 

Allows you to send a series of pages in a single message, using reaction buttons to switch pages.

Credit for the original concept of paginating messages with reactions to [Rapptz](https://github.com/Rapptz) using [discord.py](https://gist.github.com/Rapptz/666785fd0d8559c18f2ced46fa862d77).

![GIF](https://6.lithi.io/24bPF.gif)

### Logging

Paginator uses the standard addon logging pattern. To enable logging, pass a `Func<LogMessage, Task>` into the constructor of `PaginatorService`, or `DiscordSocketClient.UsePaginator`.