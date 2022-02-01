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
        /// URL stub for Avrae-based character sheet
        /// </summary>
        public string AvraeURL;
        
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
                $"{indentSpace}AvraeURL: {AvraeURL}";
            return retVal;
        }
    }
}
