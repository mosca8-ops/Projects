namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using TXT.WEAVR.EditorBridge;
    using TXT.WEAVR.Utility;
    using UnityEngine;

    public static class PropertyConvert
    {

        /// <summary>
        /// Initializes and saves all property converters
        /// </summary>
        static PropertyConvert() {
            RegisterPropertyConverters();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void RegisterPropertyConverters() {
            Property.AddConverter<bool>(o => ToBoolean(o));
            Property.AddConverter<int>(o => ToInt(o));
            Property.AddConverter<float>(o => ToFloat(o));
            Property.AddConverter<double>(o => ToDouble(o));
            Property.AddConverter<string>(o => o?.ToString());
            Property.AddConverter<Vector2>(o => ToVector2(o));
            Property.AddConverter<Vector3>(o => ToVector3(o));
            Property.AddConverter<Vector4>(o => ToVector4(o));
            Property.AddConverter<Color>(o => ToColor(o));
            Property.AddConverter<Enum>(o => ToEnum(o));

            Property.SetUnityObjectConverter(o => ToUnityObject(o));
        }

        public static Enum ToEnum(object value, Type type = null)
        {
            if (value != null)
            {
                if (value is Enum || value.GetType().IsEnum())
                {
                    return value as Enum;
                }
                else if (type != null && type.IsEnum())
                {
                    try
                    {
                        return Enum.Parse(type, value.ToString()) as Enum;
                    }
                    catch (ArgumentException)
                    {
                        return Enum.GetValues(type).GetValue(0) as Enum;
                    }
                }
            }
            return type != null ? Enum.GetValues(type).GetValue(0) as Enum : null;
        }

        public static bool ToBoolean(object value)
        {
            if (value is bool retValue)
            {
                return retValue;
            }
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static int ToInt(object value)
        {
            if (value is int retValue)
            {
                return retValue;
            }
            try
            {
                return Convert.ToInt32(value);
            }
            catch (InvalidCastException)
            {
                return 0;
            }
            catch (FormatException)
            {
                return 0;
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }

        public static float ToFloat(object value)
        {
            if (value is float retValue)
            {
                return retValue;
            }

            try
            {
                if (value != null)
                {
                    return Convert.ToSingle(value.ToString().Replace(',', '.'), CultureInfo.InvariantCulture);
                }
            }
            catch (InvalidCastException)
            {
                return 0;
            }
            catch (FormatException)
            {
                return 0;
            }
            catch (ArgumentException)
            {
                return 0;
            }

            return 0;
        }

        public static bool TryParse(string value, out float result)
        {
            return float.TryParse(value?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static double ToDouble(object value)
        {
            if (value is double retValue)
            {
                return retValue;
            }
            try
            {
                return Convert.ToDouble(value);
            }
            catch (InvalidCastException)
            {
                return 0;
            }
            catch (FormatException)
            {
                return 0;
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }

        public static Vector2 ToVector2(object value)
        {
            if (value is Vector2)
            {
                return (Vector2)value;
            }
            if (value is Vector3)
            {
                return (Vector3)value;
            }
            if (value is Vector4)
            {
                return (Vector4)value;
            }
            if (value != null)
            {
                var valueString = value.ToString().TrimStart('(').TrimEnd(')').Replace(',', '.');
                var splits = valueString.Split(' ');
                if (splits.Length >= 2)
                {
                    try
                    {
                        return new Vector2(float.Parse(splits[0].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                           float.Parse(splits[1].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture));
                    }
                    catch (ArgumentException)
                    {
                        return Vector2.zero;
                    }
                }
            }
            return Vector2.zero;
        }

        public static Vector3 ToVector3(object value)
        {
            if (value is Vector2)
            {
                return (Vector2)value;
            }
            if (value is Vector3)
            {
                return (Vector3)value;
            }
            if (value is Vector4)
            {
                return (Vector4)value;
            }
            if (value != null)
            {
                var valueString = value.ToString().TrimStart('(').TrimEnd(')').Replace(',', '.');
                var splits = valueString.Split(' ');
                if (splits.Length >= 3)
                {
                    try
                    {
                        var returnV = new Vector3(float.Parse(splits[0].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                                  float.Parse(splits[1].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                                  float.Parse(splits[2].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture));
                        if (float.IsNaN(returnV.x)) { returnV.x = 0; }
                        if (float.IsNaN(returnV.y)) { returnV.y = 0; }
                        if (float.IsNaN(returnV.z)) { returnV.z = 0; }
                        return returnV;
                    }
                    catch (ArgumentException)
                    {
                        return Vector3.zero;
                    }
                }
            }
            return Vector3.zero;
        }

        public static Vector4 ToVector4(object value)
        {
            if (value is Vector2)
            {
                return (Vector2)value;
            }
            if (value is Vector3)
            {
                return (Vector3)value;
            }
            if (value is Vector4)
            {
                return (Vector4)value;
            }
            if (value != null)
            {
                var valueString = value.ToString().TrimStart('(').TrimEnd(')').Replace(',', '.');
                var splits = valueString.Split(' ');
                if (splits.Length >= 4)
                {
                    try
                    {
                        var returnV = new Vector4(float.Parse(splits[0].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                                  float.Parse(splits[1].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                                  float.Parse(splits[2].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                                  float.Parse(splits[3].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture));
                        if (float.IsNaN(returnV.x)) { returnV.x = 0; }
                        if (float.IsNaN(returnV.y)) { returnV.y = 0; }
                        if (float.IsNaN(returnV.z)) { returnV.z = 0; }
                        if (float.IsNaN(returnV.w)) { returnV.w = 0; }
                        return returnV;
                    }
                    catch (ArgumentException)
                    {
                        return Vector4.zero;
                    }
                }
            }
            return Vector4.zero;
        }

        public static Color ToColor(object value)
        {
            if (value is Color)
            {
                return (Color)value;
            }
            if (value != null)
            {
                var valueString = value.ToString().TrimStart('R', 'G', 'B', 'A', '(').TrimEnd(')').Replace(',', '.');
                var splits = valueString.Split(' ');
                if (splits.Length >= 4)
                {
                    try
                    {
                        return new Color(float.Parse(splits[0].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                         float.Parse(splits[1].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                         float.Parse(splits[2].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture),
                                         float.Parse(splits[3].TrimEnd('.'), NumberStyles.Float, CultureInfo.InvariantCulture));
                    }
                    catch (ArgumentException)
                    {
                        return Color.black;
                    }
                }
            }
            return Color.black;
        }

        public static UnityEngine.Object ToUnityObject(object value)
        {
            if (value is UnityEngine.Object)
            {
                return (UnityEngine.Object)value;
            }
            if (value is string)
            {
                var splits = ((string)value).Split('|');
                if (splits.Length < 3) { return null; }
                if (splits[0] == "[C]")
                {
                    // Component 
                    return ObjectRetriever.GetComponent(splits[1], splits[2], Type.GetType(splits[3]));
                }
                else if (splits[0] == "[G]")
                {
                    // GameObject
                    return ObjectRetriever.GetGameObject(splits[1], splits[2]);
                }
                else
                {
                    // Try as asset
                    var asset = ObjectRetriever.GetAsset(splits[1], splits[2]);
                    if (asset == null && Application.isEditor)
                    {
                        asset = AssetDatabase.LoadAssetAtPathByType(splits[1], Type.GetType(splits[2]));
                    }
                    return asset;
                }
            }
            return null;
        }

        public static bool IsUnityObjectDescriptionString(string s)
        {
            return (s.StartsWith("[C]") || s.StartsWith("[G]") || s.StartsWith("[O]")) && s.Split('|').Length > 2;
        }

        public static object TryConvertToUnityObject(string value)
        {
            if (!(value.StartsWith("[C]") || value.StartsWith("[G]") || value.StartsWith("[O]")))
            {
                return value;
            }
            var splits = value.Split('|');
            if (splits.Length < 3) { return value; }
            if (splits[0] == "[C]")
            {
                // Component 
                return ObjectRetriever.GetComponent(splits[1], splits[2], Type.GetType(splits[3]));
            }
            else if (splits[0] == "[G]")
            {
                // GameObject
                return ObjectRetriever.GetGameObject(splits[1], splits[2]);
            }
            else
            {
                // Try as asset
                var asset = ObjectRetriever.GetAsset(splits[1], splits[2]);

                if (asset == null && Application.isEditor)
                {
                    asset = AssetDatabase.LoadAssetAtPathByType(splits[1], Type.GetType(splits[2]));
                }
                return asset;
            }
        }

        public static string FromUnityObject(object value)
        {
            if (value is UnityEngine.Object)
            {
                if (value is Component)
                {
                    var gameObject = ((Component)value).gameObject;
                    var uniqueId = gameObject.GetComponent<UniqueID>();
                    if (uniqueId == null)
                    {
                        uniqueId = gameObject.AddComponent<UniqueID>();
                    }
                    return $"[C]|{uniqueId.ID}|{SceneTools.GetGameObjectPath(gameObject)}|{value.GetType().AssemblyQualifiedName}";
                }
                else if (value is GameObject)
                {
                    var gameObject = (GameObject)value;
                    var uniqueId = gameObject.GetComponent<UniqueID>();
                    if (uniqueId == null)
                    {
                        uniqueId = gameObject.AddComponent<UniqueID>();
                    }
                    return $"[G]|{uniqueId.ID}|{SceneTools.GetGameObjectPath(gameObject)}";
                }
                else if(Application.isEditor)
                {
                    return $"[O]|{AssetDatabase.GetAssetPath((UnityEngine.Object)value)}|{value.GetType().AssemblyQualifiedName}";
                }
            }
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }
    }
}