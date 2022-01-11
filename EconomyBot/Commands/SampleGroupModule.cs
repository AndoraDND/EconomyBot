using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace EconomyBot.Commands
{
    /// <summary>
    /// An example module for defining a group of commands. More complicated than the ping commands.
    /// For reference, all commands need to be public and derive from the ModuleBase class, 
    /// using a generic type of CommandContext or SocketCommandContext. The latter is more common.
    /// </summary>
    [Group("sample")]
    public class SampleGroupModule : ModuleBase<SocketCommandContext>
    {
        //In order to add specific services via Dependency Injection, we cache the service within this module, assigning it in the constructor.
        private readonly AndoraService _andoraService;

        public SampleGroupModule(AndoraService service) //Constructor for the module. Used for caching the services we need.
        {
            _andoraService = service; //Cache the AndoraService passed from the context.
        }

        //This class is an example of groups. The following are examples of command usage and what they would look like.
        //Notice in these examples that the command name is a subcommand of the group 'sample'. 
        //This is the effect of the [Group("sample")] attribute on the class.

        //!sample first 5                   -> 5
        //!sample second 5                  -> 10
        //!sample third                     -> foxbot#0282
        //!sample third @Khionu             -> Khionu#8708
        //!sample third 96642168176807936   -> Khionu#8708
        //!sample whois                     -> foxbot#0282
        //!sample whois @Khionu             -> Khionu#8708

        //If needed you may define submodules as groups within groups, though I would not suggest this as it can lead to some unclean code.
        // - Example: -
        // [Group("admin")]
        // public class AdminModule : ModuleBase<SocketCommandContext>
        // {
        //     [Group("clean")]
        //     public class CleanModule : ModuleBase<SocketCommandContext>
        //     {
        //         // !admin clean
        //         [Command]
        //         public async Task DefaultCleanAsync()
        //         {
        //             // ...
        //         }
        //
        //         // !admin clean messages 15
        //         [Command("messages")]
        //         public async Task CleanAsync(int count)
        //         {
        //            // ...

        [Command("first")]
        [Summary("Takes a number, multiplies it by one.")]
        public async Task FirstCommandAsync([Summary("Number to multiply.")] int num) //We can use a variety of parameters for commands.
        {
            await Context.Channel.SendMessageAsync($"{num}*1 = {num * 1} {_andoraService.CurrentYear}");
        }

        [Command("second")]
        [Summary("Takes a number, multiplies it by two.")]
        public async Task SecondCommandAsync([Summary("Number to multiply.")] int num) 
        {
            await Context.Channel.SendMessageAsync($"{num}*2 = {num * 2}");
        }

        [Command("third")]
        [Alias("user", "whois")] //You can define alternative names for the command.
        [Summary("Takes a user as mention, returns info about them.")]
        public async Task FirstCommandAsync([Summary("The (optional) user to get info about")] SocketUser user = null) 
        {
            var userInfo = user ?? Context.Client.CurrentUser; //Use the bot user if we don't have a ping.
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }

        //An example command for showcasing access to the Andora Service given by the service provider.

        //!sample fourth    -> The current year is 100
        //!sample year      -> The current year is 100

        [Command("fourth")]
        [Alias("year")]
        [Summary("Get the current year in Andora")]
        public async Task FourthCommandAsync()
        {
            await ReplyAsync($"The current year is {_andoraService.CurrentYear}");
        }
    }
}
