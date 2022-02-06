using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace EconomyBot.Commands
{
    [RequireUserPermission(GuildPermission.ManageChannels, ErrorMessage = "You lack permissions to use this command", Group = "pingperm", NotAGuildErrorMessage = "This command needs to be used within a server!")]
    [RequireOwner(Group = "pingperm")]
    [Group("ping_svc")]
    public class NPCPingServiceModule : ModuleBase<SocketCommandContext>
    {
        private AndoraService _andoraService;
        private NPCPingService _service;

        public NPCPingServiceModule(AndoraService andora_service)
        {
            _andoraService = andora_service; //Cache the AndoraService passed from the context.
            _service = _andoraService.NPCPingService;
        }

        [Command("watch", RunMode = RunMode.Async), Alias("aw"), Summary("Add a channel to the watched channel list")]
        public async Task AddWatchedChannelCommandAsync(SocketChannel channel)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);

            if (channel is SocketTextChannel)
            {
                _service.AddWatchChannel(Context.Guild.Id, channel.Id);
                await ReplyAsync("Watched channel added!");
            }
        }

        [Command("rwatch", RunMode = RunMode.Async), Alias("rw"), Summary("Remove a channel from the watched channel list")]
        public async Task RemoveWatchedChannelCommandAsync(SocketChannel channel)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);

            if (channel is SocketTextChannel)
            {
                _service.RemoveWatchChannel(Context.Guild.Id, channel.Id);
                await ReplyAsync("Watched channel removed!");
            }
        }

        [Command("role", RunMode = RunMode.Async), Alias("r"), Summary("Set a role for pings to check for")]
        public async Task SetPingRoleCommandAsync(SocketRole role)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);
            
            _service.SetPingRole(Context.Guild.Id, role.Id);
            await ReplyAsync("Role has been set!");
        }

        [Command("report_channel", RunMode = RunMode.Async), Alias("rc"), Summary("Set the channel to report pings to")]
        public async Task SetReportChannelCommandAsync(SocketChannel channel)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);

            if (channel is SocketTextChannel)
            {
                _service.SetReportChannel(Context.Guild.Id, channel.Id);
                await ReplyAsync("Report channel has been set!");
            }
        }
    }
}
