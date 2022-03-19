using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace EconomyBot.Commands
{
    [Group("postreport")]
    public class PostReportModule : ModuleBase<SocketCommandContext>
    {
        private AndoraService _andoraService;

        private static EmbedBuilder _embed;

        public PostReportModule(AndoraService andora_service)
        {
            _andoraService = andora_service; //Cache the AndoraService passed from the context.

            _embed = new EmbedBuilder().WithColor(new Color(0x61, 0xb9, 0x36));
        }

        [Command("run", RunMode = RunMode.Async), Alias("r"), Summary("Run report for Post Reports and process experience")]
        public async Task HandleReportPollingCommandAsync()
        {
            await _andoraService.PostReportParser.PollPlayerActivity(Context);
        }
    }
}
