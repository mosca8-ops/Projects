using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.RemoteControl
{
    [CustomEditor(typeof(BaseCommandUnit), true)]
    public class CommandUnitEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement commands = new VisualElement();
            AddStyleSheetPath(commands, "styles/WeavrRC");
            commands.AddToClassList("command-unit");
            StringBuilder sb = new StringBuilder();
            foreach(var method in target.GetType().GetMethods())
            {
                if(method.GetAttribute<RemotelyCalledAttribute>() != null)
                {
                    VisualElement command = new VisualElement();
                    command.AddToClassList("command");
                    Label elemType = new Label();
                    elemType.AddToClassList("elem-type");
                    command.Add(elemType);
                    Label label = new Label();
                    command.Add(label);

                    if(method.ReturnType == typeof(void))
                    {
                        command.AddToClassList("simple");
                        elemType.text = "SIMPLE";
                    }
                    else
                    {
                        command.AddToClassList("can-return");
                        elemType.text = "ASYNC";
                    }

                    Type retType = method.ReturnType;
                    sb.Clear().Append(' ').Append(method.Name).Append(' ').Append('(');
                    var pars = method.GetParameters();
                    if(pars?.Length > 0)
                    {
                        if(typeof(Delegate).IsAssignableFrom(pars[pars.Length - 1].ParameterType))
                        {
                            command.RemoveFromClassList("simple");
                            command.RemoveFromClassList("can-return");
                            command.AddToClassList("delayed");

                            elemType.text = "DELAYED";

                            if (pars.Last().ParameterType.IsGenericType)
                            {
                                retType = pars.Last().ParameterType.GetGenericArguments()[0];
                            }
                            pars = pars.Take(pars.Length - 1).ToArray();

                            command.tooltip = "This command has a delayed return value";
                        }

                        foreach(var p in pars)
                        {
                            sb.Append(GetTypeName(p.ParameterType)).Append(' ').Append(p.Name).Append(',').Append(' ');
                        }
                        sb.Length -= 2;
                    }
                    
                    sb.Append(')');

                    label.text = GetTypeName(retType) + sb.ToString();

                    commands.Add(command);
                }
            }


            foreach (var e in target.GetType().GetEvents(System.Reflection.BindingFlags.Public 
                                                        | System.Reflection.BindingFlags.Instance 
                                                        | System.Reflection.BindingFlags.Static 
                                                        | System.Reflection.BindingFlags.NonPublic 
                                                        | System.Reflection.BindingFlags.FlattenHierarchy))
            {
                VisualElement command = new VisualElement();
                command.AddToClassList("command");
                Label elemType = new Label("EVENT");
                elemType.AddToClassList("elem-type");
                command.Add(elemType);
                Label label = new Label();
                command.Add(label);
                command.AddToClassList("is-event");

                var method = e.EventHandlerType.GetMethod("Invoke");

                sb.Clear().Append("event ").Append(e.Name).Append(' ').Append('<');
                var pars = method.GetParameters();
                if (pars?.Length > 0)
                {
                    foreach (var p in pars)
                    {
                        sb.Append(GetTypeName(p.ParameterType)).Append(' ').Append(p.Name).Append(',').Append(' ');
                    }
                    sb.Length -= 2;
                }

                sb.Append('>');

                label.text = sb.ToString();

                commands.Add(command);
            }

            return commands;
        }

        public static void AddStyleSheetPath(VisualElement elem, string styleSheet)
        {
            var ss = WeavrStyles.StyleSheets.GetStyleSheet(styleSheet, elem.GetType());
            if (ss && !elem.styleSheets.Contains(ss))
            {
                elem.styleSheets.Add(ss);
            }
        }

        private string GetTypeName(Type type)
        {
            if(type == typeof(byte)) return "byte";
            if(type == typeof(sbyte)) return "sbyte";
            if(type == typeof(short)) return "short";
            if(type == typeof(ushort)) return "ushort";
            if(type == typeof(int)) return "int";
            if(type == typeof(uint)) return "uint";
            if(type == typeof(long)) return "long";
            if(type == typeof(ulong)) return "ulong";
            if(type == typeof(float)) return "float";
            if(type == typeof(double)) return "double";
            if(type == typeof(decimal)) return "decimal";
            if(type == typeof(object)) return "object";
            if(type == typeof(bool)) return "bool";
            if(type == typeof(char)) return "char";
            if(type == typeof(string)) return "string";
            if(type == typeof(void)) return "void";

            if (type == typeof(byte[])) return "byte[]";
            if (type == typeof(sbyte[])) return "sbyte[]";
            if (type == typeof(short[])) return "short[]";
            if (type == typeof(ushort[])) return "ushort[]";
            if (type == typeof(int[])) return "int[]";
            if (type == typeof(uint[])) return "uint[]";
            if (type == typeof(long[])) return "long[]";
            if (type == typeof(ulong[])) return "ulong[]";
            if (type == typeof(float[])) return "float[]";
            if (type == typeof(double[])) return "double[]";
            if (type == typeof(decimal[])) return "decimal[]";
            if (type == typeof(object[])) return "object[]";
            if (type == typeof(bool[])) return "bool[]";
            if (type == typeof(char[])) return "char[]";
            if (type == typeof(string[])) return "string[]";

            return type.Name;
        }
    }
}
