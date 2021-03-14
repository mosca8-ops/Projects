using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    class ProcedureMessage : VisualElement
    {
        protected const string k_MessageClass = "procedure-message";
        protected const string k_NoName = "__no_name__";
        protected const string k_CallbackFormat = @"([^#\n\r]*)(#[^#\n\r]*#)";

        public class Callback
        {
            public string message { get; private set; }
            public Action action { get; private set; }

            public Callback(string message, Action action)
            {
                this.message = message;
                this.action = action;
            }

            public void Call()
            {
                action?.Invoke();
            }

            public static Callback Ok(Action action)
            {
                return new Callback("Ok", action);
            }

            public static Callback Cancel(Action action)
            {
                return new Callback("Cancel", action);
            }
        }

        private static void LoadTree(VisualElement elem, string templateName, string classname = null)
        {
            var tpl = EditorGUIUtility.Load($"{WeavrEditor.PATH}Creator/Resources/uxml/{templateName}.uxml") as VisualTreeAsset;
            tpl?.CloneTree(elem);

            elem.AddStyleSheetPath("ProcedureMessage");
            elem.AddToClassList(classname ?? k_MessageClass);
        }

        public static VisualElement Show(string title, string message)
        {
            return new SimpleMessage(title, message, null, null);
        }

        public static VisualElement Show(string title, string message, Action okCallback)
        {
            return new SimpleMessage(title, message, Callback.Ok(okCallback), null);
        }

        public static VisualElement Show(string title, string message, Action okCallback, Action cancelCallback)
        {
            return new SimpleMessage(title, message, Callback.Ok(okCallback), Callback.Cancel(cancelCallback));
        }

        public static VisualElement Show(string title, string message, Callback okCallback)
        {
            return new SimpleMessage(title, message, okCallback, null);
        }

        public static VisualElement Show(string title, string message, Callback okCallback, Callback cancelCallback)
        {
            return new SimpleMessage(title, message, okCallback, cancelCallback);
        }

        public static VisualElement ShowMultiple(string title, string message, params Callback[] callbacks)
        {
            return new MultipleButtonsMessage(title, message, callbacks);
        }

        public static VisualElement ShowFormat(string title, string message, params Action[] callbacks)
        {
            return new SmartFormatMessage(title, message, callbacks);
        }

        public static void Show(VisualElement parent, string title, string message)
        {
            parent.Add(new SimpleMessage(title, message, null, null));
        }

        public static void Show(VisualElement parent, string title, string message, Action okCallback)
        {
            parent.Add(new SimpleMessage(title, message, Callback.Ok(okCallback), null));
        }

        public static void Show(VisualElement parent, string title, string message, Action okCallback, Action cancelCallback)
        {
            parent.Add(new SimpleMessage(title, message, Callback.Ok(okCallback), Callback.Cancel(cancelCallback)));
        }

        public static void Show(VisualElement parent, string title, string message, Callback okCallback)
        {
            parent.Add(new SimpleMessage(title, message, okCallback, null));
        }

        public static void Show(VisualElement parent, string title, string message, Callback okCallback, Callback cancelCallback)
        {
            parent.Add(new SimpleMessage(title, message, okCallback, cancelCallback));
        }

        public static void ShowMultiple(VisualElement parent, string title, string message, params Callback[] callbacks)
        {
            parent.Add(new MultipleButtonsMessage(title, message, callbacks));
        }

        public static void ShowFormat(VisualElement parent, string title, string message, params Action[] callbacks)
        {
            parent.Add(new SmartFormatMessage(title, message, callbacks));
        }


        private static Button ApplyButton(VisualElement elem, string name, Callback callback, string classname = null)
        {
            var button = elem.Q<Button>(name);
            if(button == null)
            {
                button = new Button();
                if (!string.IsNullOrEmpty(name) && name != k_NoName)
                {
                    button.name = name;
                }
            }
            if(button != null && callback != null) {
                button.text = callback.message;
                if (callback.action != null)
                {
                    button.clickable.clicked += callback.action;
                }
                else
                {
                    button.SetEnabled(false);
                }
            }
            if (!string.IsNullOrEmpty(classname))
            {
                button.AddToClassList(classname);
            }
            return button;
        }

        private static Label ApplyLabel(VisualElement elem, string name, string text, string classname = null)
        {
            var label = elem.Q<Label>(name);
            if (label == null)
            {
                label = new Label();
                if (!string.IsNullOrEmpty(name) && name != k_NoName)
                {
                    label.name = name;
                }
            }
            if (label != null && text != null)
            {
                label.text = text;
            }
            if (!string.IsNullOrEmpty(classname))
            {
                label.AddToClassList(classname);
            }
            return label;
        }

        private class SimpleMessage : VisualElement
        {

            public SimpleMessage(string title, string message, Callback okCallback, Callback cancelCallback)
            {
                LoadTree(this, "ProcedureSimpleMessage");
                
                ApplyLabel(this, "title-label", title);
                ApplyLabel(this, "message-label", message);
                ApplyButton(this, "ok-button", okCallback).visible = okCallback?.action != null;
                ApplyButton(this, "cancel-button", cancelCallback).visible = cancelCallback?.action != null;
            }
        }

        private class MultipleButtonsMessage : VisualElement
        {
            public MultipleButtonsMessage(string title, string message, params Callback[] callbacks)
            {
                LoadTree(this, "ProcedureMultipleButtonsMessage");

                ApplyLabel(this, "title-label", title);
                ApplyLabel(this, "message-label", message);

                var buttonsContainer = this.Q("buttons-container");
                foreach(var callback in callbacks)
                {
                    buttonsContainer.Add(ApplyButton(this, k_NoName, callback, "button"));
                }
            }
        }

        private class SmartFormatMessage : VisualElement
        {
            public SmartFormatMessage(string title, string message, params Action[] callbacks)
            {
                LoadTree(this, "ProcedureSmartFormatMessage");

                ApplyLabel(this, "title-label", title);

                var messageContainer = this.Q("message-container");

                var regex = new Regex(k_CallbackFormat);

                var splits = message.Split('\n');
                int callback_i = 0;
                foreach (var split in splits)
                {
                    var matches = regex.Matches(split);
                    var line = new VisualElement();
                    line.AddToClassList("message-line");
                    messageContainer.Add(line);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        var match = matches[i];
                        line.Add(ApplyLabel(this, k_NoName, match.Groups[1].Value, "inline-label"));
                        line.Add(ApplyButton(this, k_NoName,
                            new Callback(match.Groups[2].Value.Trim('#'), callback_i < callbacks.Length ? callbacks[callback_i++] : null), "inline-button"));
                    }

                    int lastIndex = split.LastIndexOf('#') + 1;
                    line.Add(ApplyLabel(this, k_NoName, split.Substring(lastIndex), "inline-label"));
                }
            }
        }
    }
}
