using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using EconomyBot.DataStorage;

namespace EconomyBot
{
    public class PriceDatabase
    {
        private Dictionary<string, Tuple<string, int, int, int>> _storedData;

        private string _databaseFileName;

        public PriceDatabase(string fileName)
        {
            _databaseFileName = fileName;

            LoadDatabase(_databaseFileName);
        }

        /// <summary>
        /// Parse a CSV file for this database. Load the data into the classes StoredData dictionary.
        /// </summary>
        /// <param name="fileName"></param>
        private void LoadDatabase(string fileName)
        {
            _storedData = new Dictionary<string, Tuple<string, int, int, int>>();
            
            var fileData = FileReader.ReadCSV(fileName);
            if (fileData != null)
            {
                foreach (var obj in fileData)
                {
                    _storedData.Add(obj.Key, new Tuple<string, int, int, int>(obj.Value[0], int.Parse(obj.Value[1]), int.Parse(obj.Value[2]), int.Parse(obj.Value[3])));
                }
            }
        }

        private void SaveDatabase(string fileName)
        {
            string fileData = "";
            foreach(var keypair in _storedData)
            {
                fileData += $"{keypair.Key.ToLower()}, {keypair.Value.Item1}, {keypair.Value.Item2}, {keypair.Value.Item3}, {keypair.Value.Item4},\n";
            }

            FileReader.WriteCSV(fileName, fileData);
        }

        /// <summary>
        /// Parse a copper value amount and return the value as a string formated to Gold/Silver/Copper
        /// </summary>
        /// <param name="copperValue"></param>
        /// <returns></returns>
        public string FormatGold(int copperValue)
        {
            var remainder = copperValue;
            int gold = (remainder / 100);
            remainder %= 100;
            int silver = (remainder / 10);
            remainder %= 10;
            int copper = remainder;

            var retVal = "";
            if(gold != 0)
            {
                retVal += $"{gold}gp";
            }
            if(silver != 0)
            {
                if (retVal.Length > 0)
                {
                    retVal += ",";
                }
                retVal += $" {silver}sp";
            }
            if (copper != 0)
            {
                if (retVal.Length > 0)
                {
                    retVal += ",";
                }
                retVal += $" {copper}cp";
            }

            return retVal;
        }
    
        /// <summary>
        /// Parse an item value from string and return the result as a copper value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int ParseGold(string value)
        {
            int retVal = 0;
            string lowerValue = value.ToLower().TrimStart().TrimEnd();
            var itemValues = lowerValue.Replace(",", "").Replace("p", "").Replace("g", "g ").Replace("s", "s ").Replace("c", "c ").Split(' ');
            if (itemValues.Length > 1)
            {
                for (int i = itemValues.Length - 1; i >= 0; i--)
                {
                    if (itemValues[i].Length > 0)
                    {
                        if (itemValues[i].Contains('g'))
                        {
                            retVal += int.Parse(itemValues[i].Replace("g", "")) * 100;
                        }
                        else if (itemValues[i].Contains('s'))
                        {
                            retVal += int.Parse(itemValues[i].Replace("s", "")) * 10;
                        }
                        else if (itemValues[i].Contains('c'))
                        {
                            retVal += int.Parse(itemValues[i].Replace("c", ""));
                        }
                    }
                }
                return retVal;
            }
            else if(itemValues.Length > 0)
            {
                if (itemValues[0].Contains('g'))
                {
                    retVal += int.Parse(itemValues[0].Replace("g", "")) * 100;
                }
                else if (itemValues[0].Contains('s'))
                {
                    retVal += int.Parse(itemValues[0].Replace("s", "")) * 10;
                }
                else if (itemValues[0].Contains('c'))
                {
                    retVal += int.Parse(itemValues[0].Replace("c", ""));
                }
                else
                {
                    retVal += int.Parse(itemValues[0]) * 100;
                }
            }
            
            return retVal;
        }

        /// <summary>
        /// Try to parse the copper value from a string. Returns false if something went wrong.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal bool CanParseGold(string value)
        {
            try
            {
                ParseGold(value);
            }
            catch(Exception e)
            {
                return false;
            }

            return true;
        }

