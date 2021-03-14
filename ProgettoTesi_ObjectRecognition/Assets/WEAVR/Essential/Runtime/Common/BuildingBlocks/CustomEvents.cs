using System;
using System.Linq;
using System.Reflection;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    public interface IUnityEventCopy
    {
        void TryCopyFrom(UnityEventBase uevent);
    }

    [Serializable]
    public class WeavrUnityEvent<T> : UnityEvent<T>, IUnityEventCopy
    {
        public void TryCopyFrom(UnityEventBase uevent)
        {
            var registerMethod = typeof(UnityEventBase).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                       .FirstOrDefault(m => m.Name == "RegisterPersistentListener" 
                                                                         && m.GetParameters().Length == 3 
                                                                         && m.GetParameters()[0].ParameterType == typeof(int) 
                                                                         && m.GetParameters()[1].ParameterType == typeof(object)
                                                                         && m.GetParameters()[2].ParameterType == typeof(MethodInfo));
            
            if(registerMethod == null) { return; }

            for (int i = 0; i < uevent.GetPersistentEventCount(); i++)
            {
                var target = uevent.GetPersistentTarget(i);
                var methodName = uevent.GetPersistentMethodName(i);
                var method = GetMethod(methodName, target);
                if(method != null)
                {
                    try
                    {
                        registerMethod.Invoke(this, new object[] { GetPersistentEventCount(), target, method });
                    }
                    catch
                    {

                    }
                    //#if UNITY_EDITOR
                    //                    // This is used only for source coding
                    //RegisterPersistentListener(GetPersistentEventCount(), target, method);
                    //#endif
                }
            }
        }

        protected virtual MethodInfo GetMethod(string methodName, object target)
        {
            var method = FindMethod_Impl(methodName, target) ?? GetValidMethodInfo(target, methodName, new Type[0]) ?? GetValidMethodInfo(target, methodName, new Type[] { typeof(T) });
            if(method != null)
            {
                return method;
            }

            var type = target?.GetType();
            while(type != null && type != typeof(object))
            {
                method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == methodName);
                if(method != null)
                {
                    return method;
                }
                type = type.BaseType;
            }
            return null;
        }
    }

    [Serializable]
    public class UnityEventBoolean : WeavrUnityEvent<bool> { }

    [Serializable]
    public class UnityEventColor : WeavrUnityEvent<Color> { }

    [Serializable]
    public class UnityEventFloat : WeavrUnityEvent<float> { }

    [Serializable]
    public class UnityEventInt : WeavrUnityEvent<int> { }

    [Serializable]
    public class UnityEventChar : WeavrUnityEvent<char> { }

    [Serializable]
    public class UnityEventString : WeavrUnityEvent<string> { }

    [Serializable]
    public class UnityEventTexture : WeavrUnityEvent<Texture> { }
    [Serializable]
    public class UnityEventGameObject : WeavrUnityEvent<GameObject> { }
}
