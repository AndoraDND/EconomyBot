using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using EconomyBot.DataStorage;

namespace EconomyBot
{
    internal class MessageObj 
    {
        internal string GUID;
        internal string Message;
        internal ulong GuildID;
        internal ulong GuildChannelID;
        internal DateTime StartTime;
        internal TimeSpan RecurringInterval;
        internal DateTime NextOccurance;
        internal bool FlagForDeletion { get; private set; }

        internal MessageObj(string message, ulong guildID, ulong channelID, DateTime startTime, TimeSpan recurringInterval = default(TimeSpan))
        {
            GUID = System.Guid.NewGuid().ToString();
            Message = message;
            GuildID = guildID;
            GuildChannelID = channelID;
            StartTime = startTime;
            RecurringInterval = recurringInterval;

            FlagForDeletion = false;
            CalculateNextOccurance();
        }

        internal MessageObj(string guid, string message, ulong guildID, ulong channelID, DateTime startTime, TimeSpan recurringInterval)
        {
            GUID = guid;
            Message = message;
            GuildID = guildID;
            GuildChannelID = channelID;
            StartTime = startTime;
            RecurringInterval = recurringInterval;

            FlagForDeletion = false;
            CalculateNextOccurance();
        }
        
        /// <summary>
        /// Calculate the next upcoming occurance of this message.
        /// </summary>
        internal void CalculateNextOccurance()
        {
            if (StartTime > DateTime.Now)   //Start time is in the future. Do nothing.
            {
                NextOccurance = StartTime;
            }
            else
            {
                if (RecurringInterval > default(TimeSpan))  //We don't have a recurring interval
                {
                    while ((StartTime + RecurringInterval) <= DateTime.Now)
                    {
                        StartTime += RecurringInterval;
                    }

                    if (StartTime > DateTime.Now)
                    {
                        Console.WriteLine("Encountered Next Occurance edge case. Setting to correct start time.");
                        NextOccurance = StartTime;
                    }
                    else
                    {
                        NextOccurance = StartTime + RecurringInterval;
                    }
                }
                else
                {
                    //What the heck is going on here.
                    NextOccurance = DateTime.Now + TimeSpan.FromMinutes(10); //Some arbitrary value so the next occurance isn't called.
                    FlagForDeletion = true; //Delete the message ASAP

                    Console.WriteLine("Error: Next occurance while calculating ended up in edge case <Start time already occured and Interval is zero>");
                }
            }
        }

        /// <summary>
        /// Send the message managed by this object.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public async Task PrintMessage(DiscordSocketClient client)
        {
            var guild = client.GetGuild(GuildID);
            var channel = guild.GetTextChannel(GuildChannelID);

            await channel.SendMessageAsync(Message);

            //Check if this is a single occurance
            if (RecurringInterval.Equals(TimeSpan.Zero))
            {
                FlagForDeletion = true;
            }
            else
            {
                CalculateNextOccurance();
            }
        }

        public string Print(int indent = 4)
        {
            var indentSpace = "";
            for (int i = 0; i < indent; i++)
            {
                indentSpace += " ";
            }

            var retVal = "";
            retVal = $"{indentSpace}GUID: {GUID},\n" +
                $"{indentSpace}Message: {Message},\n" +
                $"{indentSpace}GuildID: {GuildID},\n" +
                $"{indentSpace}ChannelID: {GuildChannelID},\n" +
                $"{indentSpace}StartTime: {StartTime.ToString()},\n" +
                $"{indentSpace}Interval: {RecurringInterval.ToString()},\n" +
                $"{indentSpace}NextOccurance: {NextOccurance.ToString()}";
            return retVal;
        }
    }

    public class MessageHandler
    {
        private List<MessageObj> _loadedMessages;

        public MessageHandler()
        {
            _loadedMessages = new List<MessageObj>();
            LoadMessagesFromFile();
        }

        /// <summary>
        /// Load the database of messages into memory
        /// </summary>
        private void LoadMessagesFromFile()
        {
            var dataPath = "MessageCache";
            var output = FileReader.ReadCSV(dataPath);

            if(output == null)
            {
                return;
            }

            _loadedMessages.Clear();
            foreach (var msg in output)
            {
                if (msg.Key == null || msg.Key.Length < 0 || msg.Value == null || msg.Value.Length < 5)
                    return;

                var newMsg = new MessageObj(msg.Key, msg.Value[0].TrimStart(), ulong.Parse(msg.Value[1]), ulong.Parse(msg.Value[2]), DateTime.Parse(msg.Value[3]), TimeSpan.Parse(msg.Value[4]));
                //newMsg.CalculateNextOccurance();

                _loadedMessages.Add(newMsg);
            }
        }

        /// <summary>
        /// Save the internal message database to file.
        /// </summary>
        private void SaveMessagesToFile()
        {
            var dataPath = "MessageCache";
            var fileData = "";

            foreach(var msg in _loadedMessages)
            {
                fileData += $"{msg.GUID}, {msg.Message}, {msg.GuildID}, {msg.GuildChannelID}, {msg.StartTime.ToString()}, {msg.RecurringInterval.ToString()},\n";
            }

            FileReader.WriteCSV(dataPath, fileData);
        }
    
        /// <summary>
        /// Add a message to the message handler
        /// </summary>
        /// <param name="message">Message to be posted</param>
        /// <param name="guildID">ulong ID of the discord guild/server to post in</param>
        /// <param name="channelID">ulong ID of the discord channel to post in</param>
        /// <param name="startTime">Date and Time for this message to post</param>
        /// <param name="recurringInterval">Interval at which this message will repost. Default will not recurr.</param>
        public void AddMessage(string message, ulong guildID, ulong channelID, DateTime startTime, TimeSpan recurringInterval = default(TimeSpan))
        {
            _loadedMessages.Add(new MessageObj(message, guildID, channelID, startTime, recurringInterval));
            _loadedMessages.Sort((a,b) => DateTime.Compare(a.NextOccurance, b.NextOccurance));

            SaveMessagesToFile();
        }

        /// <summary>
        /// Remove a message from this handler based on its GUID
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool RemoveMessage(string guid)
        {
            var index = _loadedMessages.FindIndex(p=>p.GUID.Equals(guid));

            if (index >= 0)
            {
                _loadedMessages.RemoveAt(index);

                SaveMessagesToFile();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all messages within the queue that are finalized. This is indicated by its deletion flag being true.
        /// </summary>
        public void ClearFlaggedMessages()
        {
            for (int i = _loadedMessages.Count - 1; i >= 0; i--)
            {
                if (_loadedMessages[i].FlagForDeletion)
                {
                    _loadedMessages.RemoveAt(i);
                }
            }

            SaveMessagesToFile();
        }

        /// <summary>
        /// Update this message handler. Called by the main program during program ticks.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public async Task Tick(DiscordSocketClient client)
        {
            if (client.ConnectionState != Discord.ConnectionState.Connected)
                return;

            var timeNow = DateTime.Now;
            for(int i = 0; i < _loadedMessages.Count; i++)
            {
                if (!_loadedMessages[i].NextOccurance.Equals(default(DateTime)) && _loadedMessages[i].NextOccurance <= timeNow)
                {
                    await _loadedMessages[i].PrintMessage(client);
                }
            }

            //Clear completed messages.
            ClearFlaggedMessages();
        }

        /// <summary>
        /// Get all data currently within this database.
        /// </summary>
        /// <returns></returns>
        internal string Dump()
        {
            var output = "";
            foreach (var item in _loadedMessages)
            {
                output += (item.Print()) + "\n\n";
            }
            return output;
        }
    }
}
