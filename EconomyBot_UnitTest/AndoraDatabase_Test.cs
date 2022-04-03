using System;
using System.IO;
using EconomyBot.DataStorage;
using EconomyBot.DataStorage.AndoraDB.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace EconomyBot_UnitTest
{
    [TestClass]
    public class AndoraDatabase_Test
    {
        private TokenCredentials _credentials;
        private AndoraDatabase _database;

        private CharacterDataJson _testCharacter;

        [TestInitialize]
        public void Initialize()
        {
            LoadProgramData();

            _database = new AndoraDatabase(_credentials);

            _testCharacter = new CharacterDataJson()
            {
                id = ulong.MaxValue.ToString(),
                exp = 3,
                @class = "Sorcerer",
                dtds = 7,
                faction = "Windwalkers (Rovgaz)",
                last_played = $"{DateTime.Now.Month.ToString("00")}-{DateTime.Now.Day.ToString("00")}-{DateTime.Now.Year}",
                last_exp_earned_date = $"{DateTime.Now.Month.ToString("00")}-{DateTime.Now.Day.ToString("00")}-{DateTime.Now.Year}",
                name = "Test Character",
                race = "Human",
                region = "Hashan",
                level = 2,
                sheet = "nullvaluehere"
            };
        }

        private void LoadProgramData()
        {
            //if (!Directory.Exists(Directory.GetCurrentDirectory() + "/Data"))
            //{
            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/Data");
            //}

            if (File.Exists("Data/TokenCredentials.json"))
            {
                _credentials = JsonConvert.DeserializeObject<TokenCredentials>(File.ReadAllText("Data/TokenCredentials.json"));
            }
            else
            {
                _credentials = new TokenCredentials()
                {
                    Bot_Token = ""
                };
                File.WriteAllText("Data/TokenCredentials.json", JsonConvert.SerializeObject(_credentials));
            }
        }

        [TestMethod]
        public void Test_Login()
        {
            Assert.IsTrue(_database.Login().Result);
        }

        [TestMethod]
        public void Test_Get1_CreateCharacter()
        {
            if (_database.Login().Result)
            {
                Assert.IsTrue(_database.Post_AddCharacter(_testCharacter).Result);
            }
            else
            {
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        public void Test_Get2_PollCharacter()
        {
            if (_database.Login().Result)
            {
                var response = _database.Get_FindCharacterData(ulong.Parse(_testCharacter.id)).Result;

                Assert.IsTrue(response.Equals(_testCharacter));
            }
            else
            {
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        public void Test_Get3_PatchCharacter()
        {
            var updatedChar = new CharacterDataJson()
            {
                @class = _testCharacter.@class,
                dtds = _testCharacter.dtds,
                id = _testCharacter.id,
                exp = _testCharacter.exp,
                faction = _testCharacter.faction,
                name = "Updated Name",
                level = _testCharacter.level,
                race = _testCharacter.race,
                region = _testCharacter.region,
                sheet = _testCharacter.sheet,
                last_exp_earned_date = _testCharacter.last_exp_earned_date,
                last_played = _testCharacter.last_played
            };

            if (_database.Login().Result)
            {
                var response = _database.Patch_UpdateCharacter(updatedChar).Result;
                Assert.IsTrue(response);

                var newResponse = _database.Get_FindCharacterData(ulong.Parse(_testCharacter.id)).Result;
                Assert.IsTrue(newResponse.Equals(updatedChar));
            }
            else
            {
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        public void Test_Get4_DeleteCharacter()
        {
            if (_database.Login().Result)
            {
                Assert.IsTrue(_database.Delete_Character(ulong.Parse(_testCharacter.id)).Result);
            }
            else
            {
                Assert.IsTrue(false);
            }
        }
    }
}
