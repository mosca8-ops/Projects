using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR
{

    public static class UnitySerializationExtensions
    {
        public static void TrySetValue(this SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    if(value is bool)
                    {
                        prop.boolValue = (bool)value;
                    }
                    break;
                case SerializedPropertyType.Float:
                    if (value is float)
                    {
                        prop.floatValue = (float)value;
                    }
                    break;
                case SerializedPropertyType.Integer:
                    if (value is int)
                    {
                        prop.intValue = (int)value;
                    }
                    break;
                case SerializedPropertyType.String:
                    if (value is string str)
                    {
                        prop.stringValue = str;
                    }
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (value is UnityEngine.Object obj)
                    {
                        prop.objectReferenceValue = obj;
                    }
                    break;
                case SerializedPropertyType.Color:
                    if (value is Color color)
                    {
                        prop.colorValue = color;
                    }
                    break;
                case SerializedPropertyType.Vector3:
                    if (value is Vector3 vec3)
                    {
                        prop.vector3Value = vec3;
                    }
                    break;
                case SerializedPropertyType.Quaternion:
                    if (value is Quaternion)
                    {
                        prop.quaternionValue = (Quaternion)value;
                    }
                    break;
                case SerializedPropertyType.Vector2:
                    if (value is Vector2)
                    {
                        prop.vector2Value = (Vector2)value;
                    }
                    break;
                case SerializedPropertyType.Enum:
                    if (value is Enum)
                    {
                        prop.enumValueIndex = (int)value;
                    }
                    break;
                case SerializedPropertyType.Bounds:
                    if (value is Bounds)
                    {
                        prop.boundsValue = (Bounds)value;
                    }
                    break;
                case SerializedPropertyType.Rect:
                    if (value is Rect)
                    {
                        prop.rectValue = (Rect)value;
                    }
                    break;
                case SerializedPropertyType.AnimationCurve:
                    if (value is AnimationCurve curve)
                    {
                        prop.animationCurveValue = curve;
                    }
                    break;
                case SerializedPropertyType.ArraySize:
                    if (value is int)
                    {
                        prop.arraySize = (int)value;
                    }
                    break;
                case SerializedPropertyType.BoundsInt:
                    if (value is BoundsInt)
                    {
                        prop.boundsIntValue = (BoundsInt)value;
                    }
                    break;
                case SerializedPropertyType.Character:
                    if (value is int)
                    {
                        prop.intValue = (int)value;
                    }
                    break;
                case SerializedPropertyType.ExposedReference:
                    throw new NotImplementedException();

                case SerializedPropertyType.FixedBufferSize:
                    throw new NotImplementedException();

                case SerializedPropertyType.Generic:
                    prop.GetValueSetter()?.Invoke(prop.serializedObject.targetObject, value);
                    break;
                    //break;
                case SerializedPropertyType.Gradient:
                    throw new NotImplementedException();
                    //if (value is Gradient)
                    //{
                    //    prop.gra = ((Gradient)value).;
                    //}
                    //break;
                case SerializedPropertyType.LayerMask:
                    if (value is int)
                    {
                        prop.intValue = (int)value;
                    }
                    break;
                case SerializedPropertyType.RectInt:
                    if (value is RectInt)
                    {
                        prop.rectIntValue = (RectInt)value;
                    }
                    break;
                case SerializedPropertyType.Vector2Int:
                    if (value is Vector2Int)
                    {
                        prop.vector2IntValue = (Vector2Int)value;
                    }
                    break;
                case SerializedPropertyType.Vector3Int:
                    if (value is Vector3Int)
                    {
                        prop.vector3IntValue = (Vector3Int)value;
                    }
                    break;
                case SerializedPropertyType.Vector4:
                    if (value is Vector4)
                    {
                        prop.vector4Value = (Vector4)value;
                    }
                    break;
            }
        }


        private static Dictionary<string, Func<object, object>> s_cachedGetters = new Dictionary<string, Func<object, object>>();
        private static object GetGenericValue(SerializedProperty prop)
        {
            string key = prop.serializedObject.targetObject.GetType().Name + prop.propertyPath;
            if(!s_cachedGetters.TryGetValue(key, out Func<object, object> getter))
            {
                getter = prop.GetValueGetter();
                s_cachedGetters[key] = getter;
            }
            return getter?.Invoke(prop.serializedObject.targetObject);
        }

        public static object TryGetValue(this SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue;
                case SerializedPropertyType.Color:
                    return prop.colorValue;
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value;
                case SerializedPropertyType.Quaternion:
                    return prop.quaternionValue;
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value;
                case SerializedPropertyType.Enum:
                    return prop.enumValueIndex;
                case SerializedPropertyType.AnimationCurve:
                    return prop.animationCurveValue;
                case SerializedPropertyType.ArraySize:
                    return prop.arraySize;
                case SerializedPropertyType.Bounds:
                    return prop.boundsValue;
                case SerializedPropertyType.Rect:
                    return prop.rectValue;
                case SerializedPropertyType.BoundsInt:
                    return prop.boundsIntValue;
                case SerializedPropertyType.Character:
                    return (char)prop.intValue;
                case SerializedPropertyType.ExposedReference:
                    return prop.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize:
                    return prop.fixedBufferSize;
                case SerializedPropertyType.Generic:
                    return GetGenericValue(prop);
                case SerializedPropertyType.Gradient:
                    return prop.colorValue;
                case SerializedPropertyType.LayerMask:
                    return prop.intValue;
                case SerializedPropertyType.RectInt:
                    return prop.rectIntValue;
                case SerializedPropertyType.Vector2Int:
                    return prop.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return prop.vector3IntValue;
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value;
            }
            return null;
        }

        public static object TryGetNonGenericValue(this SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue;
                case SerializedPropertyType.Color:
                    return prop.colorValue;
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value;
                case SerializedPropertyType.Quaternion:
                    return prop.quaternionValue;
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value;
                case SerializedPropertyType.Enum:
                    return prop.enumValueIndex;
                case SerializedPropertyType.AnimationCurve:
                    return prop.animationCurveValue;
                case SerializedPropertyType.ArraySize:
                    return prop.arraySize;
                case SerializedPropertyType.Bounds:
                    return prop.boundsValue;
                case SerializedPropertyType.Rect:
                    return prop.rectValue;
                case SerializedPropertyType.BoundsInt:
                    return prop.boundsIntValue;
                case SerializedPropertyType.Character:
                    return (char)prop.intValue;
                case SerializedPropertyType.ExposedReference:
                    return prop.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize:
                    return prop.fixedBufferSize;
                case SerializedPropertyType.Generic:
                    return null;
                case SerializedPropertyType.Gradient:
                    return prop.colorValue;
                case SerializedPropertyType.LayerMask:
                    return prop.intValue;
                case SerializedPropertyType.RectInt:
                    return prop.rectIntValue;
                case SerializedPropertyType.Vector2Int:
                    return prop.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return prop.vector3IntValue;
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value;
            }
            return null;
        }

        public static void TryCopyValueFrom(this SerializedProperty propA, SerializedProperty propB)
        {
            switch (propA.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    propA.boolValue = propB.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    propA.floatValue = propB.floatValue;
                    break;
                case SerializedPropertyType.Integer:
                    propA.intValue = propB.intValue;
                    break;
                case SerializedPropertyType.String:
                    propA.stringValue = propB.stringValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    propA.objectReferenceValue = propB.objectReferenceValue;
                    break;
                case SerializedPropertyType.Color:
                    propA.colorValue = propB.colorValue;
                    break;
                case SerializedPropertyType.Vector3:
                    propA.vector3Value = propB.vector3Value;
                    break;
                case SerializedPropertyType.Quaternion:
                    propA.quaternionValue = propB.quaternionValue;
                    break;
                case SerializedPropertyType.Vector2:
                    propA.vector2Value = propB.vector2Value;
                    break;
                case SerializedPropertyType.Enum:
                    propA.enumValueIndex = propB.enumValueIndex;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    propA.animationCurveValue = propB.animationCurveValue;
                    break;
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.FixedBufferSize:
                    try
                    {
                        propA.arraySize = propB.arraySize;
                    }
                    catch
                    {
                        // Skip this copy
                    }
                    break;
                case SerializedPropertyType.Bounds:
                    propA.boundsValue = propB.boundsValue;
                    break;
                case SerializedPropertyType.Rect:
                    propA.rectValue = propB.rectValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    propA.boundsIntValue = propB.boundsIntValue;
                    break;
                case SerializedPropertyType.Character:
                    propA.intValue = propB.intValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    propA.exposedReferenceValue = propB.exposedReferenceValue;
                    break;
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Generic:
                    try
                    {
                        CopyGenericValue(propA.Copy(), propB.Copy());
                    }
                    catch
                    {
                        // Just skip
                    }
                    break;
                case SerializedPropertyType.LayerMask:
                    propA.intValue = propB.intValue;
                    break;
                case SerializedPropertyType.RectInt:
                    propA.rectIntValue = propB.rectIntValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    propA.vector2IntValue = propB.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    propA.vector3IntValue = propB.vector3IntValue;
                    break;
                case SerializedPropertyType.Vector4:
                    propA.vector4Value = propB.vector4Value;
                    break;
            }
        }

        private static void CopyGenericValue(SerializedProperty propA, SerializedProperty propB)
        {
            while(propA.Next(propA.propertyType == SerializedPropertyType.Generic) 
                && propB.Next(propB.propertyType == SerializedPropertyType.Generic)
                && propA.type == propB.type)
            {
                switch (propA.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        propA.boolValue = propB.boolValue;
                        break;
                    case SerializedPropertyType.Float:
                        propA.floatValue = propB.floatValue;
                        break;
                    case SerializedPropertyType.Integer:
                        propA.intValue = propB.intValue;
                        break;
                    case SerializedPropertyType.String:
                        propA.stringValue = propB.stringValue;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        propA.objectReferenceValue = propB.objectReferenceValue;
                        break;
                    case SerializedPropertyType.Color:
                        propA.colorValue = propB.colorValue;
                        break;
                    case SerializedPropertyType.Vector3:
                        propA.vector3Value = propB.vector3Value;
                        break;
                    case SerializedPropertyType.Quaternion:
                        propA.quaternionValue = propB.quaternionValue;
                        break;
                    case SerializedPropertyType.Vector2:
                        propA.vector2Value = propB.vector2Value;
                        break;
                    case SerializedPropertyType.Enum:
                        propA.enumValueIndex = propB.enumValueIndex;
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        propA.animationCurveValue = propB.animationCurveValue;
                        break;
                    case SerializedPropertyType.ArraySize:
                        propA.arraySize = propB.arraySize;
                        break;
                    case SerializedPropertyType.Bounds:
                        propA.boundsValue = propB.boundsValue;
                        break;
                    case SerializedPropertyType.Rect:
                        propA.rectValue = propB.rectValue;
                        break;
                    case SerializedPropertyType.BoundsInt:
                        propA.boundsIntValue = propB.boundsIntValue;
                        break;
                    case SerializedPropertyType.Character:
                        propA.intValue = propB.intValue;
                        break;
                    case SerializedPropertyType.ExposedReference:
                        propA.exposedReferenceValue = propB.exposedReferenceValue;
                        break;
                    case SerializedPropertyType.LayerMask:
                        propA.intValue = propB.intValue;
                        break;
                    case SerializedPropertyType.RectInt:
                        propA.rectIntValue = propB.rectIntValue;
                        break;
                    case SerializedPropertyType.Vector2Int:
                        propA.vector2IntValue = propB.vector2IntValue;
                        break;
                    case SerializedPropertyType.Vector3Int:
                        propA.vector3IntValue = propB.vector3IntValue;
                        break;
                    case SerializedPropertyType.Vector4:
                        propA.vector4Value = propB.vector4Value;
                        break;
                }
            }
        }
    }
}
