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
            Last_Exp_Earned_Date = DateTime.Parse((json.last_exp_earned_date.Length > 0 ? json.last_exp_earned_date:json.last_played));
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
