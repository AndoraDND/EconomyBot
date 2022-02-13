using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using EconomyBot.DataStorage;

namespace EconomyBot
{
    public class NPCPingService
    {
        /// <summary>
        /// Main discord client used by the bot
        /// </summary>
        private DiscordSocketClient _client;

        /// <summary>
        /// Collection containing relevant role mentions for this service.
        /// </summary>
        private Dictionary<ulong, ulong> _guildRoleLookup;

        /// <summary>
        /// Collection containing relevant channels to be watching in each server.
        /// </summary>
        private Dictionary<ulong, List<ulong>> _guildChannelLookup;

        /// <summary>
        /// Collection containing relevant channels to be reported in after NPC pings are made
        /// </summary>
        private Dictionary<ulong, ulong> _guildReportChannelLookup;

        public NPCPingService(DiscordSocketClient client)
        {
            _client = client;

            LoadGuildData();
        }

        /// <summary>
        /// Load the data held by this service from file
        /// </summary>
        private void LoadGuildData()
        {
            //Clear the role list if it contains values
            if(_guildRoleLookup != null)
            {
                _guildRoleLookup.Clear();
                _guildRoleLookup = null;
            }
            _guildRoleLookup = new Dictionary<ulong, ulong>();

            //Clear the report channel list if it contains values;
            if(_guildReportChannelLookup != null)
            {
                _guildReportChannelLookup.Clear();
                _guildReportChannelLookup = null;
            }
            _guildReportChannelLookup = new Dictionary<ulong, ulong>();

            //Clear the channel list if it contains values
            if(_guildChannelLookup != null)
            {
                _guildChannelLookup.Clear();
                _guildChannelLookup = null;
            }
            _guildChannelLookup = new Dictionary<ulong, List<ulong>>();
            
            foreach (var guild in _client.Guilds)
            {
                _guildRoleLookup.Add(guild.Id, ulong.MaxValue);
                _guildReportChannelLookup.Add(guild.Id, ulong.MaxValue);
                _guildChannelLookup.Add(guild.Id, new List<ulong>());
            }

            var fileData = FileReader.ReadCSV("NPCPingServerData");

            //Check for file load failure
            if (fileData == null)
            {
                Console.WriteLine("Error: Failed to load NPC Ping Service data file!");
                return;
            }

            foreach(var data in fileData)
            {
                if(data.Key == null || data.Key.Length == 0)
                {
                    continue;
                }
                ulong.TryParse(data.Key, out var guildID);

                if (data.Value.Length < 2)
                {
                    continue;
                }
                ulong.TryParse(data.Value[0], out var roleID);
                ulong.TryParse(data.Value[1], out var reportChannel);

                List<ulong> watchedChannels = null;
                var watchedChannelCount = data.Value.Length - 2;
                if (watchedChannelCount > 0)
                {
                    watchedChannels = new List<ulong>();

                    for (int i = 0; i < watchedChannelCount; i++)
                    {
                        if (data.Value[i + 2].Length > 0)
                        {
                            watchedChannels.Add(ulong.Parse(data.Value[i + 2]));
                        }
                    }
                }

                _guildRoleLookup[guildID] = roleID;
                _guildReportChannelLookup[guildID] = reportChannel;
                _guildChannelLookup[guildID] = watchedChannels;
            }
        }

        /// <summary>
        /// Save the data held by this service to file
        /// </summary>
        private void SaveGuildData()
        {
            string fileData = "";

            foreach(var guild in _client.Guilds)
            {
                if( _guildRoleLookup.TryGetValue(guild.Id, out var roleID) &&
                _guildReportChannelLookup.TryGetValue(guild.Id, out var reportChannelID) &&
                _guildChannelLookup.TryGetValue(guild.Id, out var watchChannelList))
                {
                    fileData += $"{guild.Id}, {roleID}, {reportChannelID},";

                    foreach(var channel in watchChannelList)
                    {
                        fileData += $" {channel},";
                    }
                    fileData += "\n";
                }
            }

            FileReader.WriteCSV("NPCPingServerData", fileData);
        }

