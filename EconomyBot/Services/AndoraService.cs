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
            task = Task.Run(async () => await AndoraDB.Patch_ResetDTD());
            task.Wait();
        }

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

                var message_id = ((string)command.Data.Options.First(p => p.Name.Equals("original-message")).Value).Replace("https://discord.com/channels/", "").Split("/").Last();
                var message = await command.Channel.GetMessageAsync(ulong.Parse(message_id));
                var characterData = await CharacterDB.GetCharacterData(message.Author.Id);
                var characterSheetURL = characterData.AvraeURL;
                if (characterSheetURL == null)
                {
                    //Couldn't find character sheet.
                }
                var toolProficiencies = AvraeParser.GetToolProficiencies(characterSheetURL);
                var currency = AvraeParser.GetCurrency(characterSheetURL);

                var embedBuilder = new EmbedBuilder()
                    .WithTitle("Verification")
                    .WithDescription($"Relevant data about {characterData.CharacterName}")
                    .WithFields(new EmbedFieldBuilder() { Name = "Total Gold", Value = $"{currency.Replace("g", " Gold, ").Replace("s", " Silver, ").Replace("c", " Copper")}", IsInline = false },
                        new EmbedFieldBuilder() { Name = "Tool Proficiencies", Value = $"{toolProficiencies}", IsInline = false });

                await command.RespondAsync(embed: embedBuilder.Build(), ephemeral:true);
                //var itemList = ((string)command.Data.Options.First(p => p.Name.Equals("original-message")).Value).Split(',');
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to execute verify command.");
            }
        }

        public async Task GetCharacterSheetCommand(SocketSlashCommand command)
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

            var characterData = await CharacterDB.GetCharacterData(user.Id);

            var embedBuilder = new EmbedBuilder()
                    .WithTitle($"{(user.Nickname!=null?user.Nickname:user.Username + "#" + user.Discriminator)}'s Character Sheet")
                    .WithDescription("[Character Sheet Link](https://docs.google.com/spreadsheets/d/" + characterData.AvraeURL+")");

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral:true);
        }
    }
}
