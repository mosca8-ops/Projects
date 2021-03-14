using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    //public class ValueSerialization
    //{
    //    #region [  VALUES SERIALIZATION  ]

    //    // BOOL
    //    private static string Serialize(bool value) => value.ToString();
    //    private static bool Deserialize(string value, bool fallbackValue) => bool.TryParse(value, out bool result) ? result : fallbackValue;

    //    // INTEGER 
    //    private static string Serialize(int value) => value.ToString();
    //    private static int Deserialize(string value, int fallbackValue) => int.TryParse(value, out int result) ? result : fallbackValue;

    //    // FLOAT
    //    private static string Serialize(float value) => value.ToString(CultureInfo.InvariantCulture);
    //    private static float Deserialize(string value, float fallbackValue) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : fallbackValue;

    //    // COLOR 
    //    private static string Serialize(Color value) => $"C({Serialize(value.r)}, {Serialize(value.g)}, {Serialize(value.b)}, {Serialize(value.a)})";
    //    private static Color Deserialize(string value, Color fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("C(")) { return fallbackValue; }

    //            var splits = value.Substring(2, value.Length - 3).Split(',');
    //            return new Color(Deserialize(splits[0], fallbackValue.r),
    //                             Deserialize(splits[1], fallbackValue.g),
    //                             Deserialize(splits[2], fallbackValue.b),
    //                             Deserialize(splits[3], fallbackValue.a));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // QUATERNION
    //    private static string Serialize(Quaternion value) => $"Q({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)}, {Serialize(value.w)})";
    //    private static Quaternion Deserialize(string value, Quaternion fallbackValue)
    //    {
    //        fallbackValue = ValidQuaternion(fallbackValue, Quaternion.identity);
    //        try
    //        {
    //            if (!value.StartsWith("Q(")) { return fallbackValue; }

    //            var splits = value.Substring(2, value.Length - 3).Split(',');
    //            return ValidQuaternion(new Quaternion(Deserialize(splits[0], fallbackValue.x),
    //                             Deserialize(splits[1], fallbackValue.y),
    //                             Deserialize(splits[2], fallbackValue.z),
    //                             Deserialize(splits[3], fallbackValue.w)), fallbackValue);
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }
    //    private static Quaternion ValidQuaternion(Quaternion q, Quaternion fallback) => q.x == 0 && q.y == 0 && q.z == 0 && q.w == 0 ? fallback : q;

    //    // VECTOR 4
    //    private static string Serialize(Vector4 value) => $"V4({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)}, {Serialize(value.w)})";
    //    private static Vector4 Deserialize(string value, Vector4 fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("V4(")) { return fallbackValue; }

    //            var splits = value.Substring(3, value.Length - 4).Split(',');
    //            return new Vector4(Deserialize(splits[0], fallbackValue.x),
    //                             Deserialize(splits[1], fallbackValue.y),
    //                             Deserialize(splits[2], fallbackValue.z),
    //                             Deserialize(splits[3], fallbackValue.w));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // VECTOR 3
    //    private static string Serialize(Vector3 value) => $"V3({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)})";
    //    private static Vector3 Deserialize(string value, Vector3 fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("V3(")) { return fallbackValue; }

    //            var splits = value.Substring(3, value.Length - 4).Split(',');
    //            return new Vector3(Deserialize(splits[0], fallbackValue.x),
    //                             Deserialize(splits[1], fallbackValue.y),
    //                             Deserialize(splits[2], fallbackValue.z));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // VECTOR 3 INT
    //    private static string Serialize(Vector3Int value) => $"V3I({Serialize(value.x)}, {Serialize(value.y)}, {Serialize(value.z)})";
    //    private static Vector3Int Deserialize(string value, Vector3Int fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("V3I(")) { return fallbackValue; }

    //            var splits = value.Substring(4, value.Length - 5).Split(',');
    //            return new Vector3Int(Deserialize(splits[0], fallbackValue.x),
    //                             Deserialize(splits[1], fallbackValue.y),
    //                             Deserialize(splits[2], fallbackValue.z));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // VECTOR 2
    //    private static string Serialize(Vector2 value) => $"V2({Serialize(value.x)}, {Serialize(value.y)})";
    //    private static Vector2 Deserialize(string value, Vector2 fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("V2(")) { return fallbackValue; }

    //            var splits = value.Substring(3, value.Length - 4).Split(',');
    //            return new Vector2(Deserialize(splits[0], fallbackValue.x),
    //                             Deserialize(splits[1], fallbackValue.y));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // VECTOR 2 INT
    //    private static string Serialize(Vector2Int value) => $"V2I({Serialize(value.x)}, {Serialize(value.y)})";
    //    private static Vector2Int Deserialize(string value, Vector2Int fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("V2I(")) { return fallbackValue; }

    //            var splits = value.Substring(4, value.Length - 5).Split(',');
    //            return new Vector2Int(Deserialize(splits[0], fallbackValue.x),
    //                             Deserialize(splits[1], fallbackValue.y));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // RECT
    //    private static string Serialize(Rect value) => $"R({Serialize(value.position)}; {Serialize(value.size)})";
    //    private static Rect Deserialize(string value, Rect fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("R(")) { return fallbackValue; }

    //            var splits = value.Substring(2, value.Length - 3).Split(';');
    //            return new Rect(Deserialize(splits[0], fallbackValue.position),
    //                             Deserialize(splits[1], fallbackValue.size));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // RECT INT
    //    private static string Serialize(RectInt value) => $"RI({Serialize(value.position)}; {Serialize(value.size)})";
    //    private static RectInt Deserialize(string value, RectInt fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("RI(")) { return fallbackValue; }

    //            var splits = value.Substring(3, value.Length - 4).Split(';');
    //            return new RectInt(Deserialize(splits[0], fallbackValue.position),
    //                             Deserialize(splits[1], fallbackValue.size));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    // BOUNDS
    //    private static string Serialize(Bounds value) => $"B({Serialize(value.center)}; {Serialize(value.size)})";
    //    private static Bounds Deserialize(string value, Bounds fallbackValue)
    //    {
    //        try
    //        {
    //            if (!value.StartsWith("B(")) { return fallbackValue; }

    //            var splits = value.Substring(2, value.Length - 3).Split(';');
    //            return new Bounds(Deserialize(splits[0], fallbackValue.center),
    //                             Deserialize(splits[1], fallbackValue.size));
    //        }
    //        catch
    //        {
    //            return fallbackValue;
    //        }
    //    }

    //    #endregion
    //}
}
