using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumCompat
{
    public static class Extensions
    {
        public static Dictionary<string, object> Update(
            this Dictionary<string, object> dict1, Dictionary<string, object> dict2)
        {
            foreach (var pair in dict2)
            {
                if (pair.Value is Dictionary<string, object>)
                {
                    if (dict1.ContainsKey(pair.Key) &&
                        dict1[pair.Key] is Dictionary<string, object>)
                    {
                        dict1[pair.Key] = (dict1[pair.Key] as Dictionary<string, object>)
                            .Update((pair.Value as Dictionary<string, object>));
                        continue;
                    }
                }
                dict1[pair.Key] = pair.Value;
            }
            return dict1;
        }
    }

    public static class Json
    {
        public static Dictionary<string, object> DeserializeData(string data)
        {
            return DeserializeData(
                JsonConvert.DeserializeObject<Dictionary<string, object>>(data))
                as Dictionary<string, object>;
        }

        private static IDictionary<string, object> DeserializeData(JObject data)
        {
            return DeserializeData(
                data.ToObject<Dictionary<string, object>>());
        }

        private static IDictionary<string, object> DeserializeData(IDictionary<string, object> data)
        {
            foreach (var key in data.Keys.ToArray())
            {
                var value = data[key];

                if (value is JObject)
                    data[key] = DeserializeData(value as JObject);

                if (value is JArray)
                    data[key] = DeserializeData(value as JArray);
            }
            return data;
        }

        private static IList<object> DeserializeData(JArray data)
        {
            var list = data.ToObject<List<object>>();

            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];

                if (value is JObject)
                    list[i] = DeserializeData(value as JObject);

                if (value is JArray)
                    list[i] = DeserializeData(value as JArray);
            }
            return list;
        }
    }
}
