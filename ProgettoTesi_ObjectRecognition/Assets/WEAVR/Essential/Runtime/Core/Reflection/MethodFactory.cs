using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Core
{
    // id = declaringTypeFull->methodName(param1,param2,param3)

    public static class MethodFactory
    {
        private static Dictionary<string, Method> s_cachedMethods = new Dictionary<string, Method>();
        private static Dictionary<Type, List<Method>> s_cachedTypesMethods = new Dictionary<Type, List<Method>>();
        private static HashSet<Type> s_allowedParameterTypes = new HashSet<Type>
        {
            //typeof(void),
            typeof(int),
            typeof(byte),
            typeof(short),
            typeof(long),
            typeof(bool),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(Object),
            typeof(Vector2),
            typeof(Vector2Int),
            typeof(Vector3),
            typeof(Vector3Int),
            typeof(Vector4),
            typeof(Bounds),
            typeof(Color),
            typeof(Ray),
            typeof(Quaternion),

        };

        private static List<MemberInfo> s_hiddenMemberInfos;
        private static List<MemberInfo> HiddenMemberInfos
        {
            get
            {
                if(s_hiddenMemberInfos == null)
                {
                    s_hiddenMemberInfos = new List<MemberInfo>();
                    s_hiddenMemberInfos.AddRange(typeof(object).GetMembers(BindingFlags.Instance | BindingFlags.Public));

                }
                return s_hiddenMemberInfos;
            }
        }

        private static string[] s_hiddenMethodNames = {
            nameof(object.Equals),
            nameof(object.GetHashCode),
            nameof(object.GetType),
            nameof(object.ReferenceEquals),
            nameof(object.ToString),
            nameof(Object.GetInstanceID),
            nameof(Component.GetComponent),
            nameof(Transform.GetEnumerator),
            //nameof(MonoBehaviour.IsInvoking),
            //nameof(MonoBehaviour.CancelInvoke),

            //nameof(MonoBehaviour.StartCoroutine),
            //nameof(MonoBehaviour.StopCoroutine),
            //nameof(MonoBehaviour.StopAllCoroutines),
            //nameof(MonoBehaviour.SendMessage),
            //nameof(MonoBehaviour.SendMessageUpwards),
            //nameof(MonoBehaviour.CancelInvoke),
            //nameof(MonoBehaviour.Invoke),
            //nameof(MonoBehaviour.InvokeRepeating),
        };

        public static IEnumerable<Method> GetAllCachedMethods() => s_cachedMethods.Values;

        public static void RegisterAllowedParameterType(Type type) => s_allowedParameterTypes.Add(type);

        public static IEnumerable<Method> GetValidMethods(Type owner)
        {
            if (!s_cachedTypesMethods.TryGetValue(owner, out List<Method> methods))
            {
                methods = new List<Method>();
                foreach (var m in owner.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (IsMethodInfoInvalid(m))
                    { continue; }

                    bool valid = true;
                    foreach (var p in m.GetParameters())
                    {
                        if (!IsParameterValid(p))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (!valid) { continue; }

                    var id = CreateId(m);
                    var method = GetMethod(id);
                    if (method != null)
                    {
                        methods.Add(method);
                    }
                }

                s_cachedTypesMethods[owner] = methods;
            }
            return methods;
        }

        private static bool IsMethodInfoInvalid(MethodInfo m)
        {
            return m.ContainsGenericParameters
                                    || m.IsSpecialName
                                    || m.GetCustomAttribute<ObsoleteAttribute>() != null
                                    || m.GetCustomAttribute<DoNotExposeAttribute>() != null
                                    || HiddenMemberInfos.Contains(m)
                                    || s_hiddenMethodNames.Contains(m.Name);
        }

        private static bool IsParameterValid(ParameterInfo p)
        {
            if (p.ParameterType.IsInterface 
                || p.ParameterType == typeof(object)
                || p.ParameterType == typeof(Type) 
                || p.ParameterType.IsArray)
            {
                return false;
            }
            if (p.ParameterType.IsEnum || p.ParameterType.GetCustomAttribute<SerializableAttribute>() != null) { return true; }

            bool hasValidType = false;
            foreach (var t in s_allowedParameterTypes)
            {
                if (t.IsAssignableFrom(p.ParameterType))
                {
                    hasValidType = true;
                }
            }

            return hasValidType;
        }

        public static Method GetMethod(string id)
        {
            if(!s_cachedMethods.TryGetValue(id, out Method method))
            {
                var info = FindMethod(id);
                if (info != null)
                {
                    method = new ReflectedMethod(info)
                    {
                        Id = id,
                        Name = info.Name,
                        parameterTypenames = info.GetParameters().Select(p => (p.Name, p.ParameterType)).ToArray(),
                    };
                    s_cachedMethods[id] = method;
                }
                else
                {
                    WeavrDebug.LogError(nameof(MethodFactory), $"Unable to retrieve method with id = {id}");
                }
            }

            return method;
        }

        private static string CreateId(MethodInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(info.DeclaringType.AssemblyQualifiedName).Append('-').Append('>').Append(info.Name).Append('(');
            foreach(var p in info.GetParameters())
            {
                sb.Append(p.ParameterType.Name).Append(',');
            }
            if(sb[sb.Length - 1] == ',')
            {
                sb.Length--;
            }
            sb.Append(')');
            return sb.ToString();
        }

        private static MethodInfo FindMethod(string id)
        {
            int callerSeparator = id.IndexOf("->");
            int methodSeparator = id.IndexOf('(');
            string typename = id.Substring(0, callerSeparator);
            string methodName = id.Substring(callerSeparator + 2, methodSeparator - callerSeparator - 2);
            string parameters = id.Substring(methodSeparator + 1, id.Length - methodSeparator - 2);
            if (string.IsNullOrEmpty(parameters))
            {
                return Type.GetType(typename)?.GetMethod(methodName, new Type[0]);
            }
            string[] parTypenames = parameters.Split(',');
            Type[] parTypes = new Type[parTypenames.Length];
            bool exhaustiveSearch = false;
            for (int i = 0; i < parTypes.Length; i++)
            {
                parTypes[i] = Type.GetType(parTypenames[i]);
                if(parTypes[i] == null)
                {
                    exhaustiveSearch = true;
                    break;
                }
            }
            Type owner = Type.GetType(typename);
            if (exhaustiveSearch)
            {
                var methods = owner.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name == methodName);
                foreach(var m in methods)
                {
                    if (m.ContainsGenericParameters) { continue; }
                    var pars = m.GetParameters();
                    if(pars.Length != parTypenames.Length) { continue; }
                    for (int i = 0; i < parTypenames.Length; i++)
                    {
                        if(pars[i].ParameterType.Name != parTypenames[i])
                        {
                            continue;
                        }
                    }
                    return m;
                }
                return null;
            }
            return owner.GetMethod(methodName, parTypes);
        }

        #region [  SPECIAL METHODS REGISTRATION  ]

        #region     [  ACTIONS  ]

        public static void RegisterFastMethod<CType>(string id, string methodName, Action<CType> action) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}()";
            }
            s_cachedMethods[id] = new VoidMethod<CType>(action)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[0],
            };
        }

        public static void RegisterFastMethod<CType, T1>(string id, string methodName, Action<CType, T1> action, string p1) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name})";
            }
            s_cachedMethods[id] = new VoidMethod<CType, T1>(action)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)) },
            };
        }

        public static void RegisterFastMethod<CType, T1, T2>(string id, string methodName, Action<CType, T1, T2> action, string p1, string p2) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name},{typeof(T2).Name})";
            }
            s_cachedMethods[id] = new VoidMethod<CType, T1, T2>(action)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)), (p2, typeof(T2)) },
            };
        }

        public static void RegisterFastMethod<CType, T1, T2, T3>(string id, string methodName, Action<CType, T1, T2, T3> action, string p1, string p2, string p3) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name},{typeof(T2).Name},{typeof(T3).Name})";
            }
            s_cachedMethods[id] = new VoidMethod<CType, T1, T2, T3>(action)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)), (p2, typeof(T2)), (p3, typeof(T3)) },
            };
        }

        public static void RegisterFastMethod<CType, T1, T2, T3, T4>(string id, string methodName, Action<CType, T1, T2, T3, T4> action, string p1, string p2, string p3, string p4) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name},{typeof(T2).Name},{typeof(T3).Name},{typeof(T4).Name})";
            }
            s_cachedMethods[id] = new VoidMethod<CType, T1, T2, T3, T4>(action)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)), (p2, typeof(T2)), (p3, typeof(T3)), (p4, typeof(T4)) },
            };
        }

        #endregion

        #region    [  FUNCTIONS  ]

        public static void RegisterFastMethod<CType, RType>(string id, string methodName, Func<CType, RType> function) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}()";
            }
            s_cachedMethods[id] = new Method<CType, RType>(function)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[0],
            };
        }

        public static void RegisterFastMethod<CType, T1, RType>(string id, string methodName, Func<CType, T1, RType> function, string p1) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name})";
            }
            s_cachedMethods[id] = new Method<CType, T1, RType>(function)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)) },
            };
        }

        public static void RegisterFastMethod<CType, T1, T2, RType>(string id, string methodName, Func<CType, T1, T2, RType> function, string p1, string p2) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name},{typeof(T2).Name})";
            }
            s_cachedMethods[id] = new Method<CType, T1, T2, RType>(function)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)), (p2, typeof(T2)) },
            };
        }

        public static void RegisterFastMethod<CType, T1, T2, T3, RType>(string id, string methodName, Func<CType, T1, T2, T3, RType> function, string p1, string p2, string p3) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name},{typeof(T2).Name},{typeof(T3).Name})";
            }
            s_cachedMethods[id] = new Method<CType, T1, T2, T3, RType>(function)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)), (p2, typeof(T2)), (p3, typeof(T3)) },
            };
        }

        public static void RegisterFastMethod<CType, T1, T2, T3, T4, RType>(string id, string methodName, Func<CType, T1, T2, T3, T4, RType> function, string p1, string p2, string p3, string p4) where CType : Component
        {
            if (string.IsNullOrEmpty(id))
            {
                id = $"{typeof(CType).AssemblyQualifiedName}->{methodName}({typeof(T1).Name},{typeof(T2).Name},{typeof(T3).Name},{typeof(T4).Name})";
            }
            s_cachedMethods[id] = new Method<CType, T1, T2, T3, T4, RType>(function)
            {
                Id = id,
                Name = methodName,
                parameterTypenames = new (string, Type)[] { (p1, typeof(T1)), (p2, typeof(T2)), (p3, typeof(T3)), (p4, typeof(T4)) },
            };
        }

        #endregion

        #endregion
    }

    [Serializable]
    public abstract class Method
    {
        internal (string parName, Type parTYpe)[] parameterTypenames;

        public string Id { get; internal set; }
        public (string name, Type type)[] Parameters => parameterTypenames;
        public string Name { get; internal set; }
        public string FullName => $"{Name} ({ParametersString})";
        public abstract Type ReturnType { get; }

        public string ParametersString
        {
            get
            {
                string full = string.Empty;
                if (parameterTypenames.Length > 0)
                {
                    for (int i = 0; i < parameterTypenames.Length - 1; i++)
                    {
                        full += parameterTypenames[i].parTYpe.Name + ",";
                    }
                    full += parameterTypenames[parameterTypenames.Length - 1].parTYpe.Name;
                }
                return full;
            }
        }

        internal Method()
        {
        }

        public abstract object Invoke(object caller, params object[] pars);
    }

    [Serializable]
    public class ReflectedMethod : Method
    {
        private bool m_isGameObject;
        private Type m_componentType;
        private MethodInfo m_methodInfo;

        public override Type ReturnType => m_methodInfo.ReturnType;

        public MethodInfo MethodInfo => m_methodInfo;

        internal ReflectedMethod(MethodInfo info)
        {
            m_methodInfo = info;
            m_isGameObject = info.DeclaringType == typeof(GameObject);
            m_componentType = typeof(Component).IsAssignableFrom(info.DeclaringType) ? info.DeclaringType : null;
        }

        public override object Invoke(object caller, params object[] pars)
        {
            if (m_isGameObject)
            {
                if (caller is GameObject go)
                {
                    return Invoke(go, pars);
                }
                else if (caller is Component c)
                {
                    return Invoke(c.gameObject, pars);
                }
            }
            else if (m_componentType != null)
            {
                if (caller is Component c)
                {
                    return Invoke(m_componentType.IsAssignableFrom(c.GetType()) ? c : c.GetComponent(m_componentType), pars);
                }
                else if (caller is GameObject go)
                {
                    return Invoke(go.GetComponent(m_componentType), pars);
                }
            }
            return m_methodInfo.Invoke(caller, pars);
        }

        public object Invoke(GameObject caller, params object[] pars) => m_methodInfo.Invoke(caller, pars);

        public object Invoke(Component caller, params object[] pars) => m_methodInfo.Invoke(caller, pars);
    }

    public class VoidMethod : Method
    {
        private Action m_action;

        public override Type ReturnType => typeof(void);

        internal VoidMethod(Action action) => m_action = action;

        internal VoidMethod(MethodInfo info) => m_action = Delegate.CreateDelegate(typeof(Action), info) as Action;

        public override object Invoke(object caller, params object[] pars)
        {
            m_action();
            return null;
        }
    }

    public class VoidMethod<CType> : Method where CType : Component
    {
        private Action<CType> m_action;

        public override Type ReturnType => typeof(void);

        internal VoidMethod(Action<CType> action) => m_action = action;

        public override object Invoke(object caller, params object[] pars)
        {
            m_action(caller as CType);
            return null;
        }
    }

    public class VoidMethod<CType, T1> : Method where CType : Component
    {
        private Action<CType, T1> m_action;

        public override Type ReturnType => typeof(void);

        internal VoidMethod(Action<CType, T1> action) => m_action = action;

        public override object Invoke(object caller, params object[] pars)
        {
            m_action(caller as CType, pars[0] is T1 p1 ? p1 : default);
            return null;
        }
    }

    public class VoidMethod<CType, T1, T2> : Method where CType : Component
    {
        private Action<CType, T1, T2> m_action;

        public override Type ReturnType => typeof(void);

        internal VoidMethod(Action<CType, T1, T2> action) => m_action = action;

        public override object Invoke(object caller, params object[] pars)
        {
            m_action(caller as CType, 
                     pars[0] is T1 p1 ? p1 : default,
                     pars[1] is T2 p2 ? p2 : default);
            return null;
        }
    }

    public class VoidMethod<CType, T1, T2, T3> : Method where CType : Component
    {
        private Action<CType, T1, T2, T3> m_action;

        public override Type ReturnType => typeof(void);

        internal VoidMethod(Action<CType, T1, T2, T3> action) => m_action = action;

        public override object Invoke(object caller, params object[] pars)
        {
            m_action(caller as CType,
                     pars[0] is T1 p1 ? p1 : default,
                     pars[1] is T2 p2 ? p2 : default,
                     pars[2] is T3 p3 ? p3 : default);
            return null;
        }
    }

    public class VoidMethod<CType, T1, T2, T3, T4> : Method where CType : Component
    {
        private Action<CType, T1, T2, T3, T4> m_action;

        public override Type ReturnType => typeof(void);

        internal VoidMethod(Action<CType, T1, T2, T3, T4> action) => m_action = action;

        public override object Invoke(object caller, params object[] pars)
        {
            m_action(caller as CType,
                     pars[0] is T1 p1 ? p1 : default,
                     pars[1] is T2 p2 ? p2 : default,
                     pars[2] is T3 p3 ? p3 : default,
                     pars[3] is T4 p4 ? p4 : default);
            return null;
        }
    }

    public class Method<CType, RType> : Method where CType : Component
    {
        private Func<CType, RType> m_function;

        public override Type ReturnType => typeof(RType);

        internal Method(Func<CType, RType> function) => m_function = function;

        public override object Invoke(object caller, params object[] pars)
        {
            return m_function(caller as CType);
        }
    }

    public class Method<CType, T1, RType> : Method where CType : Component
    {
        private Func<CType, T1, RType> m_function;

        public override Type ReturnType => typeof(RType);

        internal Method(Func<CType, T1, RType> function) => m_function = function;

        public override object Invoke(object caller, params object[] pars)
        {
            return m_function(caller as CType, 
                              pars[0] is T1 p1 ? p1 : default);
        }
    }

    public class Method<CType, T1, T2, RType> : Method where CType : Component
    {
        private Func<CType, T1, T2, RType> m_function;

        public override Type ReturnType => typeof(RType);

        internal Method(Func<CType, T1, T2, RType> function) => m_function = function;

        public override object Invoke(object caller, params object[] pars)
        {
            return m_function(caller as CType,
                              pars[0] is T1 p1 ? p1 : default,
                              pars[1] is T2 p2 ? p2 : default);
        }
    }

    public class Method<CType, T1, T2, T3, RType> : Method where CType : Component
    {
        private Func<CType, T1, T2, T3, RType> m_function;

        public override Type ReturnType => typeof(RType);

        internal Method(Func<CType, T1, T2, T3, RType> function) => m_function = function;

        public override object Invoke(object caller, params object[] pars)
        {
            return m_function(caller as CType,
                              pars[0] is T1 p1 ? p1 : default,
                              pars[1] is T2 p2 ? p2 : default,
                              pars[2] is T3 p3 ? p3 : default);
        }
    }

    public class Method<CType, T1, T2, T3, T4, RType> : Method where CType : Component
    {
        private Func<CType, T1, T2, T3, T4, RType> m_function;

        public override Type ReturnType => typeof(RType);

        internal Method(Func<CType, T1, T2, T3, T4, RType> function) => m_function = function;

        public override object Invoke(object caller, params object[] pars)
        {
            return m_function(caller as CType,
                              pars[0] is T1 p1 ? p1 : default,
                              pars[1] is T2 p2 ? p2 : default,
                              pars[2] is T3 p3 ? p3 : default,
                              pars[3] is T4 p4 ? p4 : default);
        }
    }
}
