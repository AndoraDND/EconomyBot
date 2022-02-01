using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace EconomyBot.Commands
{
    [Group("econ_debug")]
    public class EconDebugModule : ModuleBase<SocketCommandContext>
    {
        private AndoraService _andoraService;
        private ReactionReplyService _pollService;

        public EconDebugModule(AndoraService andora_service, ReactionReplyService poll_service)
        {
            _andoraService = andora_service; //Cache the AndoraService passed from the context.
            _pollService = poll_service; //Cache the Discord Polling service.

            //_embed = new EmbedBuilder().WithColor(new Color(0xf1, 0xc5, 0x5f));
        }

        [Command("dump_character_db", RunMode = RunMode.Async), Summary("Dump the database of characters to a log file, send that file")]
        public async Task DumpCharacterDatabaseCommandAsync()
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

            //Create a new dump file and load it with data from this database dump
            var dumpFilePath = Directory.GetCurrentDirectory() + "/Data/DumpLog_" + DateTime.Now.ToShortTimeString().Replace(':', '-').Replace('_','-') + ".txt";
            File.WriteAllText(dumpFilePath, _andoraService.CharacterDB.Dump());

            //Send the file to the user requesting it
            await Discord.UserExtensions.SendFileAsync(Context.User, dumpFilePath, "Here is a dump of the currently cached character database.");

            //Delete the new dump file
            File.Delete(dumpFilePath);
        }

        [Command("dump_item_db", RunMode = RunMode.Async), Summary("Dump the database of items to a log file, send that file")]
        public async Task DumpItemDatabaseCommandAsync()
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

            //Create a new dump file and load it with data from this database dump
            var dumpFilePath = Directory.GetCurrentDirectory() + "/Data/DumpLog_" + DateTime.Now.ToShortTimeString().Replace(':', '-').Replace('_', '-') + ".txt";
            File.WriteAllText(dumpFilePath, _andoraService.PriceDB.Dump());

            //Send the file to the user requesting it
            await Discord.UserExtensions.SendFileAsync(Context.User, dumpFilePath, "Here is a dump of the currently cached item database.");
            
            //Delete the new dump file
            File.Delete(dumpFilePath);
        }

        [Command("dump_message_db", RunMode = RunMode.Async), Summary("Dump the current message queue")]
        public async Task DumpMessageDatabaseCommandAsync()
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

            //Create a new dump file and load it with data from this database dump
            var dumpFilePath = Directory.GetCurrentDirectory() + "/Data/DumpLog_" + DateTime.Now.ToShortTimeString().Replace(':', '-').Replace('_', '-') + ".txt";
            File.WriteAllText(dumpFilePath, Program._messageHandler.Dump());

            //Send the file to the user requesting it
            await Discord.UserExtensions.SendFileAsync(Context.User, dumpFilePath, "Here is a dump of the currently cached message database.");

            //Delete the new dump file
            File.Delete(dumpFilePath);
        }
    }
}
