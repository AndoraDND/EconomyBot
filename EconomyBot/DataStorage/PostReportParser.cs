using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;

namespace EconomyBot.DataStorage
{
    public class PostReportParser
    {
        private AndoraService _andoraService;

        private DiscordSocketClient _client;

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
        private static string ApplicationName = "Andora Tracker GSheets Parser";

        private static string SheetURLStub = "1vPl8oj_pdhVwu23ZYdqjA3rgayAWyJzvyAc9iMgF-UQ";

        //Range constants
        //private const string Range_TrainingTracking = "'Academy'!A2:E";

        public PostReportParser(string googleCredentialsPath, DiscordSocketClient client, AndoraService andoraService)
        {
            //Load Google Sheets credentials
            var credential = GoogleCredential.FromFile(googleCredentialsPath).CreateScoped(Scopes);
            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            _client = client;
            
            _andoraService = andoraService;
        }

        public async Task PollPlayerActivity(Discord.Commands.SocketCommandContext Context)
        {
            //await _client.DownloadUsersAsync(_client.Guilds); //yikes, testing only.

            /*
            var GameReportData = GetReportSheetDB(0, $"'Post Game Reports (PGR)'!A34:I", 34);
            //var EventReportData = GetReportSheetDB(1, $"'Post Event Reports (PER)'!A2:E", 2);
            //var RumorReportData = GetReportSheetDB(2, $"'Post Rumor Reports (PRR)'!A2:D", 2);

            var TrackedData = new Dictionary<string, Tuple<int, int, int>>();

            foreach(var report in GameReportData)
            {
                var characterList = new List<string>();
                for (int i = 3; i <= 8; i++)
                {
                    if (report.Count > i)
                    {
                        characterList.Add((string)report[i]);
                    }
                }

                foreach(var character in characterList)
                {
                    if(character.Contains('#'))
                    {
                        var discordName = character.Split("#")[0];
                        if (TrackedData.ContainsKey(discordName))
                        {
                            TrackedData[discordName] = new Tuple<int, int, int>(TrackedData[discordName].Item1 + 1,
                                TrackedData[discordName].Item2,
                                TrackedData[discordName].Item3);
                        }
                        else
                        {
                            TrackedData.Add(discordName, new Tuple<int, int, int>(1, 0, 0));
                        }
                    }
                }
            }

            Console.WriteLine($"Output values: \n");
            foreach (var character in TrackedData)
            {
                Console.WriteLine($"{character.Key} - < {character.Value.Item1} >");
            }
            */

            var GameReports = GetUnprocessedGameReports();
            var EventReports = GetUnprocessedEventReports();

            var TotalCalculatedRewards = new List<CombinedReward>();
            var ErrorHandlingPlayers = new List<Tuple<int, string, string>>();

            //await Context.Guild.DownloadUsersAsync();
            //await Context.Channel.SendMessageAsync(Context.Guild.Users.Count + "");

            //Handle Game Report
            foreach(var report in GameReports)
            {
                var rewardData = JsonConvert.DeserializeObject<List<JSONRewardData>>(report.JSON);
                foreach (var player in rewardData)
                {
                    try
                    {
                        string[] splitUser = player.playerName.Split('#');
                        //Console.WriteLine(splitUser[0] + " - " + splitUser[1]);

                        var discordUser = Context.Guild.Users.Where(p => p.Username.Equals(splitUser[0]) && p.Discriminator.Equals(splitUser[1])).FirstOrDefault();
                        //var discordUser = (await Context.Guild.SearchUsersAsync(player.playerName, 1)).First();//.GetUser(splitUser[0], splitUser[1]);
                        if (discordUser != default(SocketGuildUser))
                        {
                            var index = TotalCalculatedRewards.FindIndex(p => p.DiscordUser.Id.Equals(discordUser.Id));
                            if (index >= 0)
                            {
                                TotalCalculatedRewards[index].XPValue += player.playerExp;
                                if (TotalCalculatedRewards[index].LastPlayedDate < report.MissionRunDate)
                                {
                                    TotalCalculatedRewards[index].LastPlayedDate = report.MissionRunDate;
                                }
                            }
                            else
                            {
                                var newReward = new CombinedReward() { ReportID = report.RowID, DiscordUser = discordUser, LastPlayedDate = report.MissionRunDate, XPValue = player.playerExp };
                                TotalCalculatedRewards.Add(newReward);
                            }
                        }
                        else
                        {
                            ErrorHandlingPlayers.Add(new Tuple<int, string, string>(report.RowID, player.playerName, "Failed to find DiscordUser!"));
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorHandlingPlayers.Add(new Tuple<int, string, string>(report.RowID, player.playerName, e.Message));
                        continue;
                    }
                }
            }

            //Update Player Character Sheet with new Values.
            string range = "'Player character sheet'!A6:Q";
            string charDBSheetID = "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU";

            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(charDBSheetID, range);
            ValueRange response = await request.ExecuteAsync();
            var charSheetValues = response.Values;

            List<int> removeFlags = new List<int>();
            int i = 0;
            foreach (var reward in TotalCalculatedRewards)
            {
                //Update the player character sheet
                if(await UpdateCharacterSheetWithReward(charSheetValues, reward) == false)
                {
                    removeFlags.Add(i);
                    ErrorHandlingPlayers.Add(new Tuple<int, string, string>(reward.ReportID, reward.DiscordUser.Username+"#"+reward.DiscordUser.Discriminator, "Failed Updating PlayerCharacterSheet. Check Logs."));
                }
                i++;
            }

            //Remove unhandled rewards
            removeFlags.Reverse();
            foreach (var value in removeFlags)
            {
                TotalCalculatedRewards.RemoveAt(value);
            }

            //Display output to the calling user.
            var output = "RewardLog:\n";
            foreach (var player in TotalCalculatedRewards)
            {
                output += $"{player.DiscordUser} [{player.LastPlayedDate}]: - {player.XPValue}\n";
                //Console.WriteLine($"{player.DiscordUser} [{player.LastPlayedDate}]: - {player.XPValue}");
            }
            output += "\nErrorLog:\n";
            foreach(var player in ErrorHandlingPlayers)
            {
                output += $"[{player.Item1}] {player.Item2} - {player.Item3}\n";
                //Console.WriteLine($"Failed to handle Player in ReportID[{player.Item2}]: {player.Item1}");
            }

            //Output file for testing purposes.
            if (output.Length < 1800)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}\n" + output);
            }
            else
            {
                var dumpFilePath = Directory.GetCurrentDirectory() + $"/Data/{Context.User.Id}_DumpLog_" + DateTime.Now.ToShortTimeString().Replace(':', '-').Replace('_', '-') + ".txt";
                File.WriteAllText(dumpFilePath, output);
                await Context.Channel.SendFileAsync($"{Context.User.Mention}\n"+dumpFilePath);
                File.Delete(dumpFilePath);
            }

