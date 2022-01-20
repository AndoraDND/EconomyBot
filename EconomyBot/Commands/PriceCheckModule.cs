using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EconomyBot.Commands.Preconditions;

namespace EconomyBot.Commands
{
    /// <summary>
    /// An example module for defining a group of commands. More complicated than the ping commands.
    /// For reference, all commands need to be public and derive from the ModuleBase class, 
    /// using a generic type of CommandContext or SocketCommandContext. The latter is more common.
    /// </summary>
    [Group("price")]
    public class PriceCheckModule : ModuleBase<SocketCommandContext>
    {
        //In order to add specific services via Dependency Injection, we cache the service within this module, assigning it in the constructor.
        private readonly AndoraService _andoraService;
        private readonly ReactionReplyService _pollService;

        public PriceCheckModule(AndoraService andora_service, ReactionReplyService poll_service)
        {
            _andoraService = andora_service; //Cache the AndoraService passed from the context.
            _pollService = poll_service; //Cache the Discord Polling service.
        }

        [Command(RunMode = RunMode.Async), Priority(0), Summary("Search for a specific price of an item by name")]
        public async Task CheckPrice([Remainder] string input)
        {
            bool elevatedPermissions = false;
            #region Permission Checking

            //Check for valid permissions. We need to do this manually since we have multiple points of contention
            //People like to change role names, and we have a *lot* of roles that could be changed at any point.
            //Just handle it manually for now.
            if (Context.User is SocketGuildUser user)
            {
                foreach (var role in user.Roles)
                {
                    if (_andoraService.ElevatedStatusRoles.Contains(role.Id))
                    {
                        //We're good.
                        elevatedPermissions = true;
                        break;
                    }
                }
            }

            #endregion

            //Find the item within the internal database
            var item = await FindItemInDatabase(input, elevatedPermissions);

            if (item != null)
            {
                //Build an embed with the item
                var eb = new EmbedBuilder();
                eb.WithTitle(FormatToTitleCase(item.Item1));
                eb.WithAuthor($"Category: {item.Item2}");
                eb.WithColor(new Color(0xf1, 0xc5, 0x5f));
                eb.AddField(new EmbedFieldBuilder().WithName("Average Price").WithValue(_andoraService.PriceDB.FormatGold(item.Item3)).WithIsInline(false));
                eb.AddField(new EmbedFieldBuilder().WithName("Lowest Price").WithValue(_andoraService.PriceDB.FormatGold(item.Item4)).WithIsInline(false));
                eb.AddField(new EmbedFieldBuilder().WithName("Highest Price").WithValue(_andoraService.PriceDB.FormatGold(item.Item5)).WithIsInline(false));

                //Clean up the item
                item = null;

                //Post in the channel
                var message = await ReplyAsync($"{Context.Message.Author.Mention}", false, eb.Build());
            }
        }

