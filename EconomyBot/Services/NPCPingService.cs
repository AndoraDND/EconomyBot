using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using EconomyBot.DataStorage;
using Newtonsoft.Json;

namespace EconomyBot
{
    [System.Serializable]
    public class NPCPingGuildData 
    {
        public ulong GuildID { get; set; }

        public List<ulong> WatchedChannels { get; set; }

        public List<ulong> TrackedRoles { get; set; } //RoleID

        public Dictionary<ulong, ulong> ReportChannels { get; set; } //RoleID, ChannelID
    
        public Dictionary<ulong, Tuple<byte, byte, byte>> RoleEmbedColor { get; set; }
    }

    public class NPCPingService
    {
        /// <summary>
        /// Main discord client used by the bot
        /// </summary>
        private DiscordSocketClient _client;

        /// <summary>
        /// Dictionary of tracked guilds and their specific data.
        /// </summary>
        private Dictionary<ulong, NPCPingGuildData> _trackedGuildList;

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
#if false
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
#endif

            var jsonData = FileReader.ReadJSON("NPCPingGuildData");
            if(jsonData != null)
            {
                _trackedGuildList = JsonConvert.DeserializeObject<Dictionary<ulong, NPCPingGuildData>>(jsonData);
            }
            else
            {
                _trackedGuildList = new Dictionary<ulong, NPCPingGuildData>();
            }
        }

        /// <summary>
        /// Save the data held by this service to file
        /// </summary>
        private void SaveGuildData()
        {
#if false
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
#endif 
            var jsonData = JsonConvert.SerializeObject(_trackedGuildList);
            if (jsonData != null)
            {
                FileReader.WriteJson("NPCPingGuildData", jsonData);
            }
            else
            {
                Console.WriteLine("Failed to serialize NPCPingGuildData object.");
            }
        }

        internal NPCPingGuildData GetGuildData(ulong guildID)
        {
            NPCPingGuildData retVal = null;

            if(_trackedGuildList.ContainsKey(guildID))
            {
                retVal = _trackedGuildList[guildID];
            }

#if false
            if(_guildChannelLookup.ContainsKey(guildID))
            {
                retVal = _guildChannelLookup[guildID];
            }
#endif
            return retVal;
        }

        /// <summary>
        /// Add a guild to the list of saved data if it doesnt exist
        /// </summary>
        /// <param name="guild"></param>
        private void AddGuild(ulong guild)
        {
            if(!_trackedGuildList.ContainsKey(guild))
            {
                _trackedGuildList.Add(guild, new NPCPingGuildData() 
                {
                    GuildID = guild,
                    ReportChannels = new Dictionary<ulong, ulong>(),
                    TrackedRoles = new List<ulong>(),
                    WatchedChannels = new List<ulong>(),
                    RoleEmbedColor = new Dictionary<ulong, Tuple<byte, byte, byte>>()
                });
            }

#if false
            if(!_guildRoleLookup.ContainsKey(guild)) 
                _guildRoleLookup.Add(guild, ulong.MaxValue);
            if (!_guildReportChannelLookup.ContainsKey(guild)) 
                _guildReportChannelLookup.Add(guild, ulong.MaxValue);
            if (!_guildChannelLookup.ContainsKey(guild))
                _guildChannelLookup.Add(guild, new List<ulong>());
#endif
        }

        /// <summary>
        /// Set the role that is being watched for pings
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        public void AddPingRole(ulong guild, ulong role) //SetPingRole
        {
            AddGuild(guild);

            if(_trackedGuildList.ContainsKey(guild))
            {
                if(!_trackedGuildList[guild].TrackedRoles.Contains(role))
                {
                    _trackedGuildList[guild].TrackedRoles.Add(role);
                }

                SaveGuildData();
            }

#if false
            if(!_guildRoleLookup.ContainsKey(guild))
            {
                AddGuild(guild);
            }

            _guildRoleLookup[guild] = role;

            SaveGuildData();
#endif
        }

        public void RemovePingRole(ulong guild, ulong role)
        {
            if (_trackedGuildList.ContainsKey(guild))
            {
                if (_trackedGuildList[guild].TrackedRoles.Contains(role))
                {
                    _trackedGuildList[guild].TrackedRoles.Remove(role);
                }

                if (_trackedGuildList[guild].ReportChannels.ContainsKey(role))
                {
                    _trackedGuildList[guild].ReportChannels.Remove(role);
                }

                if(_trackedGuildList[guild].RoleEmbedColor.ContainsKey(role))
                {
                    _trackedGuildList[guild].RoleEmbedColor.Remove(role);
                }

                SaveGuildData();
            }
        }

        /// <summary>
        /// Set the channel that this service will report to
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="channel"></param>
        public void SetReportChannel(ulong guild, ulong roleid, ulong channelid)
        {
            AddGuild(guild);

            if(_trackedGuildList.ContainsKey(guild))
            {
                if (!_trackedGuildList[guild].ReportChannels.ContainsKey(roleid))
                {
                    _trackedGuildList[guild].ReportChannels.Add(roleid, channelid);
                }
                else
                {
                    _trackedGuildList[guild].ReportChannels[roleid] = channelid;
                }

                SaveGuildData();
            }

#if false
            if (!_guildReportChannelLookup.ContainsKey(guild))
            {
                AddGuild(guild);
            }

            _guildReportChannelLookup[guild] = channel;

            SaveGuildData();
#endif
        }

