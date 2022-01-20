using System;
using System.Collections.Generic;
using System.Text;
using EconomyBot.Commands;

namespace EconomyBot
{
    public class AndoraService
    {
        public string CurrentYear { get { return "100"; } }

        public PriceDatabase PriceDB { get; set; }

        public List<ulong> ElevatedStatusRoles { get; set; }

        public AndoraService()
        {
            PriceDB = new PriceDatabase("PriceDB");

            //Import elevated roles
            ElevatedStatusRoles = new List<ulong>();
            var roleData = DataStorage.FileReader.ReadCSV("ElevatedRoleData");
            foreach(var line in roleData)
            {
                foreach (var role in line.Value)
                {
                    if(ulong.TryParse(role, out var result))
                    {
                        ElevatedStatusRoles.Add(result);
                    }
                }
            }
        }
    }
}
