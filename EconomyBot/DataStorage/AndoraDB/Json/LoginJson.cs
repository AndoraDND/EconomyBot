using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage.AndoraDB.Json
{
    public class LoginJson
    {
        public string user;
        public string password;
    }

    public class ResponseJson
    {
        public string message;
        public string token;
    }
}
