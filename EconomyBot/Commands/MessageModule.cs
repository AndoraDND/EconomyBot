using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace EconomyBot.Commands
{
    [Group("message")]
    public class MessageModule : ModuleBase<SocketCommandContext>
    {
        private AndoraService _andoraService;

        public MessageModule(AndoraService andoraService)
        {
            _andoraService = andoraService;
        }

        [Command("add", RunMode = RunMode.Async), Alias("a"), Summary("Add a message to the queue")]
        public async Task AddMessageCommandAsync([Remainder] string input)
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
                    await ReplyAsync("You do not have role permission to use this command.");
                    return;
                }
            }
            else
            {
                await ReplyAsync("This command is only supported within Servers!");
                return;
            }

            #endregion

            var inputData = input.Split(',');

            if(inputData.Length < 3)
            {
                await ReplyAsync("Incorrect number of command parameters. Please try again.");
                return;
            }

            var msg = inputData[0];
            ulong guildID = Context.Guild.Id;
            ulong channelID = Context.Channel.Id;
            DateTime startTime = default(DateTime);
            TimeSpan interval = default(TimeSpan);

            try
            {
                startTime = DateTime.Parse(inputData[1]);
            }
            catch(Exception e)
            {
                await ReplyAsync("Failed to parse start time. Please check syntax and try again.");
                return;
            }

            try
            {
                interval = TimeSpan.Parse(inputData[2]);
            }
            catch(Exception e)
            {
                await ReplyAsync("Failed to parse time interval. Please check syntax and try again.");
                return;
            }
                
            Program._messageHandler.AddMessage(msg, guildID, channelID, startTime, interval);

            await ReplyAsync("Successfully added the message to the database!");
        }
        [Command("remove", RunMode = RunMode.Async), Alias("r"), Summary("Remove a message from the queue")]
        public async Task RemoveMessageCommandAsync([Remainder] string input)
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
                    await ReplyAsync("You do not have role permission to use this command.");
                    return;
                }
            }
            else
            {
                await ReplyAsync("This command is only supported within Servers!");
                return;
            }

            #endregion

            if (!Program._messageHandler.RemoveMessage(input.TrimStart().TrimEnd()))
            {
                await ReplyAsync("Failed to remove message from the database. Check that the GUID is correct");
            }
            else
            {
                await ReplyAsync("Successfully removed message from the database!");
            }
        }
    }
}
