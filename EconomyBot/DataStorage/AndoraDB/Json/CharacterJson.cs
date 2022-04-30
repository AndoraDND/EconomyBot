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

        public int dtds { get; set; }

        /// <summary>
        /// Datetime of last earned experience. Stored in the format of a short date string (IE: "06-22-2021")
        /// </summary>
        public string last_exp_earned_date { get; set; }

        public string last_event_part_date { get; set; }

        public string last_event_exp_date { get; set; }

        public string last_rumor_part_date { get; set; }

        public int total_sessions_played { get; set; }

        public int total_event_part { get; set; }

        public int total_rumor_part { get; set; }

        public int total_npc_pings { get; set; }

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
            retVal.last_played = DateTime_ToDBString(data.LastPlayed);//$"{data.LastPlayed.Month.ToString("00")}-{data.LastPlayed.Day.ToString("00")}-{data.LastPlayed.Year}"; //Dumb, but whatever. Backend too picky.
            retVal.last_exp_earned_date = DateTime_ToDBString(data.Last_Exp_Earned_Date);//$"{data.Last_Exp_Earned_Date.Month.ToString("00")}-{data.Last_Exp_Earned_Date.Day.ToString("00")}-{data.Last_Exp_Earned_Date.Year}";
            retVal.dtds = data.DTD;

            retVal.last_event_exp_date = DateTime_ToDBString(data.Last_Event_Exp_Date);
            retVal.last_event_part_date = DateTime_ToDBString(data.Last_Event_Part_Date);
            retVal.last_rumor_part_date = DateTime_ToDBString(data.Last_Rumor_Part_Date);
            retVal.total_sessions_played = data.Total_Sessions_Played;
            retVal.total_event_part = data.Total_Event_Part;
            retVal.total_rumor_part = data.Total_Rumor_Part;
            retVal.total_npc_pings = data.Total_NPC_Pings;
            retVal.sheet = data.AvraeURL;

            return retVal;
        }

        private static string DateTime_ToDBString(DateTime dt)
        {
            return $"{dt.Month.ToString("00")}-{dt.Day.ToString("00")}-{dt.Year.ToString("0000")}";
        }

        public bool IsSameCharacter(CharacterDataJson other)
        {
            return other.id.Equals(id);
        }


        public override bool Equals(object obj)
        {
            if(obj is CharacterDataJson)
            {
                var other = obj as CharacterDataJson;
                if (!other.id.Equals(id)) return false;
                if (!other.dtds.Equals(dtds)) return false;
                if (!other.exp.Equals(exp)) return false;
                if (!other.level.Equals(level)) return false;
                if (!other.@class.Equals(@class)) return false;
                if (!other.name.Equals(name)) return false;
                if (!other.faction.Equals(faction)) return false;
                if (!other.race.Equals(race)) return false;
                if (!other.region.Equals(region)) return false;
                if (!other.last_played.Equals(last_played)) return false;
                if (!other.last_exp_earned_date.Equals(last_exp_earned_date)) return false;
                if (!other.sheet.Equals(sheet)) return false;

                return true;
            }
            return false;
        }
    }
}
