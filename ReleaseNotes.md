### New in 2.2.0 (Released 2017/05/12)
* Use Discord.Net RC3
* Add extension methods to use IServiceCollection
* Deprecated `DiscordSocketClient#UsePaginator`

### New in 2.1.1 (Released 2017/03/07)
* Sample bot should now conform to the latest version of Discord.Net RC

### New in 2.1.0 (Released 2017/03/07)
* Updated to latest version of Discord.Net RC (cacheable update)

### New in 2.0.0 (Released 2017/01/03)
* Paginated messages now take the form of embeds
* `SendPaginatedMessage` has been renamed to `SendPaginatedMessageAsync`
* `SendPaginatedMessageAsync` now takes an entire `PaginatedMessage` as an argument, instead of building one for you