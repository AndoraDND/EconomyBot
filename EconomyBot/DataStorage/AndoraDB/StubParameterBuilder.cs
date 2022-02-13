using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.DataStorage.AndoraDB
{
    public class StubParameterBuilder
    {
        private List<KeyValuePair<string, string>> _parameters = new List<KeyValuePair<string, string>>();

        public void AddParameter(string key, string value)
        {
            _parameters.Add(new KeyValuePair<string, string>(key, value));
        }

        public void RemoveParameter(string key)
        {
            var index = _parameters.FindIndex(p => p.Key.Equals(key));
            if(index >= 0)
            {
                _parameters.RemoveAt(index);
            }
        }

        public void Clear()
        {
            _parameters.Clear();
        }

        public string Build()
        {
            var retVal = "?";
            for(int i = 0; i < _parameters.Count; i++)
            {
                if (i > 0)
                {
                    retVal += "&";
                }

                retVal += $"{_parameters[i].Key}={_parameters[i].Value}";
            }
            return retVal;
        }
    }
}
