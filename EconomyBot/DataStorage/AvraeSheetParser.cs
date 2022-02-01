using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace EconomyBot.DataStorage
{
    public class AvraeSheetParser
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
        private static string ApplicationName = "Andora GSheets Parser";

        private const string Range_Level = "'v2.1'!AL6:AM7";
        private const string Range_CurrentExp = "'v2.1'!AE7:AG7";

        private const string Range_ProficiencyBonus = "'v2.1'!H14:I15";

        private const string Range_SavingThrowList = "'v2.1'!I";
        private static readonly Dictionary<string, int> Range_SavingThrowRowLookup = new Dictionary<string, int>
        {
            { "Strength",           17 },
            { "Dexterity",          18 },
            { "Constitution",       19 },
            { "Intelligence",       20 },
            { "Wisdom",             21 },
            { "Charisma",           22 },
        };

        private const string Range_SkillList = "'v2.1'!I";
        private static readonly Dictionary<string, int> Range_SkillRowLookup = new Dictionary<string, int>
        {
            { "Acrobatics",         25 },
            { "Animal Handling",    26 },
            { "Arcana",             27 },
            { "Athletics",          28 },
            { "Deception",          29 },
            { "History",            30 },
            { "Insight",            31 },
            { "Intimidation",       32 },
            { "Investigation",      33 },
            { "Medicine",           34 },
            { "Nature",             35 },
            { "Perception",         36 },
            { "Performance",        37 },
            { "Persuasion",         38 },
            { "Religion",           39 },
            { "Sleight of Hand",    40 },
            { "Stealth",            41 },
            { "Survival",           42 },
        };

        private const string Range_PassivePerception = "'v2.1'!C45:D46";

        private const string Range_ArmorProficiency = "'v2.1'!I49:N49";
        private const string Range_WeaponProficiency = "'v2.1'!I50:N50";
        private const string Range_ToolProficiency = "'v2.1'!I52:N52";

        private const string Range_CopperPieces = "'Inventory'!D3:G4";
        private const string Range_SilverPieces = "'Inventory'!D6:G7";
        private const string Range_GoldPieces = "'Inventory'!D12:G13";

        private const string Range_InventoryColumn1 = "'Inventory'!I3:W76";
        private const string Range_InventoryColumn2 = "'Inventory'!Z3:AN76";

        public AvraeSheetParser(string googleCredentialsPath)
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
        /// Get the range for a specific skill by name
        /// </summary>
        /// <param name="skillName"></param>
        /// <returns></returns>
        public static string GetSkillBonusRange(string skillName)
        {
            if (Range_SkillRowLookup.ContainsKey(skillName))
            {
                return Range_SkillList + Range_SkillRowLookup[skillName];
            }
            return null;
        }

        /// <summary>
        /// Get the range for a specific skill by name
        /// </summary>
        /// <param name="skillName"></param>
        /// <returns></returns>
        public static string GetSavingThrowBonusRange(string savingThrowName)
        {
            if (Range_SavingThrowRowLookup.ContainsKey(savingThrowName))
            {
                return Range_SavingThrowList + Range_SavingThrowRowLookup[savingThrowName];
            }
            return null;
        }

        /// <summary>
        /// Parse a given range
        /// </summary>
        /// <param name="sheetURLStub"></param>
        /// <returns></returns>
        private IList<IList<object>> ParseRange(string sheetURLStub, string range)
        {
            try
            {
                SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(sheetURLStub, range);
                ValueRange response = request.Execute();

                return response.Values;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to poll Report Database Sheet: {0}", e.Message);
                return null;
            }
        }

        public bool CheckToolProficiency(string charSheetURL, string toolName)
        {
            var values = ParseRange(charSheetURL, Range_ToolProficiency);
            
            if (values != null && values.Count > 0)
            {
                var row = values[0];
                if(row == null)
                {
                    return false;
                }

                var value = row[0];
                if(value == null)
                {
                    return false;
                }

                var proficiencies = ((string)value).Split(",");
                foreach(var proficiency in proficiencies)
                {
                    if(proficiency.Replace("'", "").Replace("’", "").TrimStart().TrimEnd().ToLower().Contains(toolName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal string GetCurrency(string charSheetURL)
        {
            int gold = 0;
            int silver = 0;
            int copper = 0;

            var values = ParseRange(charSheetURL, Range_GoldPieces);
            if (values != null && values.Count > 0)
            {
                var row = values[0];
                if (row != null)
                {
                    var value = row[0];
                    if (value != null)
                    {
                        gold = int.Parse((string)value);
                    }
                }
            }

            values = ParseRange(charSheetURL, Range_SilverPieces);
            if (values != null && values.Count > 0)
            {
                var row = values[0];
                if (row != null)
                {
                    var value = row[0];
                    if (value != null)
                    {
                        silver = int.Parse((string)value);
                    }
                }
            }

            values = ParseRange(charSheetURL, Range_CopperPieces);
            if (values != null && values.Count > 0)
            {
                var row = values[0];
                if (row != null)
                {
                    var value = row[0];
                    if (value != null)
                    {
                        copper = int.Parse((string)value);
                    }
                }
            }

            return $"{gold}g{silver}s{copper}c";
        }
    }
}
