using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EconomyBot.DataStorage
{
    internal static class FileReader
    {
        private static readonly string CurrentDirectory = Directory.GetCurrentDirectory(); 
        private const string FilePath = "/Data";
        private const string LogsPath = FilePath + "/Logs";
        private const string DebugLogsPath = LogsPath + "/DebugLogs";
        private const string ErrorLogsPath = LogsPath + "/ErrorLogs";
        private const string DatabasePath = FilePath + "/DB";

        private static bool hasCheckedDirectories = false;

        private static void HandleFolderDirectories()
        {
            if (!hasCheckedDirectories)
            {
                //Created folder directories if needed.
                if (!Directory.Exists(CurrentDirectory + FilePath))
                {
                    Directory.CreateDirectory(CurrentDirectory + FilePath);
                }
                if (!Directory.Exists(CurrentDirectory + LogsPath))
                {
                    Directory.CreateDirectory(CurrentDirectory + LogsPath);
                }
                if (!Directory.Exists(CurrentDirectory + DebugLogsPath))
                {
                    Directory.CreateDirectory(CurrentDirectory + DebugLogsPath);
                }
                if (!Directory.Exists(CurrentDirectory + ErrorLogsPath))
                {
                    Directory.CreateDirectory(CurrentDirectory + ErrorLogsPath);
                }
                if (!Directory.Exists(CurrentDirectory + DatabasePath))
                {
                    Directory.CreateDirectory(CurrentDirectory + DatabasePath);
                }
            }
        }

        /// <summary>
        /// Write a JSON file and output the data passed as parameter.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileData"></param>
        internal static void WriteJson(string fileName, string fileData)
        {
            HandleFolderDirectories();
            var path = CurrentDirectory + DatabasePath + "/" + fileName + ".json";

            File.WriteAllText(path, fileData);
        }

        /// <summary>
        /// Read a JSON file and output the data contained
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static string ReadJSON(string fileName)
        {
            HandleFolderDirectories();

            string retVal = null;
            var path = CurrentDirectory + DatabasePath + "/" + fileName + ".json";
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            else
            {
                Console.WriteLine("Failed to open requested json file {0}.json, no such file!", fileName);
            }
            return retVal;
        }

        /// <summary>
        /// Write data to a CSV file, overwriting the already existing data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileData"></param>
        internal static void WriteCSV(string fileName, string fileData)
        {
            HandleFolderDirectories();
            var path = CurrentDirectory + DatabasePath + "/" + fileName + ".csv";

            File.WriteAllText(path, fileData);
        }

        /// <summary>
        /// Read a CSV file and output a usable form of data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static Dictionary<string, string[]> ReadCSV(string fileName)
        {
            HandleFolderDirectories();

            Dictionary<string, string[]> retVal = null;
            var path = CurrentDirectory + DatabasePath + "/" + fileName + ".csv";
            if (File.Exists(path))
            {
                retVal = new Dictionary<string, string[]>();
                foreach (var line in File.ReadAllLines(path))
                {
                    var lineData = line.Split(',');
                    string[] seperatedData = null;
                    if (lineData.Length > 1)
                    {
                        seperatedData = new string[lineData.Length - 1];
                        for(int i = 0; i < seperatedData.Length; i++)
                        {
                            seperatedData[i] = lineData[i+1];
                        }
                    }
                    retVal.Add(lineData[0], seperatedData);
                }
            }
            else
            {
                Console.WriteLine("Failed to open requested csv file {0}.csv, no such file!", fileName);
            }
            return retVal;
        }

        
    }
}
