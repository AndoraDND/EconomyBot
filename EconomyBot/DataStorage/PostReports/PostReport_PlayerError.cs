using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage
{
    public enum ReportType : byte 
    {
        Game = 0x0,
        Event = 0x1,
        Rumor = 0x2,
        Unknown = 0xFF
    }

    public class PostReport_PlayerError
    {
        public ReportType ReportType;
        public int ReportRowID;
        public string PlayerName;
        public int ExperienceValue;
        public string ErrorMessage;
        public string StackTrace;

        public PostReport_PlayerError()
        {
            ReportType = ReportType.Unknown;
            ReportRowID = -1;
            PlayerName = "";
            ExperienceValue = 0;
            ErrorMessage = "Unknown";
            StackTrace = "";
        }

        public PostReport_PlayerError(ReportType type, int reportRowID, string playerName, int experienceValue = -1, string errorMessage = "Unknown", string stackTrace = "")
        {
            ReportType = type;
            ReportRowID = reportRowID;
            PlayerName = playerName;
            ExperienceValue = experienceValue;
            ErrorMessage = errorMessage;
            StackTrace = stackTrace;
        }
    }
}