        [Command("add", RunMode = RunMode.Async), Priority(1), Summary("Add an item to the database")]
        public async Task AddItemCommandAsync([Remainder] string input )
        {
            #region Permission Checking

            //Check for valid permissions. We need to do this manually since we have multiple points of contention
            //People like to change role names, and we have a *lot* of roles that could be changed at any point.
            //Just handle it manually for now.
            if (Context.User is SocketGuildUser user)
            {
                bool roleFound = false;
                foreach(var role in user.Roles)
                {
                    if(_andoraService.ElevatedStatusRoles.Contains(role.Id))
                    {
                        //We're good.
                        roleFound = true;
                        break;
                    }
                }

                if(!roleFound)
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

            //Core of Command
            if (input.Contains("\""))
            {
                int firstIndex = input.IndexOf("\"");
                int lastIndex = input.LastIndexOf("\"");

                string itemNameInput = input.Substring(firstIndex+1, (lastIndex - firstIndex) - 1);
                var args = new string[2];
                args[0] = itemNameInput;

                if(input.Length <= lastIndex+1)
                {
                    await ReplyAsync("Incorrect syntax on item add! Use the following syntax ``!price add \"item name\" Xg``");
                    return;
                }

                args[1] = input.Substring(lastIndex + 1);
                if(!_andoraService.PriceDB.CanParseGold(args[1]))
                {
                    await ReplyAsync("Failed to parse item price. Make sure to avoid spaces and separate values by commas if needed (Example: 5g, 3s, 2c / 2g, 8c");
                }

                if(!_andoraService.PriceDB.AddItem(args[0], "General", args[1]))
                {
                    await ReplyAsync("Failed to add item to the database. Item already exists!");
                    return;
                }

                await ReplyAsync("Item successfully added to database!");
            }
            else
            {
                //Not a valid item addition
                await ReplyAsync("Incorrect syntax on item add! Use the following syntax ``!price add \"item name\" Xg``");
                return;
            }
        }

        [Command("set", RunMode = RunMode.Async), Priority(1), Summary("Set the price of an item within the database")]
        public async Task SetItemPriceCommandAsync([Remainder] string input)
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

            //Core of Command
            if (input.Contains("\""))
            {
                int firstIndex = input.IndexOf("\"");
                int lastIndex = input.LastIndexOf("\"");

                string itemNameInput = input.Substring(firstIndex + 1, (lastIndex - firstIndex) - 1);
                var args = new string[2];
                args[0] = itemNameInput;

                if (input.Length <= lastIndex + 1)
                {
                    await ReplyAsync("Incorrect syntax on item set! Use the following syntax ``!price set \"item name\" Xg``");
                    return;
                }

                args[1] = input.Substring(lastIndex + 1);
                if (!_andoraService.PriceDB.CanParseGold(args[1]))
                {
                    await ReplyAsync("Failed to parse item price. Make sure to avoid spaces and separate values by commas if needed (Example: 5g, 3s, 2c / 2g, 8c");
                    return;
                }

                var itemName = await _andoraService.PriceDB.FindItem(args[0], _pollService, Context, true);
                if(itemName != null)
                {
                    _andoraService.PriceDB.SetPrice(itemName, _andoraService.PriceDB.ParseGold(args[1]));
                }
                else
                {
                    await ReplyAsync("Failed to set item price in the database. No existing item!");
                    return;
                }

                await ReplyAsync("Item successfully added to database!");
            }
            else
            {
                //Not a valid item addition
                await ReplyAsync("Incorrect syntax on item set! Use the following syntax ``!price set \"item name\" Xg``");
                return;
            }
        }

        [Command("setcategory", RunMode = RunMode.Async), Priority(1), Summary("Set the category of an item within the database")]
        public async Task SetItemCategoryCommandAsync([Remainder] string input)
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

            //Core of Command
            if (input.Contains("\""))
            {
                int firstIndex = input.IndexOf("\"");
                int lastIndex = input.LastIndexOf("\"");

                string itemNameInput = input.Substring(firstIndex + 1, (lastIndex - firstIndex) - 1);
                var args = new string[2];
                args[0] = itemNameInput;

                if (input.Length <= lastIndex + 1)
                {
                    await ReplyAsync("Incorrect syntax on item set! Use the following syntax ``!price setcategory \"item name\" category``");
                    return;
                }

                args[1] = input.Substring(lastIndex + 1);

                var itemName = await _andoraService.PriceDB.FindItem(args[0], _pollService, Context, true);
                if (itemName != null)
                {
                    _andoraService.PriceDB.SetCategory(itemName, args[1].TrimStart().TrimEnd());
                }
                else
                {
                    await ReplyAsync("Failed to set item category in the database. No existing item!");
                    return;
                }

                await ReplyAsync("Item successfully added to database!");
            }
            else
            {
                //Not a valid item addition
                await ReplyAsync("Incorrect syntax on item set! Use the following syntax ``!price setcategory \"item name\" category``");
                return;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Convert the first character of each word to upper. Ignore certain words.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string FormatToTitleCase(string value)
        {
            var retVal = "";
            var split = value.Split(' ');
            foreach (var word in split)
            {
                if (word.Equals("of") || word.Equals("the") || word.Equals("and") || word.Equals("in") || word.Equals("a"))
                {
                    retVal += word + " ";
                }
                else
                {
                    var firstChar = Char.ToUpper(word[0]);
                    var newWord = firstChar + word.Substring(1);
                    retVal += newWord + " ";
                }
            }
            retVal.TrimEnd();
            return retVal;
        }

        /// <summary>
        /// Search for the item within the database. Return a usable value
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task<Tuple<string, string, int, int, int>> FindItemInDatabase(string input, bool elevatedPermissions = false)
        {
            var itemName = await _andoraService.PriceDB.FindItem(input, _pollService, Context, elevatedPermissions);
            if (itemName != null)
            {
                var itemDetails = _andoraService.PriceDB.GetItemDetails(itemName);

                return new Tuple<string, string, int, int, int>(itemName, itemDetails.Item1, itemDetails.Item2, itemDetails.Item3, itemDetails.Item4);
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
