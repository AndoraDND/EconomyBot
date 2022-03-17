using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace EconomyBot.DataStorage
{
    public class TrackingSheetParser
    {
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

        private static string SheetURLStub = "1r9Wmr-uq1zUnCoVgq1Q8iz_0_o-l1zXOLmzsPkLFj70";//10-5DmLvpEcuFFTXTO5cxaXtrJFq-bOrYhO5PClUdZIk";

        //Range constants
        private const string Range_TrainingTracking = "'Academy'!A2:E";
        private const string Range_MundaneCraftingTracking = "'Mundane Crafting'!A2:F";
        private const string Range_MagicalCraftingTracking = "'Magical Crafting'!A2:I";
        private const string Range_ResearchTracking = "'Research'!A2:D";

        public TrackingSheetParser(string googleCredentialsPath)
        {
            //Load Google Sheets credentials
            var credential = GoogleCredential.FromFile(googleCredentialsPath).CreateScoped(Scopes);
            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        /// <summary>
        /// Parse a given range
        /// </summary>
        /// <param name="sheetURLStub"></param>
        /// <returns></returns>
        private IList<IList<object>> GetRange(string range)
        {
            try
            {
                SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(SheetURLStub, range);
                ValueRange response = request.Execute();

                return response.Values;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to poll Tracking Sheet: {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Submit a request to use DTD on research. Returns whether or not the research finished.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dtd"></param>
        /// <returns></returns>
        public async Task<Tuple<bool,int>> HandleResearchRequest(string guildNickname, CharacterData data, int dtd, int requiredDays)
        {
            var researchValues = GetRange(Range_ResearchTracking);

            if(guildNickname == null || guildNickname.Length <= 0)
            {
                guildNickname = data.CharacterName + data.DiscordName.Split('#')[0];
            }

            var researchInstances = researchValues.Where(p => ((string)p[0]).Contains(data.DiscordName.Split('#')[0])).Where(p=>p.Count < 4); //Find our user, but only when the research isn't complete
            IList<object> latestResearchInstance = null;

            if(researchInstances.Count() > 0)
            {
                latestResearchInstance = researchValues.Last();
            }

            if (latestResearchInstance != null)
            {
                int index = -1;
                for(int i = 0; i < researchValues.Count; i++)
                {
                    if (researchValues[i][0].Equals(latestResearchInstance[0]) && researchValues[i][2].Equals(latestResearchInstance[2]))
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                {
                    Console.WriteLine("Error: Failed to find research index again?");
                    return new Tuple<bool, int>(false, -1);
                }

                var daysSpent_unparsed = (latestResearchInstance[2] as string).Split("/");
                int.TryParse(daysSpent_unparsed[0], out var spentDays);
                int.TryParse(daysSpent_unparsed[1], out var maxDays);
                
                if(spentDays + dtd >= maxDays)
                {
                    //Research Completed
                    IList<IList<object>> valueList = new List<IList<object>>();
                    valueList.Add(new List<object>());
                    valueList[0].Add(guildNickname);
                    valueList[0].Add(DateTime.Now.ToShortDateString());
                    valueList[0].Add($"{maxDays}/{maxDays}");
                    valueList[0].Add(DateTime.Now.ToShortDateString());

                    //TODO: Update tracking sheet
                    await UpdateSheet($"'Research'!A{index + 2}:D{index + 2}", valueList);
                    return new Tuple<bool, int>(true, maxDays - (spentDays + dtd));
                }
                else
                {
                    //Research Incomplete
                    IList<IList<object>> valueList = new List<IList<object>>();
                    valueList.Add(new List<object>());
                    valueList[0].Add(guildNickname);
                    valueList[0].Add(DateTime.Now.ToShortDateString());
                    valueList[0].Add($"{spentDays + dtd}/{maxDays}");

                    //TODO: Update tracking sheet
                    await UpdateSheet($"'Research'!A{index + 2}:D{index + 2}", valueList);
                    return new Tuple<bool, int>(true, maxDays - (spentDays + dtd));
                }
            }
            else
            {
                //Client has no relevant research. Append list with a new value.

                IList<IList<object>> valueList = new List<IList<object>>();
                valueList.Add(new List<object>());
                valueList[0].Add(guildNickname);
                valueList[0].Add(DateTime.Now.ToShortDateString());
                valueList[0].Add($"{dtd}/{requiredDays}");

                await AppendSheet(Range_ResearchTracking, valueList);

                return new Tuple<bool, int>(true, requiredDays - (dtd));
                //Console.WriteLine("Error: Failed to find a relevant research.");
                //return false;
            }
        }

        /// <summary>
        /// Submit a request to use DTD on research. Returns whether or not the research finished.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dtd"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, int>> HandleTrainingRequest(string guildNickname, CharacterData data, string trainingName, int dtd, int requiredDays)
        {
            var trainingValues = GetRange(Range_TrainingTracking);

            if (guildNickname == null || guildNickname.Length <= 0)
            {
                guildNickname = data.CharacterName + data.DiscordName.Split('#')[0];
            }

            var trainingInstances = trainingValues.Where(p => ((string)p[0]).Contains(data.DiscordName.Split('#')[0])).Where(p => p.Count < 5).Where(p => ((string)p[1]).ToLower().Equals(trainingName.ToLower())); //Find our user, but only when the research isn't complete

            IList<object> latestTrainingInstance = null;

            if (trainingInstances.Count() > 0)
            {
                latestTrainingInstance = trainingValues.Last();
            }

            if (latestTrainingInstance != null)
            {
                int index = -1;
                for (int i = 0; i < trainingValues.Count; i++)
                {
                    if (trainingValues[i][0].Equals(latestTrainingInstance[0]) && trainingValues[i][3].Equals(latestTrainingInstance[3]))
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                {
                    Console.WriteLine("Error: Failed to find training index again?");
                    return new Tuple<bool, int>(false, -1);
                }

                var daysSpent_unparsed = (latestTrainingInstance[3] as string).Split("/");
                int.TryParse(daysSpent_unparsed[0], out var spentDays);
                int.TryParse(daysSpent_unparsed[1], out var maxDays);

                if (spentDays + dtd >= maxDays)
                {
                    //Research Completed
                    IList<IList<object>> valueList = new List<IList<object>>();
                    valueList.Add(new List<object>());
                    valueList[0].Add(guildNickname);
                    valueList[0].Add(trainingName);
                    valueList[0].Add(latestTrainingInstance[2]);
                    valueList[0].Add($"{maxDays}/{maxDays}");
                    valueList[0].Add(DateTime.Now.ToShortDateString());

                    //TODO: Update tracking sheet
                    await UpdateSheet($"'Academy'!A{index + 2}:E{index + 2}", valueList);
                    return new Tuple<bool, int>(true, maxDays - (spentDays + dtd));
                }
                else
                {
                    //Research Incomplete
                    IList<IList<object>> valueList = new List<IList<object>>();
                    valueList.Add(new List<object>());
                    valueList[0].Add(guildNickname);
                    valueList[0].Add(trainingName);
                    valueList[0].Add(latestTrainingInstance[2]);
                    valueList[0].Add($"{spentDays + dtd}/{maxDays}");

                    //TODO: Update tracking sheet
                    await UpdateSheet($"'Academy'!A{index + 2}:E{index + 2}", valueList);
                    return new Tuple<bool, int>(true, maxDays - (spentDays + dtd));
                }
            }
            else
            {
                //Client has no relevant research. Append list with a new value.

                IList<IList<object>> valueList = new List<IList<object>>();
                valueList.Add(new List<object>());
                valueList[0].Add(guildNickname);
                valueList[0].Add(trainingName);
                valueList[0].Add(DateTime.Now.ToShortDateString());
                valueList[0].Add($"{dtd}/{requiredDays}");

                await AppendSheet(Range_TrainingTracking, valueList);

                return new Tuple<bool, int>(true, requiredDays - (dtd));
                //Console.WriteLine("Error: Failed to find a relevant research.");
                //return false;
            }
        }

        private async Task<bool> AppendSheet(string range, IList<IList<object>> values)
        {
            try
            {
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

                ValueRange requestBody = new ValueRange() { MajorDimension = "ROWS", Values = values };

                SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(requestBody, SheetURLStub, range);
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;

                AppendValuesResponse response = await request.ExecuteAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to append Tracking Sheet: {0}", e.Message);
                return false;
            }
        }

        private async Task<bool> UpdateSheet(string range, IList<IList<object>> values)
        {
            try
            {
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                ValueRange requestBody = new ValueRange() { MajorDimension = "ROWS", Values = values };

                SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(requestBody, SheetURLStub, range);
                request.ValueInputOption = valueInputOption;

                UpdateValuesResponse response = await request.ExecuteAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to update Tracking Sheet: {0}", e.Message);
                return false;
            }
        }
    }
}
