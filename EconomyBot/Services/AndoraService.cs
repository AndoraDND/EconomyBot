using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using EconomyBot.Commands;
using EconomyBot.DataStorage;

namespace EconomyBot
{
    public class AndoraService
    {
        /// <summary>
        /// Current Year in Andora.
        /// </summary>
        public string CurrentYear { get { return "100"; } }

        /// <summary>
        /// Service that manages NPC pings.
        /// </summary>
        public NPCPingService NPCPingService { get; set; }

        /// <summary>
        /// Service that handles distribution of embed notifications to relevant channels.
        /// </summary>
        public NotifierService NotifierService { get; set; }

        /// <summary>
        /// Database for Item costs
        /// </summary>
        public PriceDatabase PriceDB { get; set; }

        /// <summary>
        /// Parser for Avarae Google-Sheets-based character sheets.
        /// </summary>
        public AvraeSheetParser AvraeParser { get; set; }

        /// <summary>
        /// Parser for the Andora Tracking sheet.
        /// </summary>
        public TrackingSheetParser TrackingSheetParser { get; set; }

        /// <summary>
        /// Parser for Post Reports.
        /// </summary>
        public PostReportParser PostReportParser { get; set; }

        /// <summary>
        /// Database of character data relevant to Andora.
        /// </summary>
        public CharacterDatabase CharacterDB { get; set; }

        /// <summary>
        /// Hastur backend service. Used for storage of data for characters
        /// </summary>
        public AndoraDatabase AndoraDB { get; set; }

        /// <summary>
        /// Lookup table for specific roles
        /// </summary>
        public List<ulong> ElevatedStatusRoles { get; set; }

        /// <summary>
        /// Lookup table for tool proficiencies and their DTD gold generation, in copper.
        /// </summary>
        public Dictionary<string, int> DTDToolValues { get; set; }

        internal AndoraService(Discord.WebSocket.DiscordSocketClient client, TokenCredentials credentials)
        {
            var gSheetsCredPath = "Data/andora-3db990b2eff4.json";

            NPCPingService = new NPCPingService(client);
            NotifierService = new NotifierService(client);
            PriceDB = new PriceDatabase("PriceDB");
            AvraeParser = new AvraeSheetParser(gSheetsCredPath);
            TrackingSheetParser = new TrackingSheetParser(gSheetsCredPath);
            
            AndoraDB = new AndoraDatabase(credentials);
            System.Threading.Tasks.Task.Run(async () => await AndoraDB.RefreshLoginCredentials());

            CharacterDB = new CharacterDatabase(client, gSheetsCredPath, AndoraDB);

            PostReportParser = new PostReportParser(gSheetsCredPath, client, this);
            //PostReportParser.PollPlayerActivity();

            //Import elevated roles
            ElevatedStatusRoles = new List<ulong>();
            var roleData = DataStorage.FileReader.ReadCSV("ElevatedRoleData");
            foreach(var line in roleData)
            {
                foreach (var role in line.Value)
                {
                    if(ulong.TryParse(role, out var result))
                    {
                        ElevatedStatusRoles.Add(result);
                    }
                }
            }

            //Build the stored Tools value dictionary;
            BuildToolValuesDictionary();
        }

        public void BuildToolValuesDictionary()
        {
            DTDToolValues = new Dictionary<string, int>();

            var fileData = FileReader.ReadCSV("DTDToolValues");
            foreach (var kvp in fileData)
            {
                DTDToolValues.Add(kvp.Key, int.Parse(kvp.Value[0]));
            }

            var tempStr = "";
            foreach (var kvp in DTDToolValues)
            {
                tempStr += $"{kvp.Key},{kvp.Value},\n";
            }


            //FileReader.WriteCSV("DTDToolValues", tempStr);
        }

        /// <summary>
        /// Update each member's specific events as requested via a weekly refresh.
        /// </summary>
        internal void OnWeeklyRefresh()
        {
            Console.WriteLine("Updating Andora Database expired credentials...");
            var task = Task.Run(async () => await AndoraDB.RefreshLoginCredentials());
            task.Wait();

            Console.WriteLine("Resetting DTD for players in Andora Database...");
            task = Task.Run(async () => await AndoraDB.Post_ResetDTD());
            task.Wait();
        }

        #region Slash Commands