            Console.WriteLine("Finished processing reports");
        }

        public List<PostGameReport> GetUnprocessedGameReports()
        {
            var retVal = new List<PostGameReport>();

            var values = GetReportSheetDB(0, $"'Post Game Reports (PGR)'!A64:P", 64, false);
            int rowID = 64;
            foreach(var row in values)
            {
                var report = new PostGameReport(rowID);
                try
                {
                    DateTime.TryParse((string)row[0], out report.TimeStamp);
                    report.DMName = (string)row[1];
                    DateTime.TryParse((string)row[2], out report.MissionRunDate);
                    report.Players = new List<string>();
                    for (int i = 3; i <= 8; i++)
                    {
                        if (((string)row[i]) != null && ((string)row[i]).Length > 0)
                        {
                            report.Players.Add((string)row[i]);
                        }
                    }
                    report.ItemResults = (string)row[9];
                    report.StoryDevelopments = (string)row[10];
                    report.MainStoryRelated = ((string)row[11]).ToLower().Contains('y');
                    report.WeeklyRumorBoard = (string)row[12];
                    report.SessionSpecifics = (string)row[13];
                    report.JSON = (string)row[14];

                    retVal.Add(report);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Failed to process row <{rowID}> : {e.Message}");
                }

                //Increment row ID
                rowID++;
            }

            return retVal;
        }