        internal bool AddItem(string itemName, string category, string goldCost)
        {
            if(_storedData.ContainsKey(itemName.ToLower()))
            {
                return false;
            }

            var copperCost = ParseGold(goldCost);
            _storedData.Add(itemName.ToLower(), new Tuple<string, int, int, int>(category, copperCost, copperCost/2, copperCost*2));

            SaveDatabase(_databaseFileName);
            return true;
        }

        /// <summary>
        /// Get the details for a specific database item.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Tuple<string, int, int, int> GetItemDetails(string name)
        {
            if(_storedData.ContainsKey(name))
            {
                return _storedData[name];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Find the exact name of an item in the database based on an approximate name.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pollService"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal async Task<string> FindItem(string input, ReactionReplyService pollService, SocketCommandContext context, bool elevatedPermissions = false)
        {
            var searchList = new List<Tuple<string, int>>();
            foreach(var item in _storedData)
            {
                var difference = StringSearch.Compare(input, item.Key);

                if (!elevatedPermissions && !item.Value.Item1.TrimStart().TrimEnd().Equals("General"))
                    continue;

                if(difference == item.Key.Length && difference == input.Length)
                {
                    searchList.Clear();
                    searchList.Add(new Tuple<string, int>(item.Key, difference));
                    break;
                }
                else //if(difference < 8)
                {
                    searchList.Add(new Tuple<string, int>(item.Key, difference));
                }
            }

            //Sort the list in descending order by comparing string difference
            searchList.Sort((a, b) => b.Item2.CompareTo(a.Item2));

            if (searchList.Count <= 1)
            {
                //Only found one result.
                return searchList[0].Item1;
            }
            else
            {
                //Need user input, since we didn't find an exact match.

                var optionList = new List<string>();

                //Create option list
                for(int i = 0; i < Math.Min(searchList.Count, 6); i++)
                {
                    optionList.Add(searchList[i].Item1);
                }

                optionList.Add("None of the Above");

                var itemName = await pollService.CreatePoll("No exact match, found a few similar items", optionList, context);

                if (itemName.Equals("None of the Above"))
                {
                    itemName = null;
                }

                searchList.Clear();
                optionList.Clear();

                searchList = null;
                optionList = null;

                return itemName;
            }
        }

        internal void SetPrice(string itemName, int copperValue)
        {
            if(!_storedData.ContainsKey(itemName))
            {
                return;
            }

            _storedData[itemName] = new Tuple<string, int, int, int>(_storedData[itemName].Item1,
                copperValue,
                copperValue / 2,
                copperValue * 2
                );
            
            SaveDatabase(_databaseFileName);
        }

        internal void SetCategory(string itemName, string category)
        {
            if (!_storedData.ContainsKey(itemName))
            {
                return;
            }

            _storedData[itemName] = new Tuple<string, int, int, int>(category,
                _storedData[itemName].Item2,
                _storedData[itemName].Item3,
                _storedData[itemName].Item4
                );

            SaveDatabase(_databaseFileName);
        }

        /// <summary>
        /// Get all data currently within this database.
        /// </summary>
        /// <returns></returns>
        internal string Dump()
        {
            var output = "";
            foreach (var item in _storedData)
            {
                output += PrintItem(item) + "\n\n";
            }
            return output;
        }

        /// <summary>
        /// Print the data for an object stored in this database.
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        private string PrintItem(KeyValuePair<string, Tuple<string, int, int, int>> itemData, int indent = 4)
        {
            var indentSpace = "";
            for (int i = 0; i < indent; i++)
            {
                indentSpace += " ";
            }

            var retVal = "";
            retVal = $"{indentSpace}ItemName: {itemData.Key},\n" +
                $"{indentSpace}Category: {itemData.Value.Item1},\n" +
                $"{indentSpace}Market_Avg: {itemData.Value.Item2},\n" +
                $"{indentSpace}Market_Low: {itemData.Value.Item3},\n" +
                $"{indentSpace}Market_High: {itemData.Value.Item4}";
            return retVal;
        }

    }
}
