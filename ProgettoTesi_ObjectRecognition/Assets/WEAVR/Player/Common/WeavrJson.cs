using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TXT.WEAVR.Player
{
    public static class WeavrJson
    {
        public static object Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static async Task<object> SerializeAsync(object obj)
        {
            return await Task.Run<object>(() => JsonConvert.SerializeObject(obj));
        }

        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        public static async Task<T> DeserializeAsync<T>(string json)
        {
            return await Task.Run(() => JsonConvert.DeserializeObject<T>(json));
        }

        public static async Task<T> DeserializeFileAsync<T>(string filepath)
        {
            return await Task.Run(() => JsonConvert.DeserializeObject<T>(File.ReadAllText(filepath)));
        }
    }
}
