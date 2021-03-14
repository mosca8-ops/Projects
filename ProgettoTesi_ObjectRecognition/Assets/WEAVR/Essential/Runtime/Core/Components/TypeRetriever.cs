using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Core
{

    public class TypeRetriever
    {
        private static List<Type> s_allTypes;
        private static Dictionary<Type, Dictionary<Type, Attribute>> s_typesWithAttributes;
        private static Dictionary<Type, List<Type>> s_implementedInterfaces;

        private static List<Type> Types
        {
            get
            {
                if(s_allTypes == null)
                {
                    CacheTypes();
                }
                return s_allTypes;
            }
        }

        private static Dictionary<Type, Dictionary<Type, Attribute>> Attributes
        {
            get
            {
                if(s_typesWithAttributes == null)
                {
                    CacheTypes();
                }
                return s_typesWithAttributes;
            }
        }

        public static IReadOnlyList<Type> AllTypes => Types;

        private static void CacheTypes()
        {
            s_allTypes = new List<Type>();
            s_typesWithAttributes = new Dictionary<Type, Dictionary<Type, Attribute>>();
            s_implementedInterfaces = new Dictionary<Type, List<Type>>();

            Dictionary<Type, Attribute> typesAttributes = null;
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetAssemblyTypes(assembly))
                {
                    s_allTypes.Add(type);
                    if (!type.IsInterface)
                    {
                        foreach (var iface in type.GetInterfaces())
                        {
                            if (!s_implementedInterfaces.TryGetValue(iface, out List<Type> ifaceTypes))
                            {
                                ifaceTypes = new List<Type>();
                                s_implementedInterfaces[iface] = ifaceTypes;
                            }
                            ifaceTypes.Add(type);
                        }
                    }
                    foreach (Attribute attribute in type.GetCustomAttributes(true))
                    {
                        if (s_typesWithAttributes.TryGetValue(attribute.GetType(), out typesAttributes))
                        {
                            typesAttributes[type] = attribute;
                        }
                        else
                        {
                            typesAttributes = new Dictionary<Type, Attribute>();
                            typesAttributes.Add(type, attribute);
                            s_typesWithAttributes[attribute.GetType()] = typesAttributes;
                        }
                    }
                }
            }

            #region Ugly Hack
            //    AppDomain.CurrentDomain.GetAssemblies()
            //.SelectMany(t =>
            //{
            //        // Ugly hack to handle mis-versioned dlls
            //        var innerTypes = new Type[0];
            //    try
            //    {
            //        innerTypes = t.GetTypes();
            //    }
            //    catch { }
            //    return innerTypes;
            //})
            #endregion
        }

        private static Type[] GetAssemblyTypes(System.Reflection.Assembly assembly)
        {
            if (true)
            {
                #region Ugly Hack
                try
                {
                    return assembly.GetTypes();
                }
                catch
                {
                    return new Type[0];
                }
                #endregion
            }

            return assembly.GetTypes();
        }

        public static IEnumerable<Type> GetTypesWhichImplement<T>() => GetTypesWhichImplement(typeof(T));

        public static IEnumerable<Type> GetTypesWhichImplement(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                return new Type[0];
            }
            if(s_implementedInterfaces == null)
            {
                CacheTypes();
            }
            return s_implementedInterfaces.TryGetValue(interfaceType, out List<Type> types) ? types : new List<Type>();
        }

        public static IEnumerable<Type> GetTypesWithAttribute<T>() where T : Attribute
        {
            Type attributeType = typeof(T);
            Dictionary<Type, Attribute> typesDictionary = null;
            if (Attributes.TryGetValue(attributeType, out typesDictionary))
            {
                return typesDictionary.Keys;
            }
            return null;
        }

        public static IEnumerable<KeyValuePair<Type, T>> GetAttributes<T>() where T : Attribute
        {
            Type attributeType = typeof(T);
            Dictionary<Type, Attribute> typesDictionary = null;
            List<KeyValuePair<Type, T>> list = new List<KeyValuePair<Type, T>>();
            if (Attributes.TryGetValue(attributeType, out typesDictionary))
            {
                foreach(var pair in typesDictionary)
                {
                    list.Add(new KeyValuePair<Type, T>(pair.Key, pair.Value as T));
                }
            }
            return list;
        }
    }
}
