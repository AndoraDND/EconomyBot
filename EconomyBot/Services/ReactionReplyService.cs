using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace EconomyBot
{
    public class ReactionReply
    {
        /// <summary>
        /// The final value from the poll
        /// </summary>
        public int ReturnValue = -1;

        /// <summary>
        /// Message ID of the poll response. Used for cleanup
        /// </summary>
        public ulong ResponseMessageID;
        
        /// <summary>
        /// Message ID of the poll
        /// </summary>
        public ulong MessageID;

        /// <summary>
        /// Channel ID of the message
        /// </summary>
        public ulong MessageChannel;

        /// <summary>
        /// Person who originally called for this poll
        /// </summary>
        public ulong CallerID;

        /// <summary>
        /// List of emote options for this poll
        /// </summary>
        public List<string> Options;
    }

    public class ReactionReplyService
    {
        private Dictionary<ulong, ReactionReply> _currentReplyQueue = new Dictionary<ulong, ReactionReply>();

        private List<Emoji> _numeralEmoteReaction = new List<Emoji>()
        {
            Emoji.Parse("\x0031\xFE0F\x20E3"),
            Emoji.Parse("\x0032\xFE0F\x20E3"),
            Emoji.Parse("\x0033\xFE0F\x20E3"),
            Emoji.Parse("\x0034\xFE0F\x20E3"),
            Emoji.Parse("\x0035\xFE0F\x20E3"),
            Emoji.Parse("\x0036\xFE0F\x20E3"),
            Emoji.Parse("\x0037\xFE0F\x20E3"),
            Emoji.Parse("\x0038\xFE0F\x20E3"),
            Emoji.Parse("\x0039\xFE0F\x20E3")
        };

        /// <summary>
        /// Unused. Former code.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        internal Task OnReactionReceived(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            /*
            if(_currentReplyQueue.ContainsKey(message.Id) && !reaction.User.Value.IsBot)
            {
                //Message is waiting for answer, handle this queue
                var numeralIndex = _numeralEmoteReaction.FindIndex(p => p.Name.Equals(reaction.Emote.Name));
                if (numeralIndex != -1)
                {
                    int resultIndex = numeralIndex; 
                    //int resultIndex = _currentReplyQueue[message.Id].Options.IndexOf(reaction.Emote.Name);
                    _currentReplyQueue[message.Id].ReturnValue = resultIndex;

                    RemoveItem(message.Id);
                }
            }
            */

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called by the Discord Client when a message is received. We only use it for messages that pertain to our current running polls.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal Task OnMessageReceived(SocketMessage message)
        {
            if(_currentReplyQueue.ContainsKey(message.Author.Id))
            {
                var poll = _currentReplyQueue[message.Author.Id];
                if(poll.MessageChannel.Equals(message.Channel.Id))
                {
                    var text = message.Content.TrimStart().TrimEnd();
                    if (text.Length < 2 && text.Length > 0)
                    {
                        int.TryParse(text, out var returnVal);

                        if (poll.Options.Count >= returnVal)
                        {
                            poll.ReturnValue = returnVal - 1;
                            poll.ResponseMessageID = message.Id;

                            RemoveItem(message.Author.Id);
                        }
                    }
                }
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Remove a tracked poll from this service.
        /// </summary>
        /// <param name="messageAuthor"></param>
        internal void RemoveItem(ulong messageAuthor)
        {
            if (_currentReplyQueue.ContainsKey(messageAuthor))
            {
                _currentReplyQueue.Remove(messageAuthor);
            }
        }

        /// <summary>
        /// Add a tracked poll to this service.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagePoll"></param>
        /// <returns></returns>
        internal async Task AddItem(Discord.Rest.RestUserMessage message, ReactionReply messagePoll)
        {
            if (!_currentReplyQueue.ContainsKey(messagePoll.CallerID))
            {
                _currentReplyQueue.Add(messagePoll.CallerID, messagePoll);
            }

            /*
            var emoteOptions = new List<Emoji>();
            for(int i = 0; i < messagePoll.Options.Count; i++)
            {
                emoteOptions.Add(_numeralEmoteReaction[i]);
            }
            */

            //await message.AddReactionsAsync(emoteOptions.ToArray());
        }

        /// <summary>
        /// Create a poll to verify a selection from a list of options.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="Options"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal async Task<string> CreatePoll(string message, List<string> Options, SocketCommandContext context)
        {
            string formattedOptionsList = "";
            for(int i = 0; i < Options.Count; i++)
            {
                formattedOptionsList += $"\n{_numeralEmoteReaction[i]} - {Options[i]}";
            }
            var discordMessage = await context.Channel.SendMessageAsync(message + formattedOptionsList);

            var reactionReply = new ReactionReply()
            {
                CallerID = context.Message.Author.Id,
                MessageID = discordMessage.Id,
                MessageChannel = discordMessage.Channel.Id,
                Options = Options
            };

            //Add a poll to the list of tracked polls within this service.
            await AddItem(discordMessage, reactionReply);

            int timeOut = 1000 * 60;

            //var task = CheckMessageReplyFinished(reactionReply);

            await WaitWhile(() => reactionReply.ReturnValue == -1, 25, timeOut);

            if(reactionReply.ReturnValue == -1)
            {
                //Console.WriteLine("Timed out.");
                await context.Channel.DeleteMessageAsync(discordMessage);
                return null;
            }
            else
            {
                //Console.WriteLine($"Got poll result : {reactionReply.Options[reactionReply.ReturnValue]}");
                await context.Channel.DeleteMessageAsync(discordMessage);
                await context.Channel.DeleteMessageAsync(reactionReply.ResponseMessageID);
                return Options[reactionReply.ReturnValue];
            }

            /*
            if (await Task.WhenAny(task, Task.Delay(timeOut)) == task)
            {
                //Console.WriteLine($"Got poll result : {reactionReply.Options[reactionReply.ReturnValue]}");
                await context.Channel.DeleteMessageAsync(discordMessage);
                return Options[reactionReply.ReturnValue];
            }
            else
            {
                //Console.WriteLine("Timed out.");
                await context.Channel.DeleteMessageAsync(discordMessage);
                return null;
            }
            */
        }

        /// <summary>
        /// Blocks while condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the block.</param>
        /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                throw new TimeoutException();
        }
    }
}
