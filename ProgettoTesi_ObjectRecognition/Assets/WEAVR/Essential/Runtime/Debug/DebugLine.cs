using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Debugging
{

    [AddComponentMenu("WEAVR/Debug/Debug Line")]
    public class DebugLine : MonoBehaviour
    {
        public enum LineType
        {
            Field,
            Property,
            Event,
            UnityEvent,
            Method
        }

        [Draggable]
        public Text label;
        [Draggable]
        public Text value;
        [Space]
        public bool useNiceNames = true;
        public Color changeColor = Color.green;
        public Color nullColor = Color.red;

        [SerializeField]
        [HideInInspector]
        private string m_memberName;
        [SerializeField]
        [HideInInspector]
        private LineType m_lineType;

        [SerializeField]
        [HideInInspector]
        private string m_memberTypeString;

        [SerializeField]
        [HideInInspector]
        private string m_memberInfoTypename;

        private Func<object, object> m_getter;

        private Color m_startColor;

        private object m_lastValue;
        private int m_eventCalls;

        public LineType Type => m_lineType;
        public string MemberTypename => m_memberTypeString;

        private void Reset()
        {
            SetupComponents();
        }

        private void OnValidate()
        {
            SetupComponents();
        }

        private void Start()
        {
            if (value != null)
            {
                m_startColor = value.color;
            }
        }

        public bool Save(MemberInfo memberInfo)
        {
            string displayName = useNiceNames ? DebugUtility.NicifyName(memberInfo.Name) : memberInfo.Name;
            m_memberName = memberInfo.Name;
            gameObject.name = $"Line_{displayName}";
            label.text = displayName + ":";

            m_memberInfoTypename = memberInfo.GetMemberType().FullName;

            if (memberInfo.IsUnityEvent())
            {
                m_lineType = LineType.UnityEvent;
                m_memberTypeString = memberInfo is EventInfo ? ((EventInfo)memberInfo).EventHandlerType.Name
                                                             : memberInfo is FieldInfo ? ((FieldInfo)memberInfo).FieldType.Name
                                                             : memberInfo is PropertyInfo ? ((PropertyInfo)memberInfo).PropertyType.Name
                                                             : memberInfo is MethodInfo ? ((MethodInfo)memberInfo).ReturnType.Name
                                                             : "Undefined";
                return true;
            }
            else if (memberInfo is FieldInfo)
            {
                m_lineType = LineType.Field;
                m_memberTypeString = ((FieldInfo)memberInfo).FieldType.Name;
                return true;
            }
            else if (memberInfo is PropertyInfo)
            {
                m_lineType = LineType.Property;
                m_memberTypeString = ((PropertyInfo)memberInfo).PropertyType.Name;
                return true;
            }
            else if (memberInfo is MethodInfo && ((MethodInfo)memberInfo).ReturnType != typeof(void) && ((MethodInfo)memberInfo).GetParameters().Length == 0)
            {
                m_lineType = LineType.Method;
                m_memberTypeString = ((MethodInfo)memberInfo).ReturnType.Name;
                return true;
            }
            else if (memberInfo is EventInfo)
            {
                m_lineType = LineType.Event;
                m_memberTypeString = ((EventInfo)memberInfo).EventHandlerType.Name;
                return true;
            }
            return false;
        }

        public void SetLabelName(string labelName)
        {
            if (label != null)
            {
                label.text = labelName;
            }
        }

        public void PrepareData(MonoBehaviour owner)
        {
            if (string.IsNullOrEmpty(m_memberName))
            {
                m_getter = o => "Member not set";
                return;
            }
            switch (m_lineType)
            {
                case LineType.Field:
                    m_getter = owner.GetType().GetField(m_memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FastGetter();
                    break;
                case LineType.Property:
                    m_getter = owner.GetType().GetProperty(m_memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FastGetter();
                    break;
                case LineType.Event:
                    var eventInfo = owner.GetType().GetEvent(m_memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (eventInfo.GetRaiseMethod().ReturnType == typeof(void))
                    {
                        //eventInfo.AddEventHandler(owner, CaptureEventCallWithParameters);
                        //m_getter = o => $"Raised {m_eventCalls} times";
                    }
                    else
                    {
                        m_getter = o => "Failed to parse event";
                    }
                    break;
                case LineType.UnityEvent:
                    var memberInfo = owner.GetType().GetMember(m_memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];
                    UnityEventBase unityEvent = null;
                    if (memberInfo is FieldInfo)
                    {
                        unityEvent = ((FieldInfo)memberInfo).GetValue(owner) as UnityEventBase;
                    }
                    else if (memberInfo is PropertyInfo)
                    {
                        unityEvent = ((PropertyInfo)memberInfo).GetValue(owner) as UnityEventBase;
                    }
                    if (unityEvent != null && DispatchUnityEvent(unityEvent))
                    {
                        m_getter = o => $"Raised {m_eventCalls} times";
                    }
                    break;
                case LineType.Method:
                    var methodInfo = owner.GetType().GetMethod(m_memberName, new Type[0]);
                    if (methodInfo != null)
                    {
                        var method = DebugUtility.CreateReturnMethod(owner, methodInfo);
                        m_getter = o => method();
                    }
                    else
                    {
                        m_getter = o => null;
                    }
                    break;
            }
        }
        
        
        public bool HasMember(MemberInfo info) {
            return info.Name == m_memberName && info.GetMemberType().FullName == m_memberInfoTypename;
        }

        private bool DispatchUnityEvent(UnityEventBase unityEvent)
        {
            if (unityEvent is UnityEvent)
            {
                ((UnityEvent)unityEvent).AddListener(CaptureEventCall);
            }
            else
            {
                var addListenerMethod = unityEvent.GetType().GetMethod(nameof(UnityEvent.AddListener), BindingFlags.Public | BindingFlags.Instance);
                if (addListenerMethod == null)
                {
                    Debug.Log("Add listener not found");
                    return false;
                }
                Delegate[] parameters = new Delegate[1];
                BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                MethodInfo captureEventCall = null;
                Type unityActionType = null;
                var genericArgs = GetGenericArguments(unityEvent);
                switch (genericArgs.Length)
                {
                    case 1:
                        captureEventCall = GetType().GetMethod(nameof(CaptureEventCall_1), bindingFlags)
                                                        .MakeGenericMethod(genericArgs[0]);
                        unityActionType = typeof(UnityAction<>).MakeGenericType(genericArgs[0]);
                        //parameters[0] = Delegate.CreateDelegate(unityActionType, this, captureEventCall);
                        parameters[0] = captureEventCall.CreateDelegate(unityActionType, this);
                        break;
                    case 2:
                        captureEventCall = GetType().GetMethod(nameof(CaptureEventCall_2), bindingFlags)
                                                        .MakeGenericMethod(genericArgs[0], genericArgs[1]);
                        unityActionType = typeof(UnityAction<,>).MakeGenericType(genericArgs[0], genericArgs[1]);
                        //parameters[0] = Delegate.CreateDelegate(unityActionType, this, captureEventCall);
                        parameters[0] = captureEventCall.CreateDelegate(unityActionType, this);
                        break;
                    case 3:
                        captureEventCall = GetType().GetMethod(nameof(CaptureEventCall_3), bindingFlags)
                                                        .MakeGenericMethod(genericArgs[0], genericArgs[1], genericArgs[2]);
                        unityActionType = typeof(UnityAction<,,>).MakeGenericType(genericArgs[0], genericArgs[1], genericArgs[2]);
                        //parameters[0] = Delegate.CreateDelegate(unityActionType, this, captureEventCall);
                        parameters[0] = captureEventCall.CreateDelegate(unityActionType, this);
                        break;
                    case 4:
                        captureEventCall = GetType().GetMethod(nameof(CaptureEventCall_1), bindingFlags)
                                                        .MakeGenericMethod(genericArgs[0], genericArgs[1], genericArgs[2], genericArgs[3]);
                        unityActionType = typeof(UnityAction<,,,>).MakeGenericType(genericArgs[0], genericArgs[1], genericArgs[2], genericArgs[3]);
                        //parameters[0] = Delegate.CreateDelegate(unityActionType, this, captureEventCall);
                        parameters[0] = captureEventCall.CreateDelegate(unityActionType, this);
                        break;
                }
                if (captureEventCall == null)
                {
                    Debug.LogError("Not a valid unity event");
                    return false;
                }
                addListenerMethod.Invoke(unityEvent, parameters);
            }
            return true;
        }

        private Type[] GetGenericArguments(UnityEventBase unityEvent)
        {
            List<Type> args = new List<Type>();
            var type = unityEvent.GetType();
            while (type.BaseType != typeof(UnityEventBase))
            {
                type = type.BaseType;
            }
            if (type.IsGenericType)
            {
                args.AddRange(type.GetGenericArguments());
            }
            return args.ToArray();
        }

        #region [  EVENT TEMPLATES  ]

        private void CaptureEventCall_1<T0>(T0 par1)
        {
            CaptureEventCall();
        }

        private void CaptureEventCall_2<T0, T1>(T0 par1, T1 par2)
        {
            CaptureEventCall();
        }

        private void CaptureEventCall_3<T0, T1, T2>(T0 par1, T1 par2, T2 par3)
        {
            CaptureEventCall();
        }

        private void CaptureEventCall_4<T0, T1, T2, T3>(T0 par1, T1 par2, T2 par3, T3 par4)
        {
            CaptureEventCall();
        }

        private Tout CaptureEventCallWithReturn_1<T0, Tout>(T0 par1)
        {
            return CaptureEventCallWithReturn<Tout>();
        }

        private Tout CaptureEventCallWithReturn_2<T0, T1, Tout>(T0 par1, T1 par2)
        {
            return CaptureEventCallWithReturn<Tout>();
        }

        private Tout CaptureEventCallWithReturn_3<T0, T1, T2, Tout>(T0 par1, T1 par2, T2 par3)
        {
            return CaptureEventCallWithReturn<Tout>();
        }

        private Tout CaptureEventCallWithReturn_4<T0, T1, T2, T3, Tout>(T0 par1, T1 par2, T2 par3, T3 par4)
        {
            return CaptureEventCallWithReturn<Tout>();
        }

        private Tout CaptureEventCallWithReturn<Tout>()
        {
            CaptureEventCall();
            return default(Tout);
        }

        #endregion

        private void CaptureEventCall()
        {
            m_eventCalls++;
        }

        private void CaptureEventCallWithParameters(params object[] parameters)
        {
            m_eventCalls++;
        }

        public void UpdateInfo(MonoBehaviour owner, int updateRate)
        {
            if (m_getter == null)
            {
                m_getter = o => "Getter not set";
            }
            var currentValue = m_getter(owner);
            if (currentValue == null)
            {
                if (m_lastValue != null || value.text != "null")
                {
                    m_lastValue = null;
                    value.color = nullColor;
                    value.text = "null";
                }
                else if (value.color != m_startColor)
                {
                    value.color = Color.Lerp(value.color, m_startColor, Time.deltaTime * updateRate);
                }
            }
            else if (!currentValue.Equals(m_lastValue))
            {
                m_lastValue = currentValue;
                value.color = changeColor;
                //value.text = currentValue.ToString();
                value.text = GetNiceString(currentValue);
            }
            else if (value.color != m_startColor)
            {
                value.color = Color.Lerp(value.color, m_startColor, Time.deltaTime * updateRate);
            }
        }

        private string GetNiceString(object obj)
        {
            if (obj.GetType().IsValueType)
            {
                return obj.ToString();
            }
            if (obj is string)
            {
                return (string)obj;
            }
            if (obj is UnityEngine.Object)
            {
                return string.IsNullOrEmpty(((UnityEngine.Object)obj).name) ? obj.GetType().Name : ((UnityEngine.Object)obj).name;
            }
            if (obj.GetType().IsArray)
            {
                return $"{obj.GetType().GetElementType().Name}[{((Array)obj).Length}]";
            }
            if (obj.GetType().IsGenericType)
            {
                if (obj is ICollection)
                {
                    return $"{obj.GetType().GenericTypeArguments[0].Name}[{((ICollection)obj).Count}]";
                }
                if (obj is IDictionary)
                {
                    return $"{{{obj.GetType().GenericTypeArguments[0].Name}, {obj.GetType().GenericTypeArguments[1].Name}}}[{((IDictionary)obj).Count}]";
                }
            }
            if (!IsToStringOverriden(obj))
            {
                return $"[{obj.GetType().Name}]";
            }
            return obj.ToString();
        }

        private bool IsToStringOverriden(object obj)
        {
            if (obj == null) { return false; }
            return obj.GetType().GetMethod(nameof(ToString), new Type[0]).DeclaringType != typeof(object);
        }

        private void SetupComponents()
        {
            if (label == null)
            {
                label = GetComponentInChildren<Text>();
            }
            if (value == null)
            {
                var texts = GetComponentsInChildren<Text>();
                if (texts.Length > 1)
                {
                    value = texts[1];
                }
            }
        }

        internal void UpdateColors(BehaviourDebug.DebugColors colors)
        {
            switch (m_lineType)
            {
                case LineType.Field:
                    label.color = colors.fieldColor;
                    break;
                case LineType.Property:
                    label.color = colors.propertyColor;
                    break;
                case LineType.UnityEvent:
                case LineType.Event:
                    label.color = colors.eventColor;
                    break;
                case LineType.Method:
                    label.color = colors.methodColor;
                    break;
            }
            if (colors.changeColor.a > 0)
            {
                changeColor = colors.changeColor;
            }
            if (colors.nullColor.a > 0)
            {
                nullColor = colors.nullColor;
            }
        }
    }
}