        public List<PostEventReport> GetUnprocessedEventReports()
        {
            var retVal = new List<PostEventReport>();

            var values = GetReportSheetDB(0, $"'Post Event Reports (PER)'!A25:P", 25, false);
            int rowID = 25;
            foreach (var row in values)
            {
                var report = new PostEventReport(rowID);
                try
                {
                    if (row[15] != null && !((string)row[15]).Equals("TRUE")) //Make sure the row isn't already processed.
                    {
                        DateTime.TryParse((string)row[0], out report.TimeStamp);
                        report.MainRunner = (string)row[1];
                        report.OtherRunners = new List<string>();
                        var valueArr = ((string)row[2]).Split(',');
                        foreach (var value in valueArr)
                        {
                            report.OtherRunners.Add(value.TrimStart().TrimEnd());
                        }
                        report.EventData = (string)row[3];
                        report.Participants = new List<string>();
                        valueArr = ((string)row[4]).Replace("\n", "").Split(',');
                        foreach (var value in valueArr)
                        {
                            report.Participants.Add(value.TrimStart().TrimEnd());
                        }
                        int.TryParse((string)row[5], out report.XPAwarded);
                        report.ItemRewards = (string)row[6];
                        report.EventRewards = (string)row[7];
                        report.MajorStoryDevelopments = (string)row[8];
                        report.WeeklyRumorBoard = (string)row[9];
                        report.EventSpecifics = (string)row[10];

                        retVal.Add(report);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to process row <{rowID}> : {e.Message}");
                }

                //Increment row ID
                rowID++;
            }

            return retVal;
        }

        public async Task<bool> UpdateCharacterSheetWithReward(IList<IList<object>> valueList, CombinedReward reward)
        {
            if (valueList == null && valueList.Count <= 0)
            {
                Console.WriteLine("Error updating character sheet : Value list is null or empty!");
                return false;
            }

            /*
            Console.WriteLine($"Number of rows retrieved:{valueList.Count}");
            if(valueList.Count > 0)
            {
                Console.WriteLine($"Number of columns retrieved: {valueList[0].Count}");
            }*/

            try
            {
                int index = -1;
                for(int i = 0; i < valueList.Count; i++)
                {
                    if(valueList.Count <= 0 || valueList[0].Count <= 0)
                    {
                        continue;
                    }

                    if(((string)valueList[i][1]).Equals(reward.DiscordUser.Username+"#"+reward.DiscordUser.Discriminator))
                    {
                        index = i+6;
                        Console.WriteLine($"{reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator} - index[{index}]");
                        break;
                    }
                }

                if(index == -1)
                {
                    //Failed to find user in list.
                    Console.WriteLine($"Failed to find discord user in player character sheet : {reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator}");
                    return false;
                }

                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

                IList<IList<object>> updatedValues = new List<IList<object>>();
                updatedValues.Add(new List<object>());
                updatedValues[0].Add(int.Parse(((string)valueList[index][11])) + reward.XPValue); //Exp

                string charDBSheetID = "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU";
                string range = $"'Player character sheet'!L{index}";
                SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(charDBSheetID, range);
                ValueRange expresponse = await request.ExecuteAsync();

                request = _service.Spreadsheets.Values.Get(charDBSheetID, range);
                ValueRange playedresponse = await request.ExecuteAsync();

                Console.WriteLine($"{reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator}[{index}] - CurrExp[{(string)expresponse.Values[0][0]}] CurrLastPlayed[{(string)playedresponse.Values[0][0]}]");
                Console.WriteLine($"        NewExp[{reward.XPValue}] NewLastPlayed[{reward.LastPlayedDate}]");
                
                /*
                //Update EXP Column
                ValueRange requestBody = new ValueRange() { MajorDimension = "ROWS", Values = updatedValues };
                SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(requestBody,
                    "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                    $"'Player character sheet'!L{index}");
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;

                AppendValuesResponse response = await request.ExecuteAsync();

                updatedValues = new List<IList<object>>();
                updatedValues.Add(new List<object>());
                updatedValues[0].Add(reward.LastPlayedDate.ToShortDateString()); //Date last played

                //Update LastPlayedColumn
                requestBody = new ValueRange() { MajorDimension = "ROWS", Values = updatedValues };
                request = _service.Spreadsheets.Values.Append(requestBody,
                    "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                    $"'Player character sheet'!P{index}");
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;

                response = await request.ExecuteAsync();
                */
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to update character sheet : {e.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a table of information regarding player character sheets from Google Sheets.
        /// </summary>
        /// <param name="logEntriesToConsole"></param>
        /// <returns></returns>
        public IList<IList<object>> GetCharacterSheetDB(bool logEntriesToConsole = false)
        {
            string range = "'Player character sheet'!A3:D";
            string charDBSheetID = "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU";
            int index = 0;
            try
            {
                SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(charDBSheetID, range);
                ValueRange response = request.Execute();
                var values = response.Values;

                if (values != null && values.Count > 0)
                {

                    if (logEntriesToConsole)
                    {
                        foreach (var row in values)
                        {
                            if (row.Count >= 3)
                            {
                                Console.WriteLine("{0} {1} {2}", row[0], row[1], row[2]);
                            }
                            index++;
                        }
                    }

                    return values;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to poll Character Database Sheet: {0} - {1}", e.Message, index);
            }

            return null;
        }

        /// <summary>
        /// Get a table of data from Google Sheets for rumor reports
        /// </summary>
        /// <param name="reportType"></param>
        /// <param name="logEntriesToConsole"></param>
        /// <returns></returns>
        public IList<IList<object>> GetReportSheetDB(int reportType, string range, int startIndex = 0, bool logEntriesToConsole = false)
        {
            /*
            switch (reportType)
            {
                case 0: //Post Game Reports
                    range = $"'Post Game Reports (PGR)'!A{_saveData.LastProcessedRowID_Games}:O";
                    _cachedStartIndex = _saveData.LastProcessedRowID_Games;
                    break;
                case 1: //Post Event Reports
                    range = $"'Post Event Reports (PER)'!A{_saveData.LastProcessedRowID_Events}:J";
                    _cachedStartIndex = _saveData.LastProcessedRowID_Events;
                    break;
                case 2: //Post Rumor Reports
                    range = $"'Post Rumor Reports (PRR)'!A{_saveData.LastProcessedRowID_Rumors}:G";
                    _cachedStartIndex = _saveData.LastProcessedRowID_Rumors;
                    break;
                default:
                    Console.WriteLine("Error during retrieval of report data : Invalid report type!");
                    return null;
            }
            */

            int index = startIndex;
            try
            {
                SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(SheetURLStub, range);
                ValueRange response = request.Execute();
                var values = response.Values;

                if (values != null && values.Count > 0)
                {
                    if (logEntriesToConsole)
                    {
                        foreach (var row in values)
                        {
                            Console.Write($"{index} - ");
                            foreach (var val in row)
                            {
                                Console.Write($"{val} ");
                            }
                            Console.Write("\n");
                            index++;
                        }
                    }

                    return values;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to poll Report Database Sheet: {0} - {1}", e.Message, index);
            }

            return null;
        }
    }
}
