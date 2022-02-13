using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage.AndoraDB.Json
{
    public class NewFactionJson
    {
        public string name { get; set; }
        public int cap { get; set; }
    }

    public class FactionJson
    {
        public int id { get; set; }
        public string name { get; set; }
        public int cap { get; set; }
    }
}
