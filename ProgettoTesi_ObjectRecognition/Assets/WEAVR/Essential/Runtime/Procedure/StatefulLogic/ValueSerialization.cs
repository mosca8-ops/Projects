using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;

namespace TXT.WEAVR.Procedure
{
    public class ValueSerialization
    {
        public static string Serialize(object value)
        {
            if (value == null)
                return "null";

            string serializedValue = string.Empty;
            Type valueType = value.GetType();

            if (valueType == typeof(bool))
                serializedValue = Serialize((bool)value);

            else if (valueType == typeof(int))
                serializedValue = Serialize((int)value);

            else if (valueType == typeof(uint))
                serializedValue = Serialize((uint)value);

            else if (valueType == typeof(float))
                serializedValue = Serialize((float)value);

            else if (valueType == typeof(string))
                serializedValue = value.ToString();

            else if (valueType == typeof(Color))
                serializedValue = Serialize((Color)value);

            else if (valueType == typeof(Quaternion))
                serializedValue = Serialize((Quaternion)value);

            else if (valueType == typeof(Vector4))
                serializedValue = Serialize((Vector4)value);

            else if (valueType == typeof(Vector3))
                serializedValue = Serialize((Vector3)value);

            else if (valueType == typeof(Vector3Int))
                serializedValue = Serialize((Vector3Int)value);

            else if (valueType == typeof(Vector2))
                serializedValue = Serialize((Vector2)value);

            else if (valueType == typeof(Vector2Int))
                serializedValue = Serialize((Vector2Int)value);

            else if (valueType == typeof(Rect))
                serializedValue = Serialize((Rect)value);

            else if (valueType == typeof(RectInt))
                serializedValue = Serialize((RectInt)value);

            else if (valueType == typeof(Bounds))
                serializedValue = Serialize((Bounds)value);

            else if (valueType.IsEnum)
                serializedValue = JsonConvert.SerializeObject(value);

            return serializedValue;
        }

        public static object Deserialize(string value, Type valueType)
        {
            object deserializedObject = null;

            if (valueType == typeof(bool))
                deserializedObject = Deserialize(value, default(bool));

            else if (valueType == typeof(int))
                deserializedObject = Deserialize(value, default(int));

            else if (valueType == typeof(uint))
                deserializedObject = Deserialize(value, default(uint));

            else if (valueType == typeof(float))
                deserializedObject = Deserialize(value, default(float));

            else if (valueType == typeof(string))
                deserializedObject = value;

            else if (valueType == typeof(Color))
                deserializedObject = Deserialize(value, default(Color));

            else if (valueType == typeof(Quaternion))
                deserializedObject = Deserialize(value, default(Quaternion));

            else if (valueType == typeof(Vector4))
                deserializedObject = Deserialize(value, default(Vector4));

            else if (valueType == typeof(Vector3))
                deserializedObject = Deserialize(value, default(Vector3));

            else if (valueType == typeof(Vector3Int))
                deserializedObject = Deserialize(value, default(Vector3Int));

            else if (valueType == typeof(Vector2))
                deserializedObject = Deserialize(value, default(Vector2));

            else if (valueType == typeof(Vector2Int))
                deserializedObject = Deserialize(value, default(Vector2Int));

            else if (valueType == typeof(Rect))
                deserializedObject = Deserialize(value, default(Rect));

            else if (valueType == typeof(RectInt))
                deserializedObject = Deserialize(value, default(RectInt));

            else if (valueType == typeof(Bounds))
                deserializedObject = Deserialize(value, default(Bounds));

            else if (valueType.IsEnum)
                deserializedObject = JsonConvert.DeserializeObject(value, valueType);

            return deserializedObject;
        }

        #region [  VALUES SERIALIZATION  ]
        // BOOL
        private static string Serialize(bool value) => value.ToString();
        private static bool Deserialize(string value, bool fallbackValue) => bool.TryParse(value, out bool result) ? result : fallbackValue;

        // INTEGER 
        private static string Serialize(int value) => value.ToString();
        private static int Deserialize(string value, int fallbackValue) => int.TryParse(value, out int result) ? result : fallbackValue;

        // UNSIGNED INTEGER
        private static string Serialize(uint value) => value.ToString();
        private static uint Deserialize(string value, uint fallbackValue) => uint.TryParse(value, out uint result) ? result : fallbackValue;

        // FLOAT
        private static string Serialize(float value) => value.ToString("0.000000", CultureInfo.InvariantCulture);
        private static float Deserialize(string value, float fallbackValue) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : fallbackValue;

        // COLOR 
        private static string Serialize(Color value) => "#" + ColorUtility.ToHtmlStringRGBA(value);
        private static Color Deserialize(string value, Color fallbackValue) => ColorUtility.TryParseHtmlString(value, out Color result) ? result : fallbackValue;

        // QUATERNION
        private static string Serialize(Quaternion value) => $"Q({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)}, {Serialize(value.w)})";
        private static Quaternion Deserialize(string value, Quaternion fallbackValue)
        {
            fallbackValue = ValidQuaternion(fallbackValue, Quaternion.identity);
            try
            {
                if (!value.StartsWith("Q(")) { return fallbackValue; }

                var splits = value.Substring(2, value.Length - 3).Split(',');
                return ValidQuaternion(new Quaternion(Deserialize(splits[0], fallbackValue.x),
                                 Deserialize(splits[1], fallbackValue.y),
                                 Deserialize(splits[2], fallbackValue.z),
                                 Deserialize(splits[3], fallbackValue.w)), fallbackValue);
            }
            catch
            {
                return fallbackValue;
            }
        }
        private static Quaternion ValidQuaternion(Quaternion q, Quaternion fallback) => q.x == 0 && q.y == 0 && q.z == 0 && q.w == 0 ? fallback : q;

