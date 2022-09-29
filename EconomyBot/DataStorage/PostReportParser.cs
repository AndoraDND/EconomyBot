using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Sheet URL stub for the PostReport sheet
        /// </summary>
        private static string SheetURLStub = "1vPl8oj_pdhVwu23ZYdqjA3rgayAWyJzvyAc9iMgF-UQ";

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

        private IList<IList<object>> cachedPlayerCharacterSheet = null;

        public async Task<string> UpdatePriority(DateTime date, params SocketGuildUser[] users)
        {
            string range = "'Player character sheet'!A6:Q";
            string charDBSheetID = "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU";

            var ErrorHandlingPlayers = new List<string>();
            var OutputPlayers = new List<Tuple<string, DateTime>>();
            
            var output = "Process Log:\n";

            await Task.Delay(60000 - (DateTime.Now.Second * 1000)); // This disgusting line is just for avoiding rate limiting. :(

            foreach (var user in users)
            {
                if (user == null)
                    continue;

                #region Post rewards

                //Update Player Character Sheet with new Values.
                
                SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(charDBSheetID, range);
                ValueRange response = await request.ExecuteAsync();
                cachedPlayerCharacterSheet = response.Values;

                //Tuple<string,string> error = null;
                //var reward = new CombinedReward()
                //{
                //    DiscordUser = user,
                //    LastExpDate = date,
                //    XPValue = 0,
                //    LastPlayedDate = date,
                //    ReportID = -1
                //};

                var lastPlayedDate = await GetCurrentPriorityDate(cachedPlayerCharacterSheet, user, date);

                //var error = await UpdateCharacterSheetWithReward(cachedPlayerCharacterSheet, reward);

                //Update the player character sheet
                if (lastPlayedDate == null && lastPlayedDate.HasValue)
                {
                    ErrorHandlingPlayers.Add(user.Username + "#" + user.Discriminator);
                    //ErrorHandlingPlayers.Add(new PostReport_PlayerError(ReportType.Unknown,
                    //    reward.ReportID,
                    //    reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator,
                    //    reward.XPValue,
                    //    error.Item1,
                    //    error.Item2));
                }
                else
                {
                    OutputPlayers.Add(new Tuple<string, DateTime>(user.Username + "#" + user.Discriminator, lastPlayedDate.Value));
                }

                await Task.Delay(6000);
                
                #endregion
            }

            #region Output

            //Display output to the calling user.
            output += "\nPrevious Dates\n";
            foreach (var player in OutputPlayers)
            {
                output += $"{player.Item1} : [{player.Item2}]\n";
            }
            output += "\nErrorLog:\n";
            foreach (var player in ErrorHandlingPlayers)
            {
                output += $"{player} - [ Failed to update ]\n";
            }

            return output;

            #endregion
        }

        /// <summary>
        /// Process the list of reports needing to be processed.
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        public async Task ProcessReports(Discord.Commands.SocketCommandContext Context)
        {
            await Context.Channel.SendMessageAsync("**Starting report process.** \n*Please note that this process may take some time and you may experience significant delays.*\nDo not start another process while waiting.");

            #region Poll Reports 

            //Collect unprocessed reports
            var GameReports = GetUnprocessedGameReports();
            var EventReports = GetUnprocessedEventReports();

            //Early exit if we have no reports to process.
            if(GameReports.Count <= 0 && EventReports.Count <= 0)
            {
                await Context.Channel.SendMessageAsync("**Finished.**\n*No pending reports to process!*");
                return;
            }

            var TotalCalculatedRewards = new List<CombinedReward>();
            var ErrorHandlingPlayers = new List<PostReport_PlayerError>();

            #endregion

            await Context.Channel.SendMessageAsync("Successfully polled new reports. *Beginnning processing...*");

            #region Process Reports

            //Riddiculous that we need to do this, but whatever I guess.
            if (true) //_client.GetGuild(Context.Guild.Id).Users.Count != Context.Guild.Users.Count())
            {
                Console.WriteLine($"Need to download new users. {_client.GetGuild(Context.Guild.Id).Users.Count} != {Context.Guild.Users.Count()}");
                await Context.Guild.DownloadUsersAsync();
            }

            //Generate final output
            var output = "Process Log:\n";

            output += "Handling Game Reports:\n";
            foreach (var report in GameReports)
            {
                HandleGameReport(Context, report, ref output, ref TotalCalculatedRewards, ref ErrorHandlingPlayers);
                try
                {
                    await _andoraService.NotifierService.HandleDispatch(Context.Guild.Id, report.DMName, report.MissionRunDate.ToShortDateString() + report.MissionRunDate.ToShortTimeString(), report.Players, report.SessionSpecifics);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Failed dispatching message to NotifierService! : {e.Message} - {e.StackTrace}");
                }
                await Task.Delay(1000);
            }

            output += "Handling Event Reports:\n";
            foreach (var report in EventReports)
            {
                HandleEventReport(Context, report, ref output, ref TotalCalculatedRewards, ref ErrorHandlingPlayers);
                await Task.Delay(1000);
            }

            #endregion

            await Context.Channel.SendMessageAsync("Successfully processed reports. *Beginning reward allocation...*");

            #region Post rewards

            //Update Player Character Sheet with new Values.
            string range = "'Player character sheet'!A6:Q";
            string charDBSheetID = "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU";

            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(charDBSheetID, range);
            ValueRange response = await request.ExecuteAsync();
            cachedPlayerCharacterSheet = response.Values;

            await Task.Delay(60000 - (DateTime.Now.Second * 1000)); // :(
            
            List<int> removeFlags = new List<int>();
            int i = 0;
            foreach (var reward in TotalCalculatedRewards)
            {
                var error = await UpdateCharacterSheetWithReward(cachedPlayerCharacterSheet, reward);
                //Tuple<string,string> error = null;

                //Update the player character sheet
                if (error != null)
                {
                    removeFlags.Add(i);
                    ErrorHandlingPlayers.Add(new PostReport_PlayerError(ReportType.Unknown,
                        reward.ReportID,
                        reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator,
                        reward.XPValue,
                        error.Item1,
                        error.Item2));
                }
                i++;
                await Task.Delay(6000);
            }

            #endregion

            #region Cleanup

            //Remove unhandled rewards
            removeFlags.Reverse();
            foreach (var value in removeFlags)
            {
                TotalCalculatedRewards.RemoveAt(value);
            }

            //Mark our processed reports as such on the PostReport response sheet.
            foreach (var report in GameReports)
            {
                await SetGameReportProcessed(report);
            }
            foreach(var report in EventReports)
            {
                await SetEventReportProcessed(report);
            }

            #endregion

            #region Output

            //Display output to the calling user.
            foreach (var player in TotalCalculatedRewards)
            {
                output += $"{player.DiscordUser} [{player.LastPlayedDate}]: - {player.XPValue}\n";
            }
            output += "\nErrorLog:\n";
            foreach(var player in ErrorHandlingPlayers)
            {
                output += $"{player.ReportType.ToString()} - [ {player.ReportRowID} ]: " +
                    $"<Name: {player.PlayerName}, Exp: {player.ExperienceValue}> - " +
                    $"{player.ErrorMessage} {(player.StackTrace.Length > 0 ? ($" - {player.StackTrace}"):" ")} \n";
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
                await Context.Channel.SendFileAsync(dumpFilePath, $"{Context.User.Mention}");
                File.Delete(dumpFilePath);
            }

            //Console.WriteLine("Finished processing reports");

            #endregion
        }

        /// <summary>
        /// Handle the data of an Event Report
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="report"></param>
        /// <param name="output"></param>
        /// <param name="TotalCalculatedRewards"></param>
        /// <param name="ErrorHandlingPlayers"></param>
        private void HandleEventReport(Discord.Commands.SocketCommandContext Context, PostEventReport report, ref string output, ref List<CombinedReward> TotalCalculatedRewards, ref List<PostReport_PlayerError> ErrorHandlingPlayers)
        {
            output += $"RowID: {report.RowID}\n";
            if (report.XPAwarded > 0)
            {
                foreach (var player in report.Participants)
                {
                    try
                    {
                        string[] splitUser = player.Split('#');
                        //Console.WriteLine(splitUser[0] + " - " + splitUser[1]);

                        Discord.IUser discordUser = null;
                        if (Context.Guild.Users.Count > 0)
                        {
                            foreach (var user in Context.Guild.Users)
                            {
                                string username = user.Username;
                                if (username.Equals(splitUser[0]) && user.Discriminator.Equals(splitUser[1]))
                                {
                                    discordUser = user;
                                    break;
                                }
                            }
                            //discordUser = Context.Guild.Users.Where(p => p.Username.Substring(1, p.Username.Length - 2).Equals(splitUser[0]) && p.Discriminator.Equals(splitUser[1])).FirstOrDefault();
                        }

                        if (discordUser == null || discordUser == default(Discord.IUser))
                        {
                            //discordUser = (await Context.Guild.SearchUsersAsync(player, 1)).First();
                        }
                        //var discordUser = (await Context.Guild.SearchUsersAsync(player.playerName, 1)).First();//.GetUser(splitUser[0], splitUser[1]);
                        if (discordUser != null)
                        {
                            var index = TotalCalculatedRewards.FindIndex(p => p.DiscordUser.Id.Equals(discordUser.Id));
                            if (index >= 0)
                            {
                                TotalCalculatedRewards[index].XPValue += report.XPAwarded;
                                if (TotalCalculatedRewards[index].LastExpDate < DateTime.MinValue)
                                {
                                    TotalCalculatedRewards[index].LastExpDate = DateTime.MinValue;
                                }
                            }
                            else
                            {
                                var newReward = new CombinedReward() { ReportID = report.RowID, DiscordUser = discordUser, LastPlayedDate = DateTime.MinValue, XPValue = report.XPAwarded };
                                TotalCalculatedRewards.Add(newReward);
                            }
                        }
                        else
                        {
                            ErrorHandlingPlayers.Add(new PostReport_PlayerError(ReportType.Event, report.RowID, player, report.XPAwarded, "Failed to find DiscordUser!"));
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorHandlingPlayers.Add(new PostReport_PlayerError(ReportType.Event, report.RowID, player, report.XPAwarded, e.Message, e.StackTrace));
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Handle the data of a Game Report
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="report"></param>
        /// <param name="output"></param>
        /// <param name="TotalCalculatedRewards"></param>
        /// <param name="ErrorHandlingPlayers"></param>
        private void HandleGameReport(Discord.Commands.SocketCommandContext Context, PostGameReport report, ref string output, ref List<CombinedReward> TotalCalculatedRewards, ref List<PostReport_PlayerError> ErrorHandlingPlayers)
        {
            output += $"RowID: {report.RowID}\n";
            var rewardData = JsonConvert.DeserializeObject<List<JSONRewardData>>(report.JSON);

            foreach (var player in rewardData)
            {
                try
                {
                    string[] splitUser = player.playerName.Split('#');
                    //Console.WriteLine(splitUser[0] + " - " + splitUser[1]);

                    Discord.IUser discordUser = null;
                    if (Context.Guild.Users.Count > 0)
                    {
                        foreach (var user in Context.Guild.Users)
                        {
                            string username = user.Username;
                            if (username.Equals(splitUser[0]) && user.Discriminator.Equals(splitUser[1]))
                            {
                                discordUser = user;
                                break;
                            }
                        }
                        //discordUser = Context.Guild.Users.Where(p => p.Username.Substring(1, p.Username.Length - 2).Equals(splitUser[0]) && p.Discriminator.Equals(splitUser[1])).FirstOrDefault();
                    }

                    if (discordUser == null || discordUser == default(Discord.IUser))
                    {
                        //discordUser = (await Context.Guild.SearchUsersAsync(player.playerName, 1)).First();
                    }
                    //var discordUser = (await Context.Guild.SearchUsersAsync(player.playerName, 1)).First();//.GetUser(splitUser[0], splitUser[1]);
                    if (discordUser != null)
                    {
                        var index = TotalCalculatedRewards.FindIndex(p => p.DiscordUser.Id.Equals(discordUser.Id));
                        if (index >= 0)
                        {
                            TotalCalculatedRewards[index].XPValue += player.playerExp;
                            if (TotalCalculatedRewards[index].LastPlayedDate < report.MissionRunDate)
                            {
                                TotalCalculatedRewards[index].LastPlayedDate = report.MissionRunDate;
                            }
                            if (TotalCalculatedRewards[index].LastExpDate < report.MissionRunDate)
                            {
                                TotalCalculatedRewards[index].LastExpDate = report.MissionRunDate;
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
                        ErrorHandlingPlayers.Add(new PostReport_PlayerError(ReportType.Game, report.RowID, player.playerName, player.playerExp, "Failed to Find DiscordUser"));
                    }
                }
                catch (Exception e)
                {
                    ErrorHandlingPlayers.Add(new PostReport_PlayerError(ReportType.Game, report.RowID, player.playerName, player.playerExp, e.Message, e.StackTrace));
                    continue;
                }
            }
        }

        public Tuple<int,int> GetSessions_FromGameReports(string discordName)
        {
            int totalSessions = 0;
            int totalXP = 0;

            var values = GetReportSheetDB(0, $"'Post Game Reports (PGR)'!A34:P", 34, false);
            int rowID = 34;
            foreach (var row in values)
            {
                try
                {
                    var players = new List<string>();
                    for (int i = 3; i <= 8; i++)
                    {
                        if (((string)row[i]) != null && ((string)row[i]).Length > 0)
                        {
                            players.Add((string)row[i]);
                        }
                    }

                    if (!players.Contains(discordName))
                    {
                        continue;
                    }

                    totalSessions++;

                    var JSON = (string)row[14];
                    var jsonData = JsonConvert.DeserializeObject<List<JSONRewardData>>(JSON);
                    var rewardData = jsonData.FirstOrDefault(p => p.playerName.Contains(discordName));
                    if (rewardData != null)
                    {
                        totalXP += rewardData.playerExp;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to process row <{rowID}> : {e.Message}");
                }

                //Increment row ID
                rowID++;
            }

            return new Tuple<int, int>(totalSessions, totalXP);
        }

        public Tuple<int,int> GetEventParticipation_FromEventReports(string discordName)
        {
            int totalEvents = 0;
            int totalXP = 0;

            var values = GetReportSheetDB(0, $"'Post Event Reports (PER)'!A17:P", 17, false);
            int rowID = 17;
            foreach (var row in values)
            {
                try
                {
                    if(row[5] == null || ((string)row[5]).Length <= 0 || !int.TryParse((string)row[5], out var xp))
                    {
                        continue;
                    }

                    

                    //var participants = new List<string>();
                    //var valueArr = ((string)row[4]).Replace("\n", "").Split(',');

                    if (((string)row[4]).Contains(discordName))
                    {
                        totalEvents++;

                        if (xp < 1)
                        {
                            continue;
                        }
                        if (int.TryParse((string)row[5], out xp)) 
                        { 
                            totalXP += xp;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to process row <{rowID}> : {e.Message}");
                }

                //Increment row ID
                rowID++;
            }

            return new Tuple<int, int>(totalEvents, totalXP);
        }

        /// <summary>
        /// Get a list of PostGameReport objects based on unprocessed reports.
        /// </summary>
        /// <returns></returns>
        public List<PostGameReport> GetUnprocessedGameReports()
        {
            var retVal = new List<PostGameReport>();

            var values = GetReportSheetDB(0, $"'Post Game Reports (PGR)'!A64:P", 64, false);
            int rowID = 64;
            foreach(var row in values)
            {
                try
                {
                    if (row[15] != null && !((string)row[15]).Equals("TRUE"))
                    {
                        var report = new PostGameReport(rowID);
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

        /// <summary>
        /// Get a list of PostEventReport objects based on unprocessed reports.
        /// </summary>
        /// <returns></returns>
        public List<PostEventReport> GetUnprocessedEventReports()
        {
            var retVal = new List<PostEventReport>();

            var values = GetReportSheetDB(0, $"'Post Event Reports (PER)'!A25:P", 25, false);
            int rowID = 25;
            foreach (var row in values)
            {
                try
                {
                    if (row[15] != null && !((string)row[15]).Equals("TRUE")) //Make sure the row isn't already processed.
                    {
                        var report = new PostEventReport(rowID);
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

        public async Task<DateTime?> GetCurrentPriorityDate(IList<IList<object>> valueList, Discord.IUser user, DateTime? newDate = null)
        {
            if (valueList == null && valueList.Count <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error getting current priority : Value list is null or empty!");
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }

            try
            {
                int index = -1;
                for (int i = 0; i < valueList.Count; i++)
                {
                    if (valueList[i].Count <= 0) //We have an empty row.
                    {
                        continue;
                    }

                    if(((string)valueList[i][1]).Equals(user.Username+"#"+user.Discriminator)) //Check if we found our user.
                    {
                        index = i+6;
                        //Console.WriteLine($"{reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator} - index[{index}]");
                        break;
                    }
                }

                if(index == -1)
                {
                    //Failed to find user in list.
                    Console.WriteLine($"Failed to find discord user in player character sheet : {user.Username + "#" + user.Discriminator}");
                    return null;
                }

                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                
                //Parse current experience and expected experience gain
                if(!int.TryParse(((string)valueList[index-6][11]), out var currExp))
                {
                    //Current experience column on the Player Character Sheet is botched.
                    Console.WriteLine($"Failed to poll Player Experience: Check that column is an integer value!");
                    return null;
                }

                //Parse current Last Played Date and expected new Date.
                var dateString = (string)valueList[index - 6][15]; //Check Last Played Date Column
                if(dateString == null || dateString.Length <= 0)
                {
                    //Column was empty
                    dateString = (string)valueList[index - 6][0]; //Use the Date Created timestamp instead
                }

                if(!DateTime.TryParse(dateString, out var currLastPlayed)) //Check if parsing the timestamp and column succeeded
                {
                    //How even
                    currLastPlayed = new DateTime(2022, 1, 1);
                }
                if (newDate.Value != null)
                {
                    var newLastPlayed = newDate.Value < currLastPlayed ? currLastPlayed : newDate.Value;

                    //TODO: Batch requests. https://developers.google.com/sheets/api/guides/batch
                    //As is, this is stupid. But it works for now.

                    //Update the last played date column of the Player Character Sheet.
                    IList<IList<object>> updatedValues = new List<IList<object>>();
                    updatedValues.Add(new List<object>());
                    updatedValues[0].Add($"{newLastPlayed.ToShortDateString()} {newLastPlayed.ToShortTimeString()}"); //Date last played

                    ValueRange requestBody;
                    SpreadsheetsResource.ValuesResource.UpdateRequest request;
                    UpdateValuesResponse response;
                    try
                    {
                        //Update LastPlayedColumn
                        requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues };
                        request = _service.Spreadsheets.Values.Update(requestBody,
                            "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                            $"'Player character sheet'!P{index}");
                        request.ValueInputOption = valueInputOption;

                        response = await request.ExecuteAsync();
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Equals("Google.Apis.Requests.RequestError"))
                        {
                            //Rate limited. Try again.
                            await Task.Delay(60000);

                            requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues };
                            request = _service.Spreadsheets.Values.Update(requestBody,
                                "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                                $"'Player character sheet'!P{index}");
                            request.ValueInputOption = valueInputOption;

                            response = await request.ExecuteAsync();
                        }
                        else
                        {
                            throw e;
                        }
                    }
                }
                return currLastPlayed;
            }
            catch (Exception e)
            {
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to update character sheet : {e.Message}\n{e.StackTrace}");
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }

            return null;
        }

        /// <summary>
        /// Post a reward to the Player Character Sheet
        /// </summary>
        /// <param name="valueList"></param>
        /// <param name="reward"></param>
        /// <returns></returns>
        public async Task<Tuple<string,string>> UpdateCharacterSheetWithReward(IList<IList<object>> valueList, CombinedReward reward)
        {
            if (valueList == null && valueList.Count <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error updating character sheet : Value list is null or empty!");
                Console.ForegroundColor = ConsoleColor.White;
                return new Tuple<string, string>("Error updating character sheet : Value list is null or empty!", "");
            }

            try
            {
                int index = -1;
                for (int i = 0; i < valueList.Count; i++)
                {
                    if (valueList[i].Count <= 0) //We have an empty row.
                    {
                        continue;
                    }

                    if(((string)valueList[i][1]).Equals(reward.DiscordUser.Username+"#"+reward.DiscordUser.Discriminator)) //Check if we found our user.
                    {
                        index = i+6;
                        //Console.WriteLine($"{reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator} - index[{index}]");
                        break;
                    }
                }

                if(index == -1)
                {
                    //Failed to find user in list.
                    Console.WriteLine($"Failed to find discord user in player character sheet : {reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator}");
                    return new Tuple<string, string>($"Failed to find discord user in player character sheet : {reward.DiscordUser.Username + "#" + reward.DiscordUser.Discriminator}", "");
                }

                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                
                //Parse current experience and expected experience gain
                if(!int.TryParse(((string)valueList[index-6][11]), out var currExp))
                {
                    //Current experience column on the Player Character Sheet is botched.
                    return new Tuple<string, string>($"Failed to poll Player Experience: Check that column is an integer value!", "");
                }
                var newExp = currExp + reward.XPValue;

                //Parse current Last Played Date and expected new Date.
                var dateString = (string)valueList[index - 6][15]; //Check Last Played Date Column
                if(dateString == null || dateString.Length <= 0)
                {
                    //Column was empty
                    dateString = (string)valueList[index - 6][0]; //Use the Date Created timestamp instead
                }

                if(!DateTime.TryParse(dateString, out var currLastPlayed)) //Check if parsing the timestamp and column succeeded
                {
                    //How even
                    currLastPlayed = new DateTime(2022, 1, 1);
                }
                var newLastPlayed = reward.LastPlayedDate < currLastPlayed ? currLastPlayed : reward.LastPlayedDate;
                
                //Update the experience column of the Player Character Sheet.
                IList<IList<object>> updatedValues = new List<IList<object>>();
                updatedValues.Add(new List<object>());
                updatedValues[0].Add(newExp); //Exp

                //TODO: Batch requests. https://developers.google.com/sheets/api/guides/batch
                //As is, this is stupid. But it works for now.

                ValueRange requestBody;
                SpreadsheetsResource.ValuesResource.UpdateRequest request;
                UpdateValuesResponse response;
                try
                {
                    //Update EXP Column
                    requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues };
                    request = _service.Spreadsheets.Values.Update(requestBody,
                        "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                        $"'Player character sheet'!L{index}");
                    request.ValueInputOption = valueInputOption;

                    response = await request.ExecuteAsync();
                }
                catch(Exception e)
                {
                    if (e.Message.Equals("Google.Apis.Requests.RequestError"))
                    {
                        //Rate limited. Try again.
                        await Task.Delay(60000);

                        requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues };
                        request = _service.Spreadsheets.Values.Update(requestBody,
                            "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                            $"'Player character sheet'!L{index}");
                        request.ValueInputOption = valueInputOption;

                        response = await request.ExecuteAsync();
                    }
                    else
                    {
                        throw e;
                    }
                }


                //Update the last played date column of the Player Character Sheet.
                updatedValues = new List<IList<object>>();
                updatedValues.Add(new List<object>());
                updatedValues[0].Add($"{newLastPlayed.ToShortDateString()} {newLastPlayed.ToShortTimeString()}"); //Date last played

                try
                {
                    //Update LastPlayedColumn
                    requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues };
                    request = _service.Spreadsheets.Values.Update(requestBody,
                        "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                        $"'Player character sheet'!P{index}");
                    request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                    response = await request.ExecuteAsync();
                }
                catch(Exception e)
                {
                    if (e.Message.Equals("Google.Apis.Requests.RequestError"))
                    {
                        //Rate limited. Try again.
                        await Task.Delay(60000);

                        requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues };
                        request = _service.Spreadsheets.Values.Update(requestBody,
                            "1V0JMpSLVmuenr_kea8UmP8Ii87jo1g_9iG6cf8MF7RU",
                            $"'Player character sheet'!P{index}");
                        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                        response = await request.ExecuteAsync();
                    }
                    else
                    {
                        throw e;
                    }
                }
                
            }
            catch (Exception e)
            {
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to update character sheet : {e.Message}\n{e.StackTrace}");
                Console.ForegroundColor = ConsoleColor.White;
                return new Tuple<string, string>($"Failed to update character sheet : {e.Message}", e.StackTrace);
            }

            return null;
        }

        /// <summary>
        /// Set a Post Game Report as processed on the PostReport sheet
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        private async Task SetGameReportProcessed(PostGameReport report)
        {
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            SpreadsheetsResource.ValuesResource.UpdateRequest.ResponseValueRenderOptionEnum responseRenderOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ResponseValueRenderOptionEnum.FORMATTEDVALUE;

            IList<IList<object>> updatedValues = new List<IList<object>>();
            updatedValues.Add(new List<object>());
            updatedValues[0].Add("TRUE"); 

            ValueRange requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues }; //Range = $"'Post Game Reports (PGR)'!P{report.RowID}",
            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(requestBody,
                SheetURLStub,
                $"'Post Game Reports (PGR)'!P{report.RowID}");
            request.ValueInputOption = valueInputOption;
            request.ResponseValueRenderOption = responseRenderOption;

            UpdateValuesResponse response = await request.ExecuteAsync();
        }

        /// <summary>
        /// Set a Post Event Report as processed on the PostReport sheet
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        private async Task SetEventReportProcessed(PostEventReport report)
        {
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            SpreadsheetsResource.ValuesResource.UpdateRequest.ResponseValueRenderOptionEnum responseRenderOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ResponseValueRenderOptionEnum.FORMATTEDVALUE;

            IList<IList<object>> updatedValues = new List<IList<object>>();
            updatedValues.Add(new List<object>());
            updatedValues[0].Add("TRUE");

            ValueRange requestBody = new ValueRange() { MajorDimension = "COLUMNS", Values = updatedValues }; //Range = $"'Post Event Reports (PER)'!P{report.RowID}",
            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(requestBody,
                SheetURLStub,
                $"'Post Event Reports (PER)'!P{report.RowID}");
            request.ValueInputOption = valueInputOption;
            request.ResponseValueRenderOption = responseRenderOption;

            UpdateValuesResponse response = await request.ExecuteAsync();
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
