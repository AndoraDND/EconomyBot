using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace EconomyBot.DataStorage
{
    public class CharacterDatabase
    {
        //Database storage:
        //Table: character_data
        //rowID(Primary_Key), discord_id, discord_name, character_name, avrae_url

        //Table: downtime_days
        //rowID(Remote_Key), downtime_days

        private static readonly string _dbPath = Directory.GetCurrentDirectory() + @"\Data\Characters.db";
        private static readonly string _connectionString = @"Data Source=file:" + _dbPath;
        //private string stm = "SELECT SQLITE_VERSION()";

        private Dictionary<string, ulong> _CachedNameLookup;

        public CharacterDatabase()
        {
            if(!File.Exists(_dbPath))
            {
                var newFile = File.Create(_dbPath);
                newFile.Close();
            }

            //BuildCharacterTable();
            //PollAllCharacters();
            PollCharacter(126538520193007616);
        }

        public bool BuildCharacterTable()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using var cmd = new SqliteCommand("", connection);

                    //Delete the existing table if not empty
                    cmd.CommandText = "DROP TABLE IF EXISTS character_data";
                    cmd.ExecuteNonQuery();

                    //Build the new table
                    cmd.CommandText = "CREATE TABLE character_data(id INTEGER PRIMARY KEY, discord_id INT, discord_name TEXT, character_name TEXT, avrae_url TEXT)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "INSERT INTO character_data(discord_id, discord_name, character_name, avrae_url) VALUES(126538520193007616, 'Bribzy#6715', 'Ella Vernasch(Noelle)', '1SasaqMK9P2OuNrjvsrZHE0g_1eGpB6YqijpDebqhJts')";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to build character table : {0}\n{1}", e.Message, e.StackTrace);
                return false;
            }

            return true;
        }

        public void PollAllCharacters()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    using var cmd = new SqliteCommand("SELECT * FROM character_data", connection);
                    using var rdr = cmd.ExecuteReader();

                    Console.WriteLine("== Character Data Table ===");
                    Console.WriteLine($"{rdr.GetName(0)} - {rdr.GetName(1)}, {rdr.GetName(2)}, {rdr.GetName(3)}, {rdr.GetName(4)}");
                    while(rdr.Read())
                    {
                        Console.WriteLine($"{rdr.GetInt32(0)} - {rdr.GetInt64(1)}, {rdr.GetString(2)}, {rdr.GetString(3)}, {rdr.GetString(4)}");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to poll character table : {0}\n{1}", e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Poll a character from the database from a discord id.
        /// </summary>
        /// <param name="discordID">User's Discord id. This can be obtained by turning on developer mode and right clicking their name and "Copy ID"</param>
        /// <returns></returns>
        public List<CharacterData> PollCharacter(ulong discordID)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    using var cmd = new SqliteCommand("SELECT * FROM character_data WHERE discord_id = @discordid", connection);
                    cmd.Parameters.Add(new SqliteParameter("@discordid", discordID));
                    using var rdr = cmd.ExecuteReader();

                    var retVal = new List<CharacterData>();
                    while(rdr.Read())
                    {
                        retVal.Add(new CharacterData() 
                        { 
                            DiscordID = (ulong)rdr.GetInt64(1),
                            DiscordName = rdr.GetString(2),
                            CharacterName = rdr.GetString(3),
                            AvraeURL = rdr.GetString(4)
                        });
                    }

                    /*
                    Console.WriteLine("== Character Data Table ===");
                    Console.WriteLine($"{rdr.GetName(0)} - {rdr.GetName(1)}, {rdr.GetName(2)}, {rdr.GetName(3)}, {rdr.GetName(4)}");
                    while (rdr.Read())
                    {
                        Console.WriteLine($"{rdr.GetInt32(0)} - {rdr.GetInt64(1)}, {rdr.GetString(2)}, {rdr.GetString(3)}, {rdr.GetString(4)}");
                    }
                    */

                    return retVal;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to poll character table : {0}\n{1}", e.Message, e.StackTrace);
            }

            return null;
        }

        /// <summary>
        /// Poll a character from the database from a discord name and descriminator.
        /// </summary>
        /// <param name="discordName">User's Discord name and Descriminator. IE: Name#1234</param>
        /// <returns></returns>
        public List<CharacterData> PollCharacter(string discordName)
        {
            if(!discordName.Contains("#"))
            {
                Console.WriteLine("Error polling character data: Missing descriminator!");
                return null;
            }

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    using var cmd = new SqliteCommand("SELECT * FROM character_data WHERE discord_name = @discordname", connection);
                    cmd.Parameters.Add(new SqliteParameter("@discordname", discordName));
                    using var rdr = cmd.ExecuteReader();

                    var retVal = new List<CharacterData>();
                    while (rdr.Read())
                    {
                        retVal.Add(new CharacterData()
                        {
                            DiscordID = (ulong)rdr.GetInt64(1),
                            DiscordName = rdr.GetString(2),
                            CharacterName = rdr.GetString(3),
                            AvraeURL = rdr.GetString(4)
                        });
                    }

                    /*
                    Console.WriteLine("== Character Data Table ===");
                    Console.WriteLine($"{rdr.GetName(0)} - {rdr.GetName(1)}, {rdr.GetName(2)}, {rdr.GetName(3)}, {rdr.GetName(4)}");
                    while (rdr.Read())
                    {
                        Console.WriteLine($"{rdr.GetInt32(0)} - {rdr.GetInt64(1)}, {rdr.GetString(2)}, {rdr.GetString(3)}, {rdr.GetString(4)}");
                    }
                    */

                    return retVal;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to poll character table : {0}\n{1}", e.Message, e.StackTrace);
            }

            return null;
        }
    }
}
