using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace EconomyBot.Commands
{
    [Group("role")]
    public class ElevatedRoleModule : ModuleBase<SocketCommandContext>
    {
        private AndoraService _andoraService;
        
        public ElevatedRoleModule(AndoraService service)
        {
            _andoraService = service;
        }

        [Command("add", RunMode = RunMode.Async), Alias("a"), Summary("Add a role with permissions")]
        public async Task AddElevatedRoleCommandAsync(SocketRole role)
        {
            #region Permission Checking
            if (Context.User is SocketGuildUser)
            {
                var user = Context.User as SocketGuildUser;
                if (Context.Channel is SocketGuildChannel)
                {
                    if ((user.GetPermissions(Context.Channel as SocketGuildChannel)).ManageRoles == false)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            #endregion

            if (!_andoraService.ElevatedStatusRoles.Contains(role.Id))
            {
                _andoraService.ElevatedStatusRoles.Add(role.Id);

                SaveElevatedRoles();
                await ReplyAsync("Added role to list of elevated roles!");
                return;
            }

            await ReplyAsync("Failed to add role to list of elevated roles.");
        }

        [Command("remove", RunMode = RunMode.Async), Alias("r"), Summary("Remove a role from permissions")]
        public async Task RemoveElevatedRoleCommandAsync(SocketRole role)
        {
            #region Permission Checking
            if (Context.User is SocketGuildUser)
            {
                var user = Context.User as SocketGuildUser;
                if (Context.Channel is SocketGuildChannel)
                {
                    if ((user.GetPermissions(Context.Channel as SocketGuildChannel)).ManageRoles == false)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            #endregion

            if (_andoraService.ElevatedStatusRoles.Contains(role.Id))
            {
                _andoraService.ElevatedStatusRoles.Remove(role.Id);

                SaveElevatedRoles();
                await ReplyAsync("Removed role from list of elevated roles!");
                return;
            }

            await ReplyAsync("Failed to remove role from list of elevated roles.");
        }

        private void SaveElevatedRoles()
        {
            var fileData = "ElevatedRoles,";

            foreach(var role in _andoraService.ElevatedStatusRoles)
            {
                fileData += $" {role},";
            }

            DataStorage.FileReader.WriteCSV("ElevatedRoleData", fileData);
        }
    }
}
