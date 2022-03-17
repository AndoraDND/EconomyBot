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
                var utcTime = time;
                try
                {
                    utcTime = TimeZoneInfo.ConvertTimeToUtc(time, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
                }
                catch
                {
                    utcTime = TimeZoneInfo.ConvertTimeToUtc(time, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
                }

                var value = ((DateTimeOffset)utcTime).ToUnixTimeSeconds(); // (long)time.AddHours(6).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
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
