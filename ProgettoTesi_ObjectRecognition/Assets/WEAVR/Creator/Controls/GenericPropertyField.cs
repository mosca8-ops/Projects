using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using Generator = System.Func<string, UnityEngine.UIElements.VisualElement>;

namespace TXT.WEAVR
{

    public class GenericPropertyField
    {
        private enum RegisterCause
        {
            Default,
            Requested,
            Inherited,
        }

        private static Dictionary<string, Generator> s_typenameHandlers = new Dictionary<string, Generator>()
        {
            { "int", s => new LabeledField<IntegerField, int>(s) },
            { "string", s => new LabeledField<TextField, string>(s) },
            { "float", s => new LabeledField<FloatField, float>(s) },
            { "double", s => new LabeledField<FloatField, float>(s) },
            { "Vector3", s => new LabeledField<Vector3Field, Vector3>(s) },
            { "int", s => new LabeledField<IntegerField, int>(s) },
            { "int", s => new LabeledField<IntegerField, int>(s) },
        };

        private static HashSet<Type> s_inheritedHandlers = new HashSet<Type>();

        //private static Dictionary<Type, Generator> s_typeHandlers = new Dictionary<Type, Generator>()
        //{
        //    { typeof(int), s => new LabeledField<IntegerField, int>(s) },
        //    { typeof(string), s => new LabeledField<TextField, string>(s) },
        //    { typeof(float), s => new LabeledField<FloatField, float>(s) },
        //    { typeof(double), s => new LabeledField<FloatField, float>(s) },
        //};

        public static void RegisterHandler(Type type, Generator generator)
        {
            if(!s_typenameHandlers.ContainsKey(type.Name))
            {
                s_typenameHandlers[type.Name] = generator;
                if (type.IsValueType)
                {
                    return;
                }

                s_inheritedHandlers.Remove(type);
                if (type.IsInterface)
                {
                    foreach(var subType in type.GetAllImplementations())
                    {
                        if(!s_typenameHandlers.ContainsKey(subType.Name) || s_inheritedHandlers.Contains(subType))
                        {
                            s_typenameHandlers[subType.Name] = generator;
                        }
                    }
                    return;
                }

                foreach(var subType in type.GetAllLeafClasses())
                {
                    GetHandlerFromParent(subType);
                }
            }
        }

        private static Generator GetHandlerFromParent(Type type)
        {
            if(type == null)
            {
                return null;
            }
            if(!s_inheritedHandlers.Contains(type) && s_typenameHandlers.TryGetValue(type.Name, out Generator handler))
            {
                return handler;
            }

            Generator parentGenerator = GetHandlerFromParent(type.BaseType);
            if(parentGenerator != null)
            {
                s_inheritedHandlers.Add(type);
                s_typenameHandlers[type.Name] = parentGenerator;

                return parentGenerator;
            }
            return null;
        }

        public static VisualElement CreateField(SerializedProperty property)
        {
            if(s_typenameHandlers.TryGetValue(property.type, out Generator generator))
            {
                var element = generator(property.displayName);
                if (element != null)
                {
                    if (element is IBindable)
                    {
                        (element as IBindable).BindProperty(property);
                    }
                    return element;
                }
            }
            return TryGenerateCompoundField(property);
        }

        private static VisualElement TryGenerateCompoundField(SerializedProperty property)
        {
            throw new NotImplementedException();
        }
    }
}
