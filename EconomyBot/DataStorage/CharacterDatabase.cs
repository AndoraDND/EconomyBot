using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Data.Sqlite;

namespace EconomyBot.DataStorage
{
    public class CharacterDatabase
    {
        private Discord.WebSocket.DiscordSocketClient _discordClient;

        private List<CharacterData> _cachedCharacterData;

        /// <summary>
        /// Google Sheets runtime service
        /// </summary>
        private SheetsService _service;

        /// <summary>
        /// Google Sheets service allowed scopes
        /// </summary>
        private static string[] Scopes = { SheetsService.Scope.Spreadsheets };

        /// <summary>
        /// Application name used for polling from Google
        /// </summary>
        private static string ApplicationName = "Andora GSheets Parser";
        
        /// <summary>
        /// Backend service to call upon primarily before checking google sheets
        /// </summary>
        public AndoraDatabase _andoraDB = null;

        public CharacterDatabase(Discord.WebSocket.DiscordSocketClient client, string googleCredentialsPath, AndoraDatabase backend = null)
        {
            _cachedCharacterData = new List<CharacterData>();
            _discordClient = client;
            _andoraDB = backend;

            //Create google sheets lookup, for lookups related to our google sheet character database
            //Load Google Sheets credentials
            var credential = GoogleCredential.FromFile(googleCredentialsPath).CreateScoped(Scopes);
            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        /// <summary>
        /// Get character data from this database.
        /// </summary>
        /// <param name="discordID"></param>
        /// <returns></returns>
        public async Task<CharacterData> GetCharacterData(ulong discordID, bool forceGSheet = false)
        {
            var characterDataIndex = _cachedCharacterData.FindIndex(p => p.DiscordID.Equals(discordID));
            if(characterDataIndex >= 0)
            {
                if (_cachedCharacterData[characterDataIndex].Last_Polled.AddDays(14) > DateTime.UtcNow)
                {
                    return _cachedCharacterData[characterDataIndex];
                }
            }

            //Didn't find character data within cached list
            return await PollCharacterData(discordID, forceGSheet);
        }

        /// <summary>
        /// Poll the character data necessary from a relevant service.
        /// TODO: Implement management for handling our RESTful service.
        /// </summary>
        /// <param name="discordID"></param>
        /// <returns></returns>
        private async Task<CharacterData> PollCharacterData(ulong discordID, bool forceGSheet = false)
        {
            //Get the expected Discord user, find their name. This will be used for parsing our google sheet
            var discordUser = await _discordClient.GetUserAsync(discordID);
            var discordName = $"{discordUser.Username}#{discordUser.Discriminator}";

            bool backend_success = false;
            if(_andoraDB != null && forceGSheet == false)
            {
                var characterJson = await _andoraDB.Get_FindCharacterData(discordID);

                if (characterJson != null)
                {
                    backend_success = true;

                    var characterData = new CharacterData(characterJson);
                    characterData.DiscordName = discordName;

                    return characterData;
                }
            }
            
            if(!backend_success)
            {
                var sheetURLStub = "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU";
                var range = "'Player character sheet'!A2:Q";
                try
                {
                    SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(sheetURLStub, range);
                    ValueRange response = await request.ExecuteAsync();

                    var values = response.Values.Where(p => p.Count > 0 && ((string)p[1]).Contains(discordUser.Username));
                    
                    if (values.Count() > 0)
                    {
                        var sData = values.Last();

                        if (!int.TryParse((string)sData[11], out var exp)) exp = 1;
                        if (!int.TryParse((string)sData[12], out var level)) level = 1;
                        if (!DateTime.TryParse((string)sData[15], out var lastplayed))
                        {
                            if (!DateTime.TryParse((string)sData[0], out lastplayed))
                            {
                                Console.WriteLine("Error encountered while assigning last played datetime for character. Aborting poll...");
                                return default(CharacterData);
                            }
                        }

                        var characterData = new CharacterData()
                        {
                            DiscordID = discordID,
                            DiscordName = (string)sData[1],
                            CharacterName = (string)sData[2],
                            AvraeURL = ((string)sData[3]).Replace("https://docs.google.com/spreadsheets/d/", "").Split('/')[0],
                            Region = ((string)sData[6]),
                            Faction = ((string)sData[9]),
                            Race = ((string)sData[10]),
                            Experience = exp,
                            Level = level,
                            Class = ((string)sData[13]),
                            LastPlayed = lastplayed,
                            DTD = 0,
                            Last_Exp_Earned_Date = lastplayed,
                            Last_Event_Exp_Date = DateTime.MinValue,
                            Last_Event_Part_Date = DateTime.MinValue,
                            Last_Rumor_Part_Date = DateTime.MinValue,
                            Total_Sessions_Played = 0,
                            Total_Event_Part = 0,
                            Total_NPC_Pings = 0,
                            Total_Rumor_Part = 0
                        };

                        _cachedCharacterData.Add(characterData);

                        await _andoraDB.Post_AddCharacter(characterData);

                        return characterData;
                    }
                    else
                    {
                        throw new Exception($"Attempted and failed to poll character from googl sheets: \nName : < {discordUser.Username} > \nID : < {discordUser.Id} >");
                    }
                    return default(CharacterData);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to poll Database Sheet: {0}", e.Message);
                    return default(CharacterData);
                }
            }

            return default(CharacterData);
        }

        /// <summary>
        /// Get all data currently within this database.
        /// </summary>
        /// <returns></returns>
        internal string Dump()
        {
            var output = "";
            foreach(var character in _cachedCharacterData)
            {
                output += character.Print() + "\n\n";
            }
            return output;
        }
    }

    #region OLD
#if false
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
        
        /// <summary>
        /// Select *, profit
        /// </summary>
        public void PrintDatabase()
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
#endif
#endregion
}
