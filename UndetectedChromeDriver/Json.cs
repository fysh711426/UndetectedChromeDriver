using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeleniumUndetectedChromeDriver
{
    public static class Json
    {
        public static Dictionary<string, object> DeserializeData(string data)
        {
            var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            if (obj == null)
                throw new Exception("Json data cannot be null.");
            return (Dictionary<string, object>)DeserializeData(obj);
        }

        private static IDictionary<string, object> DeserializeData(JObject data)
        {
            return DeserializeData(
                data.ToObject<Dictionary<string, object>>() ?? new());
        }

        private static IList<object> DeserializeData(JArray data)
        {
            var list = data.ToObject<List<object>>() ?? new();

            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];

                if (value is JObject)
                    list[i] = DeserializeData((JObject)value);

                if (value is JArray)
                    list[i] = DeserializeData((JArray)value);
            }
            return list;
        }

        private static IDictionary<string, object> DeserializeData(IDictionary<string, object> data)
        {
            foreach (var key in data.Keys.ToArray())
            {
                var value = data[key];

                if (value is JObject)
                    data[key] = DeserializeData((JObject)value);

                if (value is JArray)
                    data[key] = DeserializeData((JArray)value);
            }
            return data;
        }
    }
}
