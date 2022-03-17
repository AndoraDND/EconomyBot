using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage
{
    public class PostEventReport
    {
        public int RowID;

        public DateTime TimeStamp;
        public string MainRunner;
        public List<string> OtherRunners;
        public string EventData;
        public List<string> Participants;
        public int XPAwarded;
        public string ItemRewards;
        public string EventRewards;
        public string MajorStoryDevelopments;
        public string WeeklyRumorBoard;
        public string EventSpecifics;

        public PostEventReport(int rowID)
        {
            RowID = rowID;
        }
    }
}