        internal async Task HandleVerifyCommand(SocketSlashCommand command)
        {
            try
            {
                bool hasElevatedRole = false;
                foreach (var role in ((SocketGuildUser)command.User).Roles)
                {
                    if (ElevatedStatusRoles.Contains(role.Id))
                    {
                        hasElevatedRole = true;
                        break;
                    }
                }

                if(hasElevatedRole == false)
                {
                    await command.RespondAsync("You do not have permissions to use this command.");
                    return;
                }

                await command.DeferAsync(true);

                var message_id = ((string)command.Data.Options.First(p => p.Name.Equals("original-message")).Value).Replace("https://discord.com/channels/", "").Split("/").Last();
                var message = await command.Channel.GetMessageAsync(ulong.Parse(message_id));
                var characterData = await CharacterDB.GetCharacterData(message.Author.Id);
                var characterSheetURL = characterData.AvraeURL;
                if (characterSheetURL == null)
                {
                    //Couldn't find character sheet.
                    throw new Exception("Failed to poll AvraeURL from Character Data");
                }

                var toolProficiencies = AvraeParser.GetToolProficiencies(characterSheetURL);
                var currency = AvraeParser.GetCurrency(characterSheetURL);

                var embedBuilder = new EmbedBuilder()
                    .WithTitle("Verification")
                    .WithDescription($"Relevant data about {characterData.CharacterName}")
                    .WithFields(new EmbedFieldBuilder() { Name = "Total Gold", Value = $"{currency.Replace("g", " Gold, ").Replace("s", " Silver, ").Replace("c", " Copper")}", IsInline = false },
                        new EmbedFieldBuilder() { Name = "Tool Proficiencies", Value = $"{toolProficiencies}", IsInline = false });

                await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
                //var itemList = ((string)command.Data.Options.First(p => p.Name.Equals("original-message")).Value).Split(',');
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to execute verify command : {e.Message}");
            }
        }

