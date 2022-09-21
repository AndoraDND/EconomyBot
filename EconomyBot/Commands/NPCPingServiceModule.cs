using System;
using System.Collections.Generic;
using System.IO;
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
                if (_service.RemoveWatchChannel(Context.Guild.Id, channel.Id))
                {
                    await ReplyAsync("Watched channel removed!");
                }
                else
                {
                    await ReplyAsync("Error attempting to remove channel!");
                }
            }
        }
        [Command("rwatchid", RunMode = RunMode.Async), Alias("rwid"), Summary("Remove a channel from the watched channel list")]
        public async Task RemoveWatchedChannelByIDCommandAsync(ulong channelID)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);

            if (_service.RemoveWatchChannel(Context.Guild.Id, channelID))
            {
                await ReplyAsync("Watched channel removed!");
            }
            else
            {
                await ReplyAsync("Error attempting to remove channel!");
            }
        }

        [Command("add_role", RunMode = RunMode.Async), Summary("Set a role for pings to check for")]
        public async Task SetPingRoleCommandAsync(SocketRole role)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);
            
            _service.AddPingRole(Context.Guild.Id, role.Id);
            await ReplyAsync("Role has been set!");
        }

        [Command("remove_role", RunMode = RunMode.Async), Alias("remr"), Summary("Remove a role for pings to check")]
        public async Task RemovePingRoleCommandAsync(SocketRole role)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);

            _service.RemovePingRole(Context.Guild.Id, role.Id);
            await ReplyAsync("Role has been removed from tracking!");
        }

        [Command("setcolor", RunMode = RunMode.Async), Alias("color"), Summary("Set an embed color for a particular role")]
        public async Task SetPingRoleColorCommandAsync(SocketRole role, [Remainder] string text)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);

            if(_service.SetColor(Context.Guild.Id, role.Id, text))
            {
                await ReplyAsync("Color has been set!");
            }
            else
            {
                await ReplyAsync("Error setting color for role! Check syntax and try again.");
            }
        }

        [Command("report_channel", RunMode = RunMode.Async), Alias("rc"), Summary("Set the channel to report pings to")]
        public async Task SetReportChannelCommandAsync(SocketRole role, SocketChannel channel)
        {
            await Context.Channel.DeleteMessageAsync(Context.Message);

            if (channel is SocketTextChannel && role is SocketRole)
            {
                _service.SetReportChannel(Context.Guild.Id, role.Id, channel.Id);
                await ReplyAsync("Report channel has been set for the role!");
            }
        }

        [Command("dump_guild_data", RunMode = RunMode.Async), Summary("Check which channels are managed by this bot.")]
        public async Task SetReportChannelCommandAsync()
        {
            #region Permission Checking

            //Check for valid permissions. We need to do this manually since we have multiple points of contention
            //People like to change role names, and we have a *lot* of roles that could be changed at any point.
            //Just handle it manually for now.
            if (Context.User is SocketGuildUser user)
            {
                bool roleFound = false;
                foreach (var role in user.Roles)
                {
                    if (_andoraService.ElevatedStatusRoles.Contains(role.Id))
                    {
                        //We're good.
                        roleFound = true;
                        break;
                    }
                }

                if (!roleFound)
                {
                    return;
                }
            }
            else
            {
                await ReplyAsync("This command is only supported within Servers!");
                return;
            }

            #endregion

            var guild = Context.Guild;
            var guildData = _service.GetGuildData(guild.Id);

            var output_text = $"====== NPC Ping Service Data in {guild.Name} < {guild.Id} > ======\n\n";

            output_text += "  == Tracked Roles: ==\n\n";
            foreach(var role in guildData.TrackedRoles)
            {
                output_text += $"    Role_Name: {guild.GetRole(role).Name}\n";
                output_text += $"    Role_ID: {role}\n";
                if (guildData.RoleEmbedColor.ContainsKey(role))
                {
                    output_text += $"    Color: {guildData.RoleEmbedColor[role].ToString()}\n";
                }
                output_text += "\n";
            }

            output_text += "  == Report Channels: ==\n\n";
            foreach (var kvp in guildData.ReportChannels)
            {
                output_text += $"    Channel: {guild.GetChannel(kvp.Value).Name} [{kvp.Value}]\n";
                output_text += $"    Role: {guild.GetRole(kvp.Key).Name} [{kvp.Key}]\n";
                output_text += "\n";
            }

            output_text += "  == Watched Channels: ==\n\n";
            foreach (var channel in guildData.WatchedChannels)
            {
                output_text += $"    Channel: {guild.GetChannel(channel).Name} [{channel}]\n";
                output_text += "\n";
            }


            //Create a new dump file and load it with data from this database dump

            DateTime dt = default(DateTime);
            try
            {
                dt = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
            }
            catch
            {
                dt = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
            }

            var dumpFilePath = Directory.GetCurrentDirectory() + $"/Data/{Context.User.Id}_DumpLog_" + dt.ToShortTimeString().Replace(':', '-').Replace('_', '-') + ".txt";
            File.WriteAllText(dumpFilePath, output_text);

            //Send the file to the user requesting it
            await Discord.UserExtensions.SendFileAsync(Context.User, dumpFilePath, "Here is a dump of the currently watched channels.");

            //Delete the new dump file
            File.Delete(dumpFilePath);
        }
    }
}
