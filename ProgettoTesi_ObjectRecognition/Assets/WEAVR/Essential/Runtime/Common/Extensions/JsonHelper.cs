using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TXT.WEAVR
{
    [Serializable]
    public class JsonArray<T>
    {
        public T[] array;
    }

    [Serializable]
    public class JsonStringArray : JsonArray<string> { }

    public static class JsonHelper
    {
        public static string ToJson<T>(this IEnumerable<T> array)
        {
            return ToJson(array.ToArray());
        }

        public static string ToJson<T>(this T[] array)
        {
            //var jsonArray = new JsonArray()
            //{
            //    array = array.ToArray()
            //};
            return JsonUtility.ToJson(new JsonArray<T>() { array = array });
        }

        public static T[] FromJsonArray<T>(this string jsonString)
        {
            if(string.IsNullOrEmpty(jsonString)) { return default; }

            if(jsonString[0] == '[' && jsonString[jsonString.Length - 1] == ']')
            {
                jsonString = $"{{\"array\":{jsonString}}}";
                var jsonArray = JsonUtility.FromJson<JsonArray<T>>(jsonString);
                return jsonArray.array/*.OfType<T>().ToArray()*/;
            }
            return null;
            //else
            //{
            //    return JsonUtility.FromJson<T>(jsonString);
            //}
        }

        public static T FromJson<T>(this string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString)) { return default; }

            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)) && jsonString[0] == '[' && jsonString[jsonString.Length - 1] == ']')
            {
                throw new ArgumentException("The json contains an array as root, please consider using FromJsonArray<T> method instead");
                //jsonString = $"{{ array: {jsonString} }}";
                //var jsonArray = JsonUtility.FromJson<JsonArray>(jsonString);
                //return (T)jsonArray.array.OfType<object>();
            }
            else
            {
                return JsonUtility.FromJson<T>(jsonString);
            }
        }

        public static object FromJson(this string jsonString, Type objType)
        {
            if (string.IsNullOrEmpty(jsonString)) { return default; }

            jsonString = jsonString.Trim();
            if (typeof(IEnumerable).IsAssignableFrom(objType) && jsonString[0] == '[' && jsonString[jsonString.Length - 1] == ']')
            {
                jsonString = $"{{ array: {jsonString} }}";
                var jsonArray = JsonUtility.FromJson<JsonArray<object>>(jsonString);
                return jsonArray.array.OfType<object>().ToArray();
            }
            else
            {
                return JsonUtility.FromJson(jsonString, objType);
            }
        }
    }
}