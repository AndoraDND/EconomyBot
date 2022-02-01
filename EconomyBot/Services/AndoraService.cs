using System;
using System.Collections.Generic;
using System.Text;
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
        /// Lookup table for specific roles
        /// </summary>
        public List<ulong> ElevatedStatusRoles { get; set; }

        /// <summary>
        /// Lookup table for tool proficiencies and their DTD gold generation, in copper.
        /// </summary>
        public Dictionary<string, int> DTDToolValues { get; set; }

        public AndoraService(Discord.WebSocket.DiscordSocketClient client)
        {
            var gSheetsCredPath = "Data\\andora-3db990b2eff4.json";

            PriceDB = new PriceDatabase("PriceDB");
            AvraeParser = new AvraeSheetParser(gSheetsCredPath);
            CharacterDB = new CharacterDatabase(client, gSheetsCredPath);

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
    }
}
