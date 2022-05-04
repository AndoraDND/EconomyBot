using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage
{
    /// <summary>
    /// Data storage for characters polled from the database.
    /// </summary>
    public struct CharacterData
    {
        /// <summary>
        /// Database specific ID.
        /// Not yet used but will be used in the future
        /// </summary>
        public int ID;

        /// <summary>
        /// Discord internal ID
        /// </summary>
        public ulong DiscordID;

        /// <summary>
        /// Discord Name and Descriminator
        /// </summary>
        public string DiscordName;
        
        /// <summary>
        /// Andora character name
        /// </summary>
        public string CharacterName;

        /// <summary>
        /// Andora character race
        /// </summary>
        public string Race;

        /// <summary>
        /// Andora player faction association
        /// </summary>
        public string Faction;

        /// <summary>
        /// Andora character class
        /// </summary>
        public string Class;

        /// <summary>
        /// Andora world region
        /// </summary>
        public string Region;

        /// <summary>
        /// Andora character level
        /// </summary>
        public int Level;

        /// <summary>
        /// Andora character experience count
        /// </summary>
        public int Experience;

        /// <summary>
        /// Date of last session
        /// </summary>
        public DateTime LastPlayed;

        /// <summary>
        /// Downtime days remaining
        /// </summary>
        public int DTD;

        /// <summary>
        /// Date of last earned experience
        /// </summary>
        public DateTime Last_Exp_Earned_Date;

        public DateTime Last_Event_Exp_Date;

        public DateTime Last_Rumor_Part_Date;

        public DateTime Last_Event_Part_Date;

        public int Total_Sessions_Played;

        public int Total_Event_Part;

        public int Total_Rumor_Part;

        public int Total_NPC_Pings;

        /// <summary>
        /// URL stub for Avrae-based character sheet
        /// </summary>
        public string AvraeURL;
        
        public CharacterData(EconomyBot.DataStorage.AndoraDB.Json.CharacterDataJson json)
        {
            ID = -1;
            DiscordID = ulong.Parse(json.id);
            DiscordName = "";
            CharacterName = json.name;
            Race = json.race;
            Faction = json.faction;
            Class = json.@class;
            Region = json.region;
            Level = json.level;
            Experience = json.exp;
            LastPlayed = DateTime.Parse(json.last_played);
            DTD = json.dtds;
            //Parse Dates. Avoiding error where the database is storing null values. Only parse on success.
            if (DateTime.TryParse(json.last_exp_earned_date.Length > 0 ? json.last_exp_earned_date : json.last_played, out var dt))
            {
                Last_Exp_Earned_Date = dt;
            }
            else
            {
                Last_Exp_Earned_Date = DateTime.MinValue;
            }

            if(DateTime.TryParse(json.last_event_exp_date, out dt))
                Last_Event_Exp_Date = dt;
            else
                Last_Event_Exp_Date = DateTime.MinValue;

            if (DateTime.TryParse(json.last_rumor_part_date, out dt))
                Last_Rumor_Part_Date = dt;
            else
                Last_Rumor_Part_Date = DateTime.MinValue;

            if (DateTime.TryParse(json.last_event_part_date, out dt))
                Last_Event_Part_Date = dt;
            else
                Last_Event_Part_Date = DateTime.MinValue;

            Total_Sessions_Played = json.total_sessions_played;
            Total_Event_Part = json.total_event_part;
            Total_Rumor_Part = json.total_rumor_part;
            Total_NPC_Pings = json.total_npc_pings; 
            AvraeURL = json.sheet;
        }

        /// <summary>
        /// Return a string with relevant data about the character
        /// </summary>
        /// <param name="indent"></param>
        /// <returns></returns>
        internal string Print(int indent = 4)
        {
            var indentSpace = "";
            for(int i = 0;i < indent; i++)
            {
                indentSpace += " ";
            }

            var retVal = "";
            retVal = $"{indentSpace}ID: {ID},\n" +
                $"{indentSpace}DiscordID: {DiscordID},\n" +
                $"{indentSpace}DiscordName: {DiscordName},\n" +
                $"{indentSpace}CharacterName: {CharacterName},\n" +
                $"{indentSpace}Race: {Race},\n" +
                $"{indentSpace}Faction: {Faction},\n" +
                $"{indentSpace}Class: {Class},\n" +
                $"{indentSpace}Region: {Region},\n" +
                $"{indentSpace}Level: {Level},\n" +
                $"{indentSpace}Experience: {Experience},\n" +
                $"{indentSpace}LastPlayed: {LastPlayed},\n" +
                $"{indentSpace}DTDs: {DTD},\n" +
                $"{indentSpace}Last_Exp: {Last_Exp_Earned_Date},\n" +
                $"{indentSpace}AvraeURL: {AvraeURL}";
            return retVal;
        }
    }
}
