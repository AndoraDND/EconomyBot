using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace EconomyBot.Commands
{
    public class UnixTimestampModule : ModuleBase<SocketCommandContext>
    {
        [Command("getunix", RunMode = RunMode.Async), Alias("getut"), Summary("Get a unix timestamp from a specified time")]
        public async Task GetUnixTimestampCommand([Remainder] string input)
        {
            if (DateTime.TryParse(input, out var time))
            {
                var value = (long)time.AddHours(6).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                await ReplyAsync($"{Context.User.Mention} Timestamp for the supplied value is : ``{value}``");
            }
            else
            {
                await ReplyAsync("Failed to get a timestamp from the specified text. Please check for any mistakes and try again.");
            }

            await Context.Channel.DeleteMessageAsync(Context.Message);
        }
    }
}
