using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage
{
    [System.Serializable]
    public class JSONRewardData
    {
        public string playerName { get; set; }
        public int playerExp { get; set; }
    }
}
