using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace EconomyBot.Commands
{
    [Group("dtd")] //!dtd
    public class DTDModule : ModuleBase<SocketCommandContext>
    {
        private AndoraService _andoraService;
        private ReactionReplyService _pollService;

        public DTDModule(AndoraService andora_service, ReactionReplyService poll_service)
        {
            _andoraService = andora_service; //Cache the AndoraService passed from the context.
            _pollService = poll_service; //Cache the Discord Polling service.
        }

        [Command("job", RunMode = RunMode.Async), Alias("j"), Summary("Commit downtime days towards earning gold via a day job.")]
        public async Task HandleJobCommandAsync([Remainder] string input)
        {
            await ReplyAsync("This command is not yet implemented. Please try again later.");
        }

        [Command("research", RunMode = RunMode.Async), Alias("r"), Summary("Commit downtime days towards research goals.")]
        public async Task HandleResearchCommandAsync([Remainder] string input)
        {
            await ReplyAsync("This command is not yet implemented. Please try again later.");
        }

        [Command("shopping", RunMode = RunMode.Async), Alias("s"), Summary("Commit a day of downtime towards shopping from the general store.")]
        public async Task HandleShoppingCommandAsync([Remainder] string input)
        {
            await ReplyAsync("This command is not yet implemented. Please try again later.");
        }

        [Command("training", RunMode = RunMode.Async), Alias("t"), Summary("Commit downtime days towards training courses.")]
        public async Task HandleTrainingCommandAsync([Remainder] string input)
        {
            await ReplyAsync("This command is not yet implemented. Please try again later.");
        }

        [Command("foraging", RunMode = RunMode.Async), Alias("f"), Summary("Commit downtime days towards foraging for reagents")]
        public async Task HandleForageCommandAsync([Remainder] string input)
        {
            await ReplyAsync("This command is not yet implemented. Please try again later.");
        }

        [Command("crafting", RunMode = RunMode.Async), Alias("c"), Summary("Commit downtime days towards crafting potions/poisons or mundane items.")]
        public async Task HandleCraftingCommandAsync([Remainder] string input)
        {
            await ReplyAsync("This command is not yet implemented. Please try again later.");
        }

        [Command("travel", RunMode = RunMode.Async), Summary("Commit downtime days towards traveling to other locations.")]
        public async Task HandleTravelCommandAsync([Remainder] string input)
        {
            await ReplyAsync("This command is not yet implemented. Please try again later.");
        }

        [Group("verify"), Alias("v")] //!dtd verify
        public class DTDVerifyModule : ModuleBase<SocketCommandContext>
        {
            private AndoraService _andoraService;
            private ReactionReplyService _pollService;

            public DTDVerifyModule(AndoraService andora_service, ReactionReplyService poll_service)
            {
                _andoraService = andora_service;
                _pollService = poll_service;
            }

            /// <summary>
            /// Verify whether a day job's results are successfully inputted by a player. 
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            [Command("job", RunMode = RunMode.Async), Alias("j"), Summary("Verify the earned gold for a character's attempted day job.")]
            public async Task CheckJobCommandAsync([Remainder] string input)//!dtd verify job
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

                var splitInput = input.Split(",");
                var characterName = splitInput[0];
                var toolType = splitInput[1].TrimStart().TrimEnd();
                int.TryParse(splitInput[2], out var daysSpent);

                //Check if character input is valid
                //TODO: Add character management to bot
                var characterSheetURL = "1SasaqMK9P2OuNrjvsrZHE0g_1eGpB6YqijpDebqhJts";

                //Check if tool input is valid.
                if (!_andoraService.DTDToolValues.ContainsKey(toolType.ToLower().Replace("'", "").Replace("’", "")))
                {
                    await ReplyAsync($"Error: No such tool in dictionary - \"{toolType}\"");
                    return;
                }
                var goldEarned = _andoraService.DTDToolValues[toolType.ToLower().Replace("'", "").Replace("’", "")];

                //Check for valid day counts
                if (daysSpent > 7 || daysSpent < 1)
                {
                    await ReplyAsync($"Error: Invalid amount of days!");
                }

                var toolProficiency = _andoraService.AvraeParser.CheckToolProficiency(characterSheetURL, toolType.ToLower().Replace("'", "").Replace("’", ""));
                if (toolProficiency)
                {
                    var goldSum = daysSpent * goldEarned;
                    var startingGold = _andoraService.PriceDB.ParseGold(_andoraService.AvraeParser.GetCurrency(characterSheetURL));

                    await ReplyAsync("**DTD Succeeded**. Verify the following details:\n" +
                        $"**Total Gold** -> {daysSpent} * {_andoraService.PriceDB.FormatGold(goldEarned)} = {_andoraService.PriceDB.FormatGold(goldSum)}\n" +
                        $"**Starting Gold** -> {_andoraService.PriceDB.FormatGold(startingGold)}\n" +
                        $"**Ending Gold** -> {_andoraService.PriceDB.FormatGold(startingGold + goldSum)}");
                }
                else
                {
                    await ReplyAsync("**DTD Failed**. No relevant proficiency!");
                }
            }
        }
    }
}
