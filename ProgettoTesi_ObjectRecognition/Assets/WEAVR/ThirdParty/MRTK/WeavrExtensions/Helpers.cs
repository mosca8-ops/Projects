#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.Common
{
    using Newtonsoft.Json;
    using System;
    using UnityEngine;

    public class JsonHelper
    {
        public static T[] GetJsonArray<T>(string json) {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonConvert.DeserializeObject<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
}
#endif
