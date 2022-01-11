using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using EconomyBot.Commands.Preconditions;

namespace EconomyBot.Commands
{
    /// <summary>
    /// An example module for defining a ping command.
    /// For reference, all commands need to be public and derive from the ModuleBase class, 
    /// using a generic type of CommandContext or SocketCommandContext. The latter is more common.
    /// </summary>
    public class PingModule : ModuleBase<SocketCommandContext>
    {
        //Example class for simple commands. Notice in this class that you can define multiple commands. 
        //A module is a container for commands, but the commands are a single function.

        //Within a module, we pass data related to the Discord context. 
        //This ends up being a large amount of what code goes into these command modules.

        // - Useful context-specific variables and functions - 
        // Context.Channel                           -> The Channel this command was exectuted in.
        // Context.Guild                             -> The Guild (server) this command was exectuted in. 
        //                                                Don't assume that the command will always be received in a server though, 
        //                                                you can call commands through private messages as well!
        // Context.Client.CurrentUser                -> The User data for this bot
        // Context.IsPrivate                         -> Is the location of this command execution in a private message?
        // Context.Message                           -> The original message that this command was received in
        // Context.Message.Author                    -> Original user that called the command
        // Context.Message.Author.Mention            -> Mention the original user that called the command. 
        //                                                Note that this returns a string, so use it within a reply!
        // ReplyAsync(string message)                -> Fast way of posting a message in the channel the command was called in
        // Context.Message.ReplyAsync(string msg)    -> Creates a message replying to the original one. 
        // Context.Message.AddReactionAsyn(...)      -> React to a message with a specific emote. Getting the emote takes a bit of work...

        // !say hello world -> hello world
        [Command("say")]
        [Summary("Echoes a message.")]
        public Task SayAsync([Remainder] [Summary("The text to echo")] string echo) // The Remainder attribute makes it so string data doesn't get parsed. 
                                                                                    // Occasionally useful.
            => ReplyAsync(echo);

        //!ping     -> Pong!
        [Command("ping")]
        [Summary("Replies to the original message with the opposite phrase.")]
        public Task PingAsync() => ReplyAsync("Pong!"); 

        //Take notice of the parameter here (or rather lack-thereof). You can use a variety of parameters for these functions.
        //bool, char, sbyte/byte, ushort/short, uint/int, ulong/long, float, double, decimal, string, enum, 
        //DateTime/DateTimeOffset/TimeSpan, nullable types (ie int?, bool?),
        //as well as discord-intrinsic objects like IChannel, IMessage, IUser, IRole, IGuildUser, SocketUser, etc.

        //If needed, we can add more, though we should be able to handle everything using what we already have.

        //!pong     -> Ping!
        [Command("pong")]
        [Summary("Replies to the original message with the opposite phrase.")]
        public Task PongAsync() => ReplyAsync("Ping!");

        // The following example only requires the user to either have the
        // Administrator permission in this guild or own the bot application.

        //!ban @Khionu  -> User @Khionu has been banned from the server.
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")] //Both of these require their group to be the same (ie "Permission"),
        [RequireOwner(Group = "Permission")]                                         //as this means that only one of the conditions needs to be satisfied.
        public class ExamplePermModule : ModuleBase<SocketCommandContext>
        {
            [Command("ban")]
            public Task BanAsync(IUser user) => Context.Guild.AddBanAsync(user);
        }

        //!pout     -> The champion @Khionu pouts.
        [RequireRole("Champion")] //Our custom precondition for making sure the user has a specific role
        public class ExampleRolePermModule : ModuleBase<SocketCommandContext>
        {
            [Command("pout")]
            public Task PoutAsync(IUser user) => Context.Channel.SendMessageAsync($"The champion {Context.Message.Author.Mention} pouts.");
        }
    }
}
