using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
            var gSheetsCredPath = "Data\\andora-3db990b2eff4.json";

            NPCPingService = new NPCPingService(client);
            PriceDB = new PriceDatabase("PriceDB");
            AvraeParser = new AvraeSheetParser(gSheetsCredPath);
            
            AndoraDB = new AndoraDatabase(credentials);
            System.Threading.Tasks.Task.Run(async () => await AndoraDB.RefreshLoginCredentials());

            CharacterDB = new CharacterDatabase(client, gSheetsCredPath, AndoraDB);

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
    }
}