        /// <summary>
        /// Add a channel that will be watched for pings by this service
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="channel"></param>
        public void AddWatchChannel(ulong guild, ulong channelid)
        {
            AddGuild(guild);

            if(_trackedGuildList.ContainsKey(guild))
            {
                if(!_trackedGuildList[guild].WatchedChannels.Contains(channelid))
                {
                    _trackedGuildList[guild].WatchedChannels.Add(channelid);
                }
            }

#if false
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
#endif
        }

        /// <summary>
        /// Remove a channel from the watch list in this service
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="channel"></param>
        public void RemoveWatchChannel(ulong guild, ulong channel)
        {
            if(_trackedGuildList.ContainsKey(guild))
            {
                if(_trackedGuildList[guild].WatchedChannels.Contains(channel))
                {
                    _trackedGuildList[guild].WatchedChannels.Remove(channel);
                }

                SaveGuildData();
            }

#if false
            if (_guildChannelLookup.TryGetValue(guild, out var watchList))
            {
                if (watchList.Contains(channel))
                {
                    _guildChannelLookup[guild].Remove(channel);
                }
            }

            SaveGuildData();
#endif
        }

        /// <summary>
        /// Set the color for the embed of a specific ping
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="roleID"></param>
        /// <param name="hexColorValue"></param>
        /// <returns></returns>
        public bool SetColor(ulong guild, ulong roleID, string hexColorValue)
        {
            Color color = default(Color);
            try
            {
                var htmlColor = System.Drawing.ColorTranslator.FromHtml(hexColorValue);
                color = new Color(htmlColor.R, htmlColor.G, htmlColor.B);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            if(_trackedGuildList.ContainsKey(guild))
            {
                if(_trackedGuildList[guild].TrackedRoles.Contains(roleID))
                {
                    if (_trackedGuildList[guild].RoleEmbedColor.ContainsKey(roleID))
                    {
                        _trackedGuildList[guild].RoleEmbedColor[roleID] = new Tuple<byte, byte, byte>(color.R, color.G, color.B);
                    }
                    else
                    {
                        _trackedGuildList[guild].RoleEmbedColor.Add(roleID, new Tuple<byte, byte, byte>(color.R, color.G, color.B));
                    }

                    return true;
                }
            }

            return false;
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

            if(!_trackedGuildList.TryGetValue(guild.Id, out var guild_data))
            {
                //Didn't find guild data. Error out.
                return;
            }

            if (guild_data.WatchedChannels.Contains(channel.Id))
            {
                foreach(var role in msg.MentionedRoles)
                {
                    if(guild_data.TrackedRoles.Contains(role.Id))
                    {
                        //Our guild matches, its in a relevant channel, and we have a tracked role.

                        if(guild_data.ReportChannels.TryGetValue(role.Id, out var reportChannelID))
                        {
                            //Add a "ping received" notification to the original message.
                            //We do this now, since we have somewhere for the data to be seen.
                            await msg.AddReactionAsync(Discord.Emoji.Parse("\uD83D\uDC40"));//\x0031\xFE0F\x20E3"));

                            var reportChannel = guild.GetTextChannel(reportChannelID);

                            var embedBuilder = new Discord.EmbedBuilder();

                            string msgLink = "https://discord.com/channels/" + guild.Id + "/" + channel.Id + "/" + msg.Id;

                            //Set embed color
                            if (guild_data.RoleEmbedColor.ContainsKey(role.Id))
                            {
                                var color = guild_data.RoleEmbedColor[role.Id];
                                embedBuilder.WithColor(new Discord.Color(color.Item1, color.Item2, color.Item3));
                            }
                            else
                            {
                                embedBuilder.WithColor(new Discord.Color(88, 148, 216));
                            }
                            
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
                            else
                            {
                                var splitMessage = msg.Content.Split(new char[] { '.', '?', '!' });
                                for(int i = splitMessage.Length-1; i >= 0; i--)
                                {
                                    if(splitMessage[i] != null && splitMessage[i].Length > 0)
                                    {
                                        msgContext = splitMessage[i];
                                        break;
                                    }
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

#if false
            if (_guildChannelLookup.ContainsKey(guild.Id) && _guildChannelLookup[guild.Id].Contains(channel.Id))
            {
                foreach (var role in msg.MentionedRoles)
                {
                    if (role.Id.Equals(_guildRoleLookup[guild.Id]))
                    {
                        //Our guild matches, its in a relevant channel, and its the correct role.

                        //Add a "ping received" notification
                        await msg.AddReactionAsync(Discord.Emoji.Parse("\uD83D\uDC40"));//\x0031\xFE0F\x20E3"));

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
#endif
        }
    }
}
