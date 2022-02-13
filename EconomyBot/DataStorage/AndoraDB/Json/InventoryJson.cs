using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage.AndoraDB.Json
{
    public class Item
    {
        public string name { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
    }

    public class MagicalItem
    {
        public string name { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
    }

    public class InventoryJson
    {
        public string id { get; set; }
        public int gold { get; set; }
        public List<Item> items { get; set; }
        public List<MagicalItem> magical_items { get; set; }
    }

    public class AddItemsJson
    {
        public Item[] items { get; set; }
        public MagicalItem[] magical_items { get; set; }
    }
}
