using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using EconomyBot.DataStorage;
using Newtonsoft.Json;

namespace EconomyBot
{
    [System.Serializable]
    public class NotifierGuildData
    {
        public ulong GuildID { get; set; }

        public Dictionary<int, ulong> TrackedRoles { get; set; }

        public Dictionary<ulong, ulong> ReportChannels { get; set; } //RoleID, ChannelID

        public Dictionary<ulong, Tuple<byte, byte, byte>> RoleEmbedColor { get; set; }
    }

    public class NotifierService
    {
        /// <summary>
        /// Main discord client used by the bot
        /// </summary>
        private DiscordSocketClient _client;

        /// <summary>
        /// Dictionary of tracked guilds and their specific data.
        /// </summary>
        private Dictionary<ulong, NotifierGuildData> _trackedGuildList;

        public NotifierService(DiscordSocketClient client)
        {
            _client = client;

            LoadGuildData();
        }

        /// <summary>
        /// Load the data held by this service from file
        /// </summary>
        private void LoadGuildData()
        {
            var jsonData = FileReader.ReadJSON("NotifierGuildData");
            if (jsonData != null)
            {
                _trackedGuildList = JsonConvert.DeserializeObject<Dictionary<ulong, NotifierGuildData>>(jsonData);
            }
            else
            {
                _trackedGuildList = new Dictionary<ulong, NotifierGuildData>();
            }
        }

        /// <summary>
        /// Save the data held by this service to file
        /// </summary>
        private void SaveGuildData()
        {
            var jsonData = JsonConvert.SerializeObject(_trackedGuildList);
            if (jsonData != null)
            {
                FileReader.WriteJson("NotifierGuildData", jsonData);
            }
            else
            {
                Console.WriteLine("Failed to serialize NotifierGuildData object.");
            }
        }

        internal NotifierGuildData GetGuildData(ulong guildID)
        {
            NotifierGuildData retVal = null;

            if (_trackedGuildList.ContainsKey(guildID))
            {
                retVal = _trackedGuildList[guildID];
            }

            return retVal;
        }

        /// <summary>
        /// Add a guild to the list of saved data if it doesnt exist
        /// </summary>
        /// <param name="guild"></param>
        private void AddGuild(ulong guild)
        {
            if (!_trackedGuildList.ContainsKey(guild))
            {
                _trackedGuildList.Add(guild, new NotifierGuildData()
                {
                    GuildID = guild,
                    ReportChannels = new Dictionary<ulong, ulong>(),
                    TrackedRoles = new Dictionary<int, ulong>(),
                    RoleEmbedColor = new Dictionary<ulong, Tuple<byte, byte, byte>>()
                });;
            }
        }

        /// <summary>
        /// Set the role that is being watched for pings
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        public void AddPingRole(ulong guild, ulong role, int id) //SetPingRole
        {
            AddGuild(guild);

            if (_trackedGuildList.ContainsKey(guild))
            {
                if (!_trackedGuildList[guild].TrackedRoles.ContainsKey(id) && !_trackedGuildList[guild].TrackedRoles.ContainsValue(role))
                {
                    _trackedGuildList[guild].TrackedRoles.Add(id, role);
                }

                SaveGuildData();
            }
        }

        /// <summary>
        /// Remove a role set for pings
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        public void RemovePingRole(ulong guild, ulong role)
        {
            if (_trackedGuildList.ContainsKey(guild))
            {
                if (_trackedGuildList[guild].TrackedRoles.ContainsValue(role))
                {
                    var kvp = _trackedGuildList[guild].TrackedRoles.ToList().Where(p => p.Value.Equals(role));

                    foreach (var pair in kvp) 
                    {
                        _trackedGuildList[guild].TrackedRoles.Remove(pair.Key);
                    }
                }

                if (_trackedGuildList[guild].ReportChannels.ContainsKey(role))
                {
                    _trackedGuildList[guild].ReportChannels.Remove(role);
                }

                if (_trackedGuildList[guild].RoleEmbedColor.ContainsKey(role))
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

            if (_trackedGuildList.ContainsKey(guild))
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

            if (_trackedGuildList.ContainsKey(guild))
            {
                if (_trackedGuildList[guild].TrackedRoles.ContainsValue(roleID))
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

        internal async Task HandleDispatch(ulong guildID, string runner, string missionDate, List<string> playersInvolved, string message)
        {
            if (!_trackedGuildList.TryGetValue(guildID, out var guild_data))
            {
                //Didn't find guild data. Error out.
                return;
            }

            List<int> messageGroups = CheckMessageContents(message);

            if (messageGroups.Count <= 0)
                return;

            //Make the embed.
            string playerList = "";
            foreach(var player in playersInvolved)
            {
                playerList += player + "\n";
            }

            var embedBuilder = new Discord.EmbedBuilder();
            embedBuilder.WithTitle("Relevant Mission Report");
            embedBuilder.WithDescription("A new post report has been handled by the bot.");
            embedBuilder.AddField(new Discord.EmbedFieldBuilder().WithName("Runner").WithValue(runner).WithIsInline(true));
            embedBuilder.AddField(new Discord.EmbedFieldBuilder().WithName("Date").WithValue(missionDate).WithIsInline(true));
            embedBuilder.AddField(new Discord.EmbedFieldBuilder().WithName("Events").WithValue(message).WithIsInline(false));
            embedBuilder.AddField(new Discord.EmbedFieldBuilder().WithName("Players").WithValue(playerList).WithIsInline(false));

            foreach (var id in messageGroups)
            {
                guild_data.TrackedRoles.TryGetValue(id, out var roleID);

                var channelID = guild_data.ReportChannels[roleID];
                var channel = _client.GetGuild(guildID).GetTextChannel(channelID);
                if (channel == null) continue;

                if (guild_data.RoleEmbedColor.ContainsKey(roleID)) 
                {
                    var colorVal = guild_data.RoleEmbedColor[roleID];
                    embedBuilder.WithColor(new Color(colorVal.Item1, colorVal.Item2, colorVal.Item3));
                }

                await channel.SendMessageAsync("", false, embedBuilder.Build());
            }
        }

        private List<string> DivinityChecklist = new List<string>()
        {
            "god",
            "prayer",
            "shrine",
            "pact",
            "patron",
            "blasphem",
            "desecrat",
            "holy"
        };

        //Divinity - 1

        /// <summary>
        /// Checks a message to see if it has any particular keywords that might indicate needing to report to another department.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>1 for Divinity, 2 for Political, 3 for Admin</returns>
        private List<int> CheckMessageContents(string message)
        {
            var retVal = new List<int>();

            foreach(var keyword in DivinityChecklist)
            {
                if (message.ToLower().Contains(keyword))
                {
                    retVal.Add(1);
                    break;
                }
            }

            //foreach (var keyword in PoliticalChecklist)
            //{
            //    if (message.ToLower().Contains(keyword))
            //    {
            //        retVal.Add(2);
            //        break;
            //    }
            //}

            //foreach (var keyword in AdminChecklist)
            //{
            //    if (message.ToLower().Contains(keyword))
            //    {
            //        retVal.Add(3);
            //        break;
            //    }
            //}

            return retVal;
        }
    }
}