        // VECTOR 4
        private static string Serialize(Vector4 value) => $"V4({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)}, {Serialize(value.w)})";
        private static Vector4 Deserialize(string value, Vector4 fallbackValue)
        {
            try
            {
                if (!value.StartsWith("V4(")) { return fallbackValue; }

                var splits = value.Substring(3, value.Length - 4).Split(',');
                return new Vector4(Deserialize(splits[0], fallbackValue.x),
                                 Deserialize(splits[1], fallbackValue.y),
                                 Deserialize(splits[2], fallbackValue.z),
                                 Deserialize(splits[3], fallbackValue.w));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // VECTOR 3
        private static string Serialize(Vector3 value) => $"V3({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)})";
        private static Vector3 Deserialize(string value, Vector3 fallbackValue)
        {
            try
            {
                if (!value.StartsWith("V3(")) { return fallbackValue; }

                var splits = value.Substring(3, value.Length - 4).Split(',');
                return new Vector3(Deserialize(splits[0], fallbackValue.x),
                                 Deserialize(splits[1], fallbackValue.y),
                                 Deserialize(splits[2], fallbackValue.z));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // VECTOR 3 INT
        private static string Serialize(Vector3Int value) => $"V3I({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)})";
        private static Vector3Int Deserialize(string value, Vector3Int fallbackValue)
        {
            try
            {
                if (!value.StartsWith("V3I(")) { return fallbackValue; }

                var splits = value.Substring(4, value.Length - 5).Split(',');
                return new Vector3Int(Deserialize(splits[0], fallbackValue.x),
                                 Deserialize(splits[1], fallbackValue.y),
                                 Deserialize(splits[2], fallbackValue.z));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // VECTOR 2
        private static string Serialize(Vector2 value) => $"V2({Serialize(value.x)}, {Serialize(value.y)})";
        private static Vector2 Deserialize(string value, Vector2 fallbackValue)
        {
            try
            {
                if (!value.StartsWith("V2(")) { return fallbackValue; }

                var splits = value.Substring(3, value.Length - 4).Split(',');
                return new Vector2(Deserialize(splits[0], fallbackValue.x),
                                 Deserialize(splits[1], fallbackValue.y));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // VECTOR 2 INT
        private static string Serialize(Vector2Int value) => $"V2I({Serialize(value.x)}, {Serialize(value.y)})";
        private static Vector2Int Deserialize(string value, Vector2Int fallbackValue)
        {
            try
            {
                if (!value.StartsWith("V2I(")) { return fallbackValue; }

                var splits = value.Substring(4, value.Length - 5).Split(',');
                return new Vector2Int(Deserialize(splits[0], fallbackValue.x),
                                 Deserialize(splits[1], fallbackValue.y));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // RECT
        private static string Serialize(Rect value) => $"R({Serialize(value.position)}; {Serialize(value.size)})";
        private static Rect Deserialize(string value, Rect fallbackValue)
        {
            try
            {
                if (!value.StartsWith("R(")) { return fallbackValue; }

                var splits = value.Substring(2, value.Length - 3).Split(';');
                return new Rect(Deserialize(splits[0], fallbackValue.position),
                                 Deserialize(splits[1], fallbackValue.size));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // RECT INT
        private static string Serialize(RectInt value) => $"RI({Serialize(value.position)}; {Serialize(value.size)})";
        private static RectInt Deserialize(string value, RectInt fallbackValue)
        {
            try
            {
                if (!value.StartsWith("RI(")) { return fallbackValue; }

                var splits = value.Substring(3, value.Length - 4).Split(';');
                return new RectInt(Deserialize(splits[0], fallbackValue.position),
                                 Deserialize(splits[1], fallbackValue.size));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // BOUNDS
        private static string Serialize(Bounds value) => $"B({Serialize(value.center)}; {Serialize(value.size)})";
        private static Bounds Deserialize(string value, Bounds fallbackValue)
        {
            try
            {
                if (!value.StartsWith("B(")) { return fallbackValue; }

                var splits = value.Substring(2, value.Length - 3).Split(';');
                return new Bounds(Deserialize(splits[0], fallbackValue.center),
                                 Deserialize(splits[1], fallbackValue.size));
            }
            catch
            {
                return fallbackValue;
            }
        }

        // OBJECT REF
        /*private static string Serialize(Object value, Context context)
        {
            if(value is GameObject go)
            {
                int goIndex = context.GetGameObjectID(go);
                return $"GO({goIndex})";
            }
            else if(value is Component c)
            {
                int goIndex = context.GetGameObjectID(c.gameObject);
                

            }
            return $"[null]";
        }*/

        /*private static Object Deserialize(string value, Context context, Object fallbackValue)
        {
            try
            {
                if (!value.StartsWith("B(")) { return fallbackValue; }

                var splits = value.Substring(2, value.Length - 3).Split(';');
                return null;
                //return new Object(Deserialize(splits[0], fallbackValue.center),
                //                 Deserialize(splits[1], fallbackValue.size));
            }
            catch
            {
                return fallbackValue;
            }
        }*/
        #endregion
    }
}