        public async Task GetCharacterSheetCommand(SocketSlashCommand command)
        {
            try
            {
                var user = (SocketGuildUser)command.Data.Options.First().Value;
                var forceObj = command.Data.Options.Where(p => p.Name.Equals("force")).FirstOrDefault();
                bool force = false;
                if(forceObj != default(SocketSlashCommandDataOption))
                {
                    force = (bool)forceObj.Value;
                }

                bool userIsSelf = command.User.Id.Equals(user.Id);

                bool hasElevatedRole = false;
                foreach (var role in ((SocketGuildUser)command.User).Roles)
                {
                    if (ElevatedStatusRoles.Contains(role.Id))
                    {
                        hasElevatedRole = true;
                        break;
                    }
                }

                if (hasElevatedRole == false && userIsSelf == false)
                {
                    await command.RespondAsync("You do not have permissions to use this command.");
                    return;
                }

                var characterData = await CharacterDB.GetCharacterData(user.Id, force);

                var embedBuilder = new EmbedBuilder()
                        .WithTitle($"{(user.Nickname != null ? user.Nickname : user.Username + "#" + user.Discriminator)}'s Character Sheet")
                        .WithDescription("[Character Sheet Link](https://docs.google.com/spreadsheets/d/" + characterData.AvraeURL + ")");

                await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get Character Sheet : {e.Message}");
            }
        }

        public async Task UpdateDBCharacterCommand(SocketSlashCommand command)
        {
            try
            {
                var user = (SocketGuildUser)command.Data.Options.First().Value;

                bool userIsSelf = command.User.Id.Equals(user.Id);

                bool hasElevatedRole = false;
                foreach (var role in ((SocketGuildUser)command.User).Roles)
                {
                    if (ElevatedStatusRoles.Contains(role.Id))
                    {
                        hasElevatedRole = true;
                        break;
                    }
                }

                if (hasElevatedRole == false && userIsSelf == false)
                {
                    await command.RespondAsync("You do not have permissions to use this command.");
                    return;
                }

                await command.DeferAsync(true);

                var characterData = await CharacterDB.GetCharacterData(user.Id, true); //We want to poll data from the Player GSheet and push to the DB.

                var sessionData = PostReportParser.GetSessions_FromGameReports(user.Username + "#" + user.Discriminator);
                var eventData = PostReportParser.GetEventParticipation_FromEventReports(user.Username + "#" + user.Discriminator);

                characterData.Total_Event_Part = eventData.Item1;
                characterData.Total_Sessions_Played = sessionData.Item1;

                var totalXP = sessionData.Item2 + eventData.Item2;
                bool xpMatch = characterData.Experience == totalXP + 1;

                var patchSuccessful = await AndoraDB.Patch_UpdateCharacter(characterData);

                var embedBuilder = new EmbedBuilder()
                        .WithTitle($"Update Character : {(user.Nickname != null ? user.Nickname : user.Username + "#" + user.Discriminator)}")
                        .WithDescription((patchSuccessful ? "Updated Successfully!" : "Error updating character!") + "\n" + (xpMatch ? "Experience Correct" : $"Experience Mismatch - Listed <{characterData.Experience}> Calculated <{totalXP+1}>"))
                        .WithColor(patchSuccessful ? Color.Green : Color.Red);

                await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: (hasElevatedRole || (userIsSelf == false)) );
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to update Character Sheet : {e.Message}");
            }
        }

        public async Task GetCharacterDTDCommand(SocketSlashCommand command)
        {
            try
            {
                var user = (SocketGuildUser)command.Data.Options.First().Value;

                //bool userIsSelf = command.User.Id.Equals(user.Id);

                bool hasElevatedRole = false;
                foreach (var role in ((SocketGuildUser)command.User).Roles)
                {
                    if (ElevatedStatusRoles.Contains(role.Id))
                    {
                        hasElevatedRole = true;
                        break;
                    }
                }

                if (hasElevatedRole == false)
                {
                    await command.RespondAsync("You do not have permissions to use this command.");
                    return;
                }

                await command.DeferAsync(true);

                var patchData = await AndoraDB.Get_DTD(user.Id);

                var embedBuilder = new EmbedBuilder()
                        .WithTitle($"DTDs remaining for {(user.Nickname != null ? user.Nickname : user.Username + "#" + user.Discriminator)}");
                if (patchData != null)
                {
                    embedBuilder.AddField(new EmbedFieldBuilder() { Name = "DTDs", Value = patchData.dtds });
                }
                else
                {
                    embedBuilder.WithDescription("Error: Failed to poll remaining DTD!");
                }

                await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to poll remaining DTD : {e.Message}");
            }
        }

        public async Task UpdateCharacterDTDCommand(SocketSlashCommand command)
        {
            try
            {
                var user = (SocketGuildUser)command.Data.Options.First().Value;
                var amount = ((int)command.Data.Options.First(p => p.Name.Equals("dtd-amount")).Value);

                //bool userIsSelf = command.User.Id.Equals(user.Id);

                bool hasElevatedRole = false;
                foreach (var role in ((SocketGuildUser)command.User).Roles)
                {
                    if (ElevatedStatusRoles.Contains(role.Id))
                    {
                        hasElevatedRole = true;
                        break;
                    }
                }

                if (hasElevatedRole == false)
                {
                    await command.RespondAsync("You do not have permissions to use this command.");
                    return;
                }

                await command.DeferAsync(true);

                var patchData = await AndoraDB.Patch_DTD(user.Id, amount);

                var embedBuilder = new EmbedBuilder()
                        .WithTitle($"Updating DTD remaining for {(user.Nickname != null ? user.Nickname : user.Username + "#" + user.Discriminator)}")
                        .WithDescription(patchData != null ? "Updated Successfully!" : "Error updating DTD!");

                if(patchData != null)
                {
                    embedBuilder.AddField(new EmbedFieldBuilder() { Name = "DTDs Remaining", Value = patchData.dtds });
                }

                await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to update Character DTD : {e.Message}");
            }
        }

        public async Task GetAppearanceCommand(SocketSlashCommand command)
        {
            try
            {
                var user = (SocketGuildUser)command.Data.Options.First().Value;

                bool hasElevatedRole = false;
                foreach (var role in ((SocketGuildUser)command.User).Roles)
                {
                    if (ElevatedStatusRoles.Contains(role.Id))
                    {
                        hasElevatedRole = true;
                        break;
                    }
                }

                await command.DeferAsync(true);// "Gathering character appearance data...");

                var characterData = await CharacterDB.GetCharacterData(user.Id);
                var characterSheetURL = characterData.AvraeURL;
                if (characterSheetURL == null)
                {
                    //Couldn't find character sheet.
                    throw new Exception("Failed to poll AvraeURL from Character Data");
                }

                var appearance = AvraeParser.GetCharacterAppearence(characterSheetURL);

                var embedBuilder = new EmbedBuilder()
                        .WithTitle($"{(user.Nickname != null ? user.Nickname : user.Username + "#" + user.Discriminator)}'s Appearance")
                        .WithFields(new EmbedFieldBuilder() { Name = "Age", Value = appearance.Age, IsInline = true }, 
                        new EmbedFieldBuilder() { Name = "Height", Value = appearance.Height, IsInline = true },
                        new EmbedFieldBuilder() { Name = "Weight", Value = appearance.Weight, IsInline = true },
                        new EmbedFieldBuilder() { Name = "Size", Value = appearance.Size, IsInline = true },
                        new EmbedFieldBuilder() { Name = "Gender", Value = appearance.Gender, IsInline = true }, 
                        new EmbedFieldBuilder() { Name = "Eyes", Value = appearance.Eyes, IsInline = true },
                        new EmbedFieldBuilder() { Name = "Hair", Value = appearance.Hair, IsInline = true },
                        new EmbedFieldBuilder() { Name = "Skin", Value = appearance.Skin, IsInline = true }
                        );
                //new MessageProperties() { Embed = embedBuilder.Build() };
                await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to poll character appearance data : {e.Message}");
            }
        }
        
        internal async Task PriorityReset_PBP(SocketSlashCommand command)
        {
            await command.DeferAsync(false);
            DateTime newDate = new DateTime(2022, 1, 1);

            var datestring = (string)command.Data.Options.FirstOrDefault(p => p.Name.Equals("end-date")).Value;

            if (!DateTime.TryParse(datestring, out newDate))
            {
                await command.FollowupAsync("Failed to parse date string, please try again!");
                return;
            }

            try
            {
                
                var userRead1 = command.Data.Options.FirstOrDefault(p => p.Name.Equals("player-one"));
                var userRead2 = command.Data.Options.FirstOrDefault(p => p.Name.Equals("player-two"));
                var userRead3 = command.Data.Options.FirstOrDefault(p => p.Name.Equals("player-three"));
                var userRead4 = command.Data.Options.FirstOrDefault(p => p.Name.Equals("player-four"));
                var userRead5 = command.Data.Options.FirstOrDefault(p => p.Name.Equals("player-five"));
                var userRead6 = command.Data.Options.FirstOrDefault(p => p.Name.Equals("player-six"));

                var user1 = (SocketGuildUser)(userRead1 == null ? null : userRead1.Value);
                var user2 = (SocketGuildUser)(userRead2 == null ? null : userRead2.Value);
                var user3 = (SocketGuildUser)(userRead3 == null ? null : userRead3.Value);
                var user4 = (SocketGuildUser)(userRead4 == null ? null : userRead4.Value);
                var user5 = (SocketGuildUser)(userRead5 == null ? null : userRead5.Value);
                var user6 = (SocketGuildUser)(userRead6 == null ? null : userRead6.Value);

                //var user = (SocketGuildUser)command.Data.Options.First().Value;

                bool hasElevatedRole = false;
                foreach (var role in ((SocketGuildUser)command.User).Roles)
                {
                    if (ElevatedStatusRoles.Contains(role.Id))
                    {
                        hasElevatedRole = true;
                        break;
                    }
                }

                if (hasElevatedRole == false)
                {
                    await command.FollowupAsync("You do not have permissions to use this command.");
                    return;
                }

                //var characterData = await CharacterDB.GetCharacterData(user.Id);

                var output = await this.PostReportParser.UpdatePriority(newDate, user1, user2, user3, user4, user5, user6);

                //Output file for testing purposes.
                if (output.Length > 1800)
                {
                    var dumpFilePath = System.IO.Directory.GetCurrentDirectory() + $"/Data/{command.User.Id}_DumpLog_" + DateTime.Now.ToShortTimeString().Replace(':', '-').Replace('_', '-') + ".txt";
                    System.IO.File.WriteAllText(dumpFilePath, output);
                    await command.FollowupWithFileAsync(dumpFilePath, $"{command.User.Mention}", ephemeral:false);
                    System.IO.File.Delete(dumpFilePath);
                }
                else
                {
                    var embedBuilder = new EmbedBuilder()
                        .WithAuthor($"{(command.User.Username + "#" + command.User.Discriminator)}", command.User.GetAvatarUrl())
                        .WithDescription(output);
                    await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to apply priority change : {e.Message}");
                await command.FollowupAsync("Failed to apply priority change. See log for details.");
            }
        }

        #endregion
    }
}
