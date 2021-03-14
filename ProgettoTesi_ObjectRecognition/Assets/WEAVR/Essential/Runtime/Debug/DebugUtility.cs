using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using TXT.WEAVR.Utility;
using UnityEngine.Events;

namespace TXT.WEAVR.Debugging
{

    public static class DebugUtility
    {
        private static readonly object[] s_zeroParams = new object[0];
        
        public static string NicifyName(string name)
        {
            if (name.StartsWith("m_"))
            {
                name = name.Substring(2);
            }
            else if (name.StartsWith("_"))
            {
                name = name.TrimStart('_');
            }
            return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
        }
        
        public static string SplitCamelCase(string inputCamelCaseString)
        {
            string sTemp = Regex.Replace(inputCamelCaseString, "([A-Z][a-z])", " $1", RegexOptions.Compiled).Trim();
            return Regex.Replace(sTemp, "([A-Z][A-Z])", " $1", RegexOptions.Compiled).Trim();
        }
        
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return ((FieldInfo)memberInfo).FieldType;
            }
            if (memberInfo is PropertyInfo)
            {
                return ((PropertyInfo)memberInfo).PropertyType;
            }
            if (memberInfo is MethodInfo)
            {
                return ((MethodInfo)memberInfo).ReturnType;
            }
            if (memberInfo is EventInfo)
            {
                return ((EventInfo)memberInfo).EventHandlerType;
            }
            return null;
        }
        
        public static bool IsPublic(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return ((FieldInfo)memberInfo).IsPublic;
            }
            if (memberInfo is PropertyInfo)
            {
                return ((PropertyInfo)memberInfo).GetMethod != null && ((PropertyInfo)memberInfo).GetMethod.IsPublic;
            }
            if (memberInfo is MethodInfo)
            {
                return ((MethodInfo)memberInfo).IsPublic;
            }
            if (memberInfo is EventInfo)
            {
                return true;
            }
            return false;
        }
        
        public static bool IsEvent(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return IsUnityEvent(((FieldInfo)memberInfo).FieldType);
            }
            else if (memberInfo is PropertyInfo)
            {
                return IsUnityEvent(((PropertyInfo)memberInfo).PropertyType);
            }
            return memberInfo is EventInfo;
        }
        
        public static bool IsUnityEvent(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return IsUnityEvent(((FieldInfo)memberInfo).FieldType);
            }
            else if (memberInfo is PropertyInfo)
            {
                return IsUnityEvent(((PropertyInfo)memberInfo).PropertyType);
            }
            return false;
        }

        public static Func<object> CreateReturnMethod(object instance, MethodInfo methodInfo)
        {
            Type returnType = methodInfo.ReturnType;
            if (!returnType.IsValueType)
            {
                //return (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), instance, methodInfo);
                return (Func<object>)methodInfo.CreateDelegate(typeof(Func<object>), instance);
            }
            else if (returnType == typeof(int))
            {
                return CreateLambdaWrapper<int>(instance, methodInfo);
            }
            else if (returnType == typeof(float))
            {
                return CreateLambdaWrapper<float>(instance, methodInfo);
            }
            else if (returnType == typeof(bool))
            {
                return CreateLambdaWrapper<bool>(instance, methodInfo);
            }
            else if (returnType == typeof(double))
            {
                return CreateLambdaWrapper<double>(instance, methodInfo);
            }
            else if (returnType == typeof(char))
            {
                return CreateLambdaWrapper<char>(instance, methodInfo);
            }
            else if (returnType == typeof(byte))
            {
                return CreateLambdaWrapper<byte>(instance, methodInfo);
            }
            else if (returnType == typeof(Vector2))
            {
                return CreateLambdaWrapper<Vector2>(instance, methodInfo);
            }
            else if (returnType == typeof(Vector2Int))
            {
                return CreateLambdaWrapper<Vector2Int>(instance, methodInfo);
            }
            else if (returnType == typeof(Vector3))
            {
                return CreateLambdaWrapper<Vector3>(instance, methodInfo);
            }
            else if (returnType == typeof(Vector3Int))
            {
                return CreateLambdaWrapper<Vector3Int>(instance, methodInfo);
            }
            else if (returnType == typeof(Vector4))
            {
                return CreateLambdaWrapper<Vector4>(instance, methodInfo);
            }
            else if (returnType == typeof(Quaternion))
            {
                return CreateLambdaWrapper<Quaternion>(instance, methodInfo);
            }
            else if (returnType == typeof(Rect))
            {
                return CreateLambdaWrapper<Rect>(instance, methodInfo);
            }
            else if (returnType == typeof(RectInt))
            {
                return CreateLambdaWrapper<RectInt>(instance, methodInfo);
            }
            else if (returnType == typeof(Bounds))
            {
                return CreateLambdaWrapper<Bounds>(instance, methodInfo);
            }
            else if (returnType == typeof(BoundsInt))
            {
                return CreateLambdaWrapper<BoundsInt>(instance, methodInfo);
            }
            else if (returnType == typeof(BoundingSphere))
            {
                return CreateLambdaWrapper<BoundingSphere>(instance, methodInfo);
            }
            else if (returnType == typeof(Color))
            {
                return CreateLambdaWrapper<Color>(instance, methodInfo);
            }
            else if (returnType == typeof(Color32))
            {
                return CreateLambdaWrapper<Color32>(instance, methodInfo);
            }
            else if (returnType == typeof(Ray))
            {
                return CreateLambdaWrapper<Ray>(instance, methodInfo);
            }
            else if (returnType == typeof(Ray2D))
            {
                return CreateLambdaWrapper<Ray2D>(instance, methodInfo);
            }
            else if (returnType == typeof(RaycastHit))
            {
                return CreateLambdaWrapper<RaycastHit>(instance, methodInfo);
            }
            else if (returnType == typeof(RaycastHit2D))
            {
                return CreateLambdaWrapper<RaycastHit2D>(instance, methodInfo);
            }
            else if (returnType == typeof(LayerMask))
            {
                return CreateLambdaWrapper<LayerMask>(instance, methodInfo);
            }
            return () => methodInfo.Invoke(instance, s_zeroParams);
        }

        private static Func<object> CreateLambdaWrapper<T>(object instance, MethodInfo methodInfo)
        {
            //Func<T> functor = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), instance, methodInfo);
            Func<T> functor = (Func<T>)methodInfo.CreateDelegate(typeof(Func<T>), instance);
            return () => functor();
        }

        public static bool IsUnityEvent(this Type memberType)
        {
            return memberType == typeof(UnityEvent) || memberType.IsSubclassOf(typeof(UnityEventBase));
        }
    }
}
