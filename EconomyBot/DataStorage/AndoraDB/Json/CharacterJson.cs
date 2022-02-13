using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage.AndoraDB.Json
{
    public class CharacterDataJson
    {
        /// <summary>
        /// Discord id of user. Should be a ulong value stored as a string.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Character's name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Character's race
        /// </summary>
        public string race { get; set; }

        /// <summary>
        /// Character's faction
        /// </summary>
        public string faction { get; set; }

        /// <summary>
        /// Character's class.
        /// </summary>
        public string @class { get; set; }

        /// <summary>
        /// Character's region
        /// </summary>
        public string region { get; set; }
        
        /// <summary>
        /// Character's current level
        /// </summary>
        public int level { get; set; }

        /// <summary>
        /// Character's experience count
        /// </summary>
        public int exp { get; set; }
        
        /// <summary>
        /// Datetime of last play. Stored in the format of a short date string (IE: "06-22-2021")
        /// </summary>
        public string last_played { get; set; }

        /// <summary>
        /// Google Sheets URL for the character's sheet. This should be stored as a Google API stub.
        /// </summary>
        public string sheet { get; set; }

        public static implicit operator CharacterDataJson(CharacterData data)
        {
            var retVal = new CharacterDataJson();

            retVal.id = data.DiscordID.ToString();
            retVal.name = data.CharacterName;
            retVal.race = data.Race;
            retVal.faction = data.Faction;
            retVal.@class = data.Class;
            retVal.region = data.Region;
            retVal.level = data.Level;
            retVal.exp = data.Experience;
            retVal.last_played = $"{data.LastPlayed.Month.ToString("00")}-{data.LastPlayed.Day.ToString("00")}-{data.LastPlayed.Year}"; //Dumb, but whatever. Backend too picky.
            retVal.sheet = data.AvraeURL;

            return retVal;
        }
    }
}