        /// <summary>
        /// Add a guild to the list of saved data if it doesnt exist
        /// </summary>
        /// <param name="guild"></param>
        private void AddGuild(ulong guild)
        {
            if(!_guildRoleLookup.ContainsKey(guild)) 
                _guildRoleLookup.Add(guild, ulong.MaxValue);
            if (!_guildReportChannelLookup.ContainsKey(guild)) 
                _guildReportChannelLookup.Add(guild, ulong.MaxValue);
            if (!_guildChannelLookup.ContainsKey(guild))
                _guildChannelLookup.Add(guild, new List<ulong>());
        }

        /// <summary>
        /// Set the role that is being watched for pings
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        public void SetPingRole(ulong guild, ulong role)
        {
            if(!_guildRoleLookup.ContainsKey(guild))
            {
                AddGuild(guild);
            }

            _guildRoleLookup[guild] = role;

            SaveGuildData();
        }

        /// <summary>
        /// Set the channel that this service will report to
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="channel"></param>
        public void SetReportChannel(ulong guild, ulong channel)
        {
            if (!_guildReportChannelLookup.ContainsKey(guild))
            {
                AddGuild(guild);
            }

            _guildReportChannelLookup[guild] = channel;

            SaveGuildData();
        }

        /// <summary>
        /// Add a channel that will be watched for pings by this service
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="channel"></param>
        public void AddWatchChannel(ulong guild, ulong channel)
        {
            if (_guildChannelLookup.TryGetValue(guild, out var watchList))
            {
                if (!watchList.Contains(channel))
                {
                    _guildChannelLookup[guild].Add(channel);
                }
            }
            else
            {
                AddGuild(guild);
                _guildChannelLookup[guild].Add(channel);
            }

            SaveGuildData();
        }

        /// <summary>
        /// Remove a channel from the watch list in this service
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="channel"></param>
        public void RemoveWatchChannel(ulong guild, ulong channel)
        {
            if (_guildChannelLookup.TryGetValue(guild, out var watchList))
            {
                if (watchList.Contains(channel))
                {
                    _guildChannelLookup[guild].Remove(channel);
                }
            }

            SaveGuildData();
        }

        internal async Task HandleMessageReceived(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            // We don't want the bot to respond to itself or other bots.
            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

            if (msg.MentionedRoles.Count == 0) return;

            var channel = msg.Channel as SocketGuildChannel;
            if (channel == null) return;
            var guild = channel.Guild;

            if (_guildChannelLookup.ContainsKey(guild.Id) && _guildChannelLookup[guild.Id].Contains(channel.Id))
            {
                foreach (var role in msg.MentionedRoles)
                {
                    if (role.Id.Equals(_guildRoleLookup[guild.Id]))
                    {
                        //Our guild matches, its in a relevant channel, and its the correct role.

                        var chID = _guildReportChannelLookup[guild.Id];
                        if (chID != ulong.MaxValue)
                        {
                            var reportChannel = guild.GetTextChannel(chID);

                            var embedBuilder = new Discord.EmbedBuilder();

                            //TODO: Generate an interesting message
                            string msgLink = "https://discord.com/channels/" + guild.Id + "/" + channel.Id + "/" + msg.Id;
                            embedBuilder.WithColor(new Discord.Color(88, 148, 216));
                            embedBuilder.WithAuthor(new Discord.EmbedAuthorBuilder().WithName($"{msg.Author.Username}#{msg.Author.Discriminator}").WithIconUrl(msg.Author.GetAvatarUrl()));
                            embedBuilder.AddField(new Discord.EmbedFieldBuilder().WithName("Link").WithValue(msgLink));

                            string msgContext = "";
                            if (msg.Content.Contains('('))
                            {
                                if (msg.Content.Contains(')'))
                                {
                                    msgContext = msg.Content.Split('(', ')')[1];
                                }
                                else
                                {
                                    msgContext = msg.Content.Substring(msg.Content.IndexOf('('));
                                }
                            }

                            if (msgContext.Length > 0)
                            {
                                embedBuilder.WithDescription(msgContext);
                            }

                            await reportChannel.SendMessageAsync("", false, embedBuilder.Build());
                        }
                    }
                }
            }
        }
    }
}
