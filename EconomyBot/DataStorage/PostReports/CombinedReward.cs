using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace EconomyBot.DataStorage
{
    public class CombinedReward
    {
        public int ReportID;
        public IUser DiscordUser;
        public DateTime LastPlayedDate;
        public int XPValue = 0;
    }
}
