using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Net.Http.Json;
using EconomyBot.DataStorage.AndoraDB.Json;
using EconomyBot.DataStorage.AndoraDB;

namespace EconomyBot.DataStorage
{
    public class AndoraDatabase
    {
        private const string Database_URI = "http://hastur-lb-168364296.us-east-1.elb.amazonaws.com:1234";

        private HttpClient client;

        private string andoraDB_User;
        private string andoraDB_Pass;

        private string _cachedJWTToken;

        public AndoraDatabase(TokenCredentials credentials)
        {
            client = new HttpClient();

            andoraDB_User = credentials.AndoraDB_User;
            andoraDB_Pass = credentials.AndoraDB_Pass;
        }

        /// <summary>
        /// Refresh the credentials associated with this service. This is only necessary once per week.
        /// </summary>
        /// <returns></returns>
        public async Task RefreshLoginCredentials()
        {
            _cachedJWTToken = null;

            await Login(andoraDB_User, andoraDB_Pass);
        }

        /// <summary>
        /// Login and cache a JWT token from the database.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> Login(string user = null, string password = null)
        {
            if(user == null)
            {
                user = andoraDB_User;
            }
            if (password == null)
            {
                password = andoraDB_Pass;
            }

            var api_stub = "/login";
            //var pass = GetHashedPassword(password); //DEPRECATED. HASHING IS HANDLED ON THE BACKEND, OUR SERVICE SITS IN THE SAME PRIVATE NETWORK.

            var loginObj = new LoginJson() { user = user, password = password };
            var json = JsonConvert.SerializeObject(loginObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await client.PostAsync(Database_URI + api_stub, content);

                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    _cachedJWTToken = JsonConvert.DeserializeObject<ResponseJson>((string)(await response.Content.ReadAsStringAsync())).token;
                    Console.WriteLine("Successfully logged into Andora Database.");
                    return true;
                }
                else
                {
                    Console.WriteLine("Error: Failed to log into Andora Database!");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Failed to log into Andora Database! {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Take an inputted cleartext password and translate it into a Hashed string.
        /// WARNING: THIS IS DEPRECATED
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        private string GetHashedPassword(string password)
        {
            StringBuilder sb = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(password));

                foreach (var b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }

            return sb.ToString();
        }

        public async Task Get_APIHealth()
        {
            var api_stub = "/healthz";

            var response = await client.GetAsync(Database_URI + api_stub);
            try
            {
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"API Health: {response.Content.ToString()}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error retrieving API health: {e.Message}");
            }
        }

        /// <summary>
        /// Attempt to create a new account in the andora database. At the moment this is not used, but may be used further in the future.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private async Task<bool> Post_Signup(string user, string password)
        {
            var api_stub = "/signup";

            var signupObj = new LoginJson() { user = user, password = password };
            var json = JsonConvert.SerializeObject(signupObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(Database_URI + api_stub, content);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get a list of all factions held within the database.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FactionJson>> Get_AllFactions()
        {
            var api_stub = "/factions";
            var response = await client.GetAsync(Database_URI + api_stub);

            try
            {
                response.EnsureSuccessStatusCode();
                var retVal = JsonConvert.DeserializeObject<List<FactionJson>>(await response.Content.ReadAsStringAsync());
                return retVal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the data for a specific faction.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<FactionJson> Get_Faction(ulong? id = null, string name = null)
        {
            var api_stub = "/faction";

            if (id == null && name == null)
                return null;

            #region Build API Stub
            var stubBuilder = new StubParameterBuilder();
            if(id.HasValue)
            {
                stubBuilder.AddParameter("id", id.Value.ToString());
            }
            if(name != null && name.Length > 0)
            {
                stubBuilder.AddParameter("name", name);
            }
            api_stub += stubBuilder.Build();
            #endregion

            var response = await client.GetAsync(Database_URI + api_stub);
            try
            {
                response.EnsureSuccessStatusCode();
                var retVal = JsonConvert.DeserializeObject<FactionJson>(await response.Content.ReadAsStringAsync());
                return retVal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Add a faction to the database. Will be useful when new factions are added to the world.
        /// </summary>
        /// <param name="jsonObj"></param>
        /// <returns></returns>
        public async Task<bool> Post_AddFaction(NewFactionJson jsonObj)
        {
            var api_stub = "/faction";

            var json = JsonConvert.SerializeObject(jsonObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);

            var response = await client.PostAsync(Database_URI + api_stub, content);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to add faction to Andora DB! : {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update a faction within the Database. This is probably unnecessary.
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        public async Task<bool> Patch_UpdateFaction(FactionJson faction)
        {
            var api_stub = "/faction";

            var json = JsonConvert.SerializeObject(faction);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);
            var response = await client.PatchAsync(Database_URI + api_stub, content);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to update faction in Andora DB! : {e.Message}");
                return false;
            }
        }

        //public async Task<bool> Delete_DeleteFaction(int? id = null) { }

        /// <summary>
        /// Get the data for a Character within the database. Will prefer ID based lookups before name based lookups.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<CharacterDataJson> Get_FindCharacterData(ulong? id = null, string name = null)
        {
            var api_stub = "/characters";

            if (id == null && name == null)
                return null;

            api_stub += "?";
            if (id.HasValue)
            {
                api_stub += $"id={id.Value}";
            }
            else if (name != null && name.Length > 0)
            {
                api_stub += $"name={name}";
            }

            var response = await client.GetAsync(Database_URI + api_stub);
            try
            {
                response.EnsureSuccessStatusCode();
                var retVal = JsonConvert.DeserializeObject<CharacterDataJson>(await response.Content.ReadAsStringAsync());
                return retVal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Poll for all characters of a given type from the database. Useful for sorting and data collection.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="race"></param>
        /// <param name="faction"></param>
        /// <param name="className"></param>
        /// <param name="region"></param>
        /// <param name="last_played"></param>
        /// <returns></returns>
        public async Task<List<CharacterDataJson>> Get_CharactersOfType(int? level = null,
            string race = null,
            string faction = null,
            string className = null,
            string region = null,
            bool last_played = false)
        {
            var api_stub = "/characters";

            if (level == null && race == null && faction == null && region == null && last_played == false)
                return null;

            #region Create API stub
            bool firstAssigned = false;
            api_stub += "?";
            if (level.HasValue)
            {
                api_stub += $"level={level.Value}";
                if (!firstAssigned)
                {
                    firstAssigned = true;
                }
            }
            if (race != null && race.Length > 0)
            {
                if (firstAssigned)
                {
                    api_stub += "&";
                }
                else
                {
                    firstAssigned = true;
                }

                api_stub += $"race={race}";
            }
            if (faction != null && faction.Length > 0)
            {
                if (firstAssigned)
                {
                    api_stub += "&";
                }
                else
                {
                    firstAssigned = true;
                }

                api_stub += $"faction={faction}";
            }
            if (className != null && className.Length > 0)
            {
                if (firstAssigned)
                {
                    api_stub += "&";
                }
                else
                {
                    firstAssigned = true;
                }

                api_stub += $"class={className}";
            }
            if (region != null && region.Length > 0)
            {
                if (firstAssigned)
                {
                    api_stub += "&";
                }
                else
                {
                    firstAssigned = true;
                }

                api_stub += $"region={region}";
            }
            if (last_played)
            {
                if (firstAssigned)
                {
                    api_stub += "&";
                }
                else
                {
                    firstAssigned = true;
                }
                api_stub += "last_played";
            }
            #endregion

            var response = await client.GetAsync(Database_URI + api_stub);
            try
            {
                response.EnsureSuccessStatusCode();
                var retVal = JsonConvert.DeserializeObject<List<CharacterDataJson>>(await response.Content.ReadAsStringAsync());
                return retVal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Add a character to the database.
        /// </summary>
        /// <param name="jsonObj"></param>
        /// <returns>True if successful. False if failure.</returns>
        public async Task<bool> Post_AddCharacter(CharacterDataJson jsonObj)
        {
            var api_stub = "/characters";

            var json = JsonConvert.SerializeObject(jsonObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);

            var response = await client.PostAsync(Database_URI + api_stub, content);

            Console.WriteLine($"Posting new character data. : {api_stub} - {json}");

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to add character to Andora DB! : {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update a character currently within the database. Character to update is automatically determined.
        /// </summary>
        /// <param name="jsonObj"></param>
        /// <returns>True if successful. false if failure.</returns>
        public async Task<bool> Patch_UpdateCharacter(CharacterDataJson jsonObj)
        {
            var api_stub = "/characters";

            var json = JsonConvert.SerializeObject(jsonObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);

            var response = await client.PatchAsync(Database_URI + api_stub, content);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to update character in Andora DB! : {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Delete a character within the database. Warning: This is dangerous and should not be used unless necessary.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> Delete_Character(ulong id)
        {
            var api_stub = "/characters";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", id.ToString());
            api_stub += stubBuilder.Build(); //$"?id={id}";

            //var content = new StringContent(id.ToString(), Encoding.UTF8, "application/json");
            //content.Headers.Add("Token", _cachedJWTToken);

            var request = new HttpRequestMessage(HttpMethod.Delete, Database_URI + api_stub);
            request.Headers.Add("Token", _cachedJWTToken);
            var response = await client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to delete character from Andora DB! : {e.Message}");
                return false;
            }
        }

        //public async Task<List<InventoryJson>> Get_AllInventories() { }

        /// <summary>
        /// TODO: Fix this when the backend service is updated with a new README
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<InventoryJson> Get_Inventory(ulong id)
        {
            var api_stub = "/inventory";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", id.ToString());
            api_stub += stubBuilder.Build(); //$"?id={id}";

            var response = await client.GetAsync(Database_URI + api_stub);
            try
            {
                response.EnsureSuccessStatusCode();
                var retVal = JsonConvert.DeserializeObject<InventoryJson>(await response.Content.ReadAsStringAsync());
                return retVal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// TODO: Fix this when the backend service is updated with a new README
        /// </summary>
        /// <param name="inventory"></param>
        /// <returns></returns>
        public async Task<bool> Post_AddInventory(InventoryJson inventory)
        {
            var api_stub = "/inventory";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", inventory.id);
            api_stub += stubBuilder.Build(); //$"?id={inventory.id}";

            var json = JsonConvert.SerializeObject(inventory);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);

            var response = await client.PostAsync(Database_URI + api_stub, content);
            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// TODO: Fix this when the backend service is updated with a new README
        /// </summary>
        /// <param name="inventory"></param>
        /// <returns></returns>
        public async Task<bool> Delete_Inventory(InventoryJson inventory)
        {
            var api_stub = "/inventory";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", inventory.id);
            api_stub += stubBuilder.Build(); //$"?id={inventory.id}";

            var request = new HttpRequestMessage(HttpMethod.Delete, Database_URI + api_stub);
            request.Headers.Add("Token", _cachedJWTToken);
            var response = await client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to delete inventory from Andora DB! : {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add gold to a specific character's bank.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<bool> Patch_AddGold(ulong id, int amount)
        {
            var api_stub = "/gold";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", id.ToString());
            stubBuilder.AddParameter("gold", amount.ToString());
            api_stub += stubBuilder.Build(); //$"?id={id}&gold={amount}";

            var content = new StringContent("", Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);

            var response = await client.PatchAsync(Database_URI + api_stub, content);
            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// Add items to a character's inventory. WARNING: This can only handle either mundane or magical items, not both at the same time. 
        /// This is a restriction posed by the backend. 
        /// If there is a need to handle both magical items and mundane items, make two calls to this function.
        /// </summary>
        /// <param name="inventoryID">Player Discord ID</param>
        /// <param name="items">A list of mundane items to add. NOTE: ONLY PICK THIS, OR MAGICAL ITEMS. NOT BOTH.</param>
        /// <param name="magic_items">A list of magical items to add. NOTE: ONLY PICK THIS, OR MUNDANE ITEMS. NOT BOTH.</param>
        /// <returns></returns>
        public async Task<bool> Patch_AddItemsToInventory(ulong inventoryID, List<Item> items = null, List<MagicalItem> magic_items = null)
        {
            var api_stub = "/items";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", inventoryID.ToString());
            api_stub += stubBuilder.Build(); //$"?id={inventoryID}";
            
            var itemsObj = new AddItemsJson();
            if(items != null)
            {
                itemsObj.items = items.ToArray();
            }
            else if (magic_items != null)
            {
                itemsObj.magical_items = magic_items.ToArray();
            }
            else
            {
                return false;
            }

            var json = JsonConvert.SerializeObject(itemsObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);

            var response = await client.PatchAsync(Database_URI + api_stub, content);
            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// Remvove a mundane item from a player's bank. TODO: Fix this when the backend service is updated with a new README.
        /// </summary>
        /// <param name="inventoryID"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public async Task<bool> Delete_RemoveItemFromInventory(ulong inventoryID, string itemName)
        {
            var api_stub = "/items";

            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", inventoryID.ToString());
            stubBuilder.AddParameter("name", itemName);
            api_stub += stubBuilder.Build(); //$"?id={inventoryID}&name={itemName}";

            var request = new HttpRequestMessage(HttpMethod.Delete, Database_URI + api_stub);
            request.Headers.Add("Token", _cachedJWTToken);
            var response = await client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to delete item from inventory in Andora DB! : {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove a magical item from a player's bank. TODO: Fix this when the backend service is updated with a new README.
        /// </summary>
        /// <param name="inventoryID"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public async Task<bool> Delete_RemoveMagicItemFromInventory(ulong inventoryID, string itemName)
        {
            var api_stub = "/magic_items";

            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", inventoryID.ToString());
            stubBuilder.AddParameter("name", itemName);
            api_stub += stubBuilder.Build(); //$"?id={inventoryID}&name={itemName}";

            var request = new HttpRequestMessage(HttpMethod.Delete, Database_URI + api_stub);
            request.Headers.Add("Token", _cachedJWTToken);
            var response = await client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to delete magic item from inventory in Andora DB! : {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get DTDs remaining for a specific character
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DTDJson> Get_DTD(ulong id)
        {
            var api_stub = "/dtds";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", id.ToString());
            api_stub += stubBuilder.Build(); //$"?id={id}";

            try
            {
                var response = await client.GetAsync(Database_URI + api_stub);

                response.EnsureSuccessStatusCode();
                var retVal = JsonConvert.DeserializeObject<DTDJson>(await response.Content.ReadAsStringAsync());
                return retVal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Update the remaining DTDs for a character.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<DTDJson> Patch_DTD(ulong id, int amount)
        {
            var api_stub = "/dtds";
            var stubBuilder = new StubParameterBuilder();
            stubBuilder.AddParameter("id", id.ToString());
            stubBuilder.AddParameter("dtds", amount.ToString());
            api_stub += stubBuilder.Build(); //$"?id={id}&gold={amount}";

            var content = new StringContent("", Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);

            try
            {
                var response = await client.PatchAsync(Database_URI + api_stub, content);
                response.EnsureSuccessStatusCode();

                var JSONObj = JsonConvert.DeserializeObject<DTDJson>(await response.Content.ReadAsStringAsync());
                return JSONObj;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Reset the DTDs remaining for all players in the database.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Post_ResetDTD(int amount = 7)
        {
            var api_stub = "/dtds";
            if (amount != 7)
            {
                var stubBuilder = new StubParameterBuilder();
                stubBuilder.AddParameter("dtds", amount.ToString());
                api_stub += stubBuilder.Build();
            }

            var content = new StringContent("", Encoding.UTF8, "application/json");
            content.Headers.Add("Token", _cachedJWTToken);
            
            try
            {
                var response = await client.PostAsync(Database_URI + api_stub, content);

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Error attempting to reset DTD in Andora DB! : {e.Message}");
                return false;
            }
        }
    }
}
