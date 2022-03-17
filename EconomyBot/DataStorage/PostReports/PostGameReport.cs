using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage
{
    public class PostGameReport
    {
        public int RowID; 

        public DateTime TimeStamp;
        public string DMName;
        public DateTime MissionRunDate;
        public List<string> Players;
        public string ItemResults;
        public string StoryDevelopments;
        public bool MainStoryRelated;
        public string WeeklyRumorBoard;
        public string SessionSpecifics;

        public string JSON;

        public PostGameReport(int rowID)
        {
            RowID = rowID;
        }
    }
}
