using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Editor
{
    public class GlobalValuesWindow : EditorWindow
    {
        private GlobalValues m_component;

        private Vector2 m_scrollPosition;
        private Dictionary<string, (ValuesStorage.Variable variable, VisualElement element)> m_variables = new Dictionary<string, (ValuesStorage.Variable variable, VisualElement element)>();
        private Dictionary<VisualElement, IVisualElementScheduledItem> m_animations = new Dictionary<VisualElement, IVisualElementScheduledItem>();

        public VisualElement Root { get; private set; }
        public GlobalValues GlobalValues { get; private set; }
        public VisualElement VariablesContainer { get; private set; }

        [MenuItem("WEAVR/Diagnostics/Variables", priority = 10)]
        public static void ShowWindow()
        {
            GetWindow<GlobalValuesWindow>("Variables");
        }

        private void OnEnable()
        {
            rootVisualElement.StretchToParentSize();
            Root = WeavrStyles.CreateFromTemplate("Windows/GlobalValuesWindow");
            Root.AddStyleSheetPath("Styles/GlobalValuesWindow");

            rootVisualElement.Add(Root);
            Root.StretchToParentSize();

            VariablesContainer = Root.Q("container");
        }

        private void Update()
        {
            if(GlobalValues.HasAnyValue)
            {
                if (!GlobalValues)
                {
                    Initialize();
                }
            }
            else
            {
                ClearAll();
            }
        }

        private void ClearAll()
        {
            VariablesContainer.Clear();
            m_variables.Clear();
            GlobalValues = null;
            VariablesContainer.Add(new Label("No Variables") { name = "no-variables-label" });
        }

        private void Initialize()
        {
            GlobalValues.VariableAdded -= GlobalValues_VariableAdded;
            GlobalValues.VariableAdded += GlobalValues_VariableAdded;

            GlobalValues.VariableRemoved -= GlobalValues_VariableRemoved;
            GlobalValues.VariableRemoved += GlobalValues_VariableRemoved;

            GlobalValues = GlobalValues.Current;

            var instanceField = Root.Q<ObjectField>("instance");
            instanceField.value = GlobalValues;
            instanceField.SetEnabled(false);

            foreach(var variable in GlobalValues.AllVariables)
            {
                if (variable != null)
                {
                    BuildVariable(variable);
                }
            }
        }

        private void BuildVariable(ValuesStorage.Variable variable)
        {
            var view = new VisualElement();
            view.AddToClassList("variable");

            var highlightView = new VisualElement()
            {
                name = "highlight-area"
            };
            highlightView.AddToClassList("change-listener");
            view.Add(highlightView);

            var varTypeView = new Label(variable.Type.ToString())
            {
                name = "var-type"
            };
            varTypeView.AddToClassList(variable.Type.ToString());
            view.Add(varTypeView);

            switch (variable.Type)
            {
                case ValuesStorage.ValueType.Bool:
                    var boolElem = new Toggle(variable.Name);
                    if (variable.Value is bool bValue)
                    {
                        boolElem.value = bValue;
                    }
                    else
                    {
                        boolElem.value = false;
                        boolElem.Add(CreateUndefinedLabel(variable.Value));
                    }
                    boolElem.RegisterValueChangedCallback(e => variable.Value = e.newValue);
                    variable.ValueChanged += v => ValueChanged(variable, (bool)v, boolElem);
                    view.Add(boolElem);
                    break;
                case ValuesStorage.ValueType.Float:
                    var floatElem = new FloatField(variable.Name);
                    if(variable.Value is float fValue)
                    {
                        floatElem.value = fValue;
                    }
                    else if(variable.Value is int iValue)
                    {
                        floatElem.value = iValue;
                    }
                    else if(variable.Value is double dValue)
                    {
                        floatElem.value = (float)dValue;
                    }
                    else
                    {
                        floatElem.value = 0;
                        floatElem.Add(CreateUndefinedLabel(variable.Value));
                    }
                    floatElem.RegisterValueChangedCallback(e => variable.Value = e.newValue);
                    variable.ValueChanged += v => ValueChanged(variable, (float)v, floatElem);
                    view.Add(floatElem);
                    break;
                case ValuesStorage.ValueType.Integer:
                    var intElem = new IntegerField(variable.Name);
                    if(variable.Value is int intValue)
                    {
                        intElem.value = intValue;
                    }
                    else if(variable.Value is float ifValue)
                    {
                        intElem.value = (int)ifValue;
                    }
                    else if(variable.Value is double idValue)
                    {
                        intElem.value = (int)idValue;
                    }
                    else
                    {
                        intElem.value = 0;
                        intElem.Add(CreateUndefinedLabel(variable.Value));
                    }
                    intElem.RegisterValueChangedCallback(e => variable.Value = e.newValue);
                    variable.ValueChanged += v => ValueChanged(variable, (int)v, intElem);
                    view.Add(intElem);
                    break;
                case ValuesStorage.ValueType.String:
                    var stringElem = new TextField(variable.Name);
                    if(variable.Value is string sValue)
                    {
                        stringElem.value = sValue;
                    }
                    else
                    {
                        stringElem.value = variable.Value?.ToString();
                        stringElem.Add(CreateUndefinedLabel(variable.Value));
                    }
                    stringElem.RegisterValueChangedCallback(e => variable.Value = e.newValue);
                    variable.ValueChanged += v => ValueChanged(variable, v as string, stringElem);
                    view.Add(stringElem);
                    break;
                case ValuesStorage.ValueType.Color:
                    var colorElem = new ColorField(variable.Name);
                    if(variable.Value is Color cValue)
                    {
                        colorElem.value = cValue;
                    }
                    else
                    {
                        colorElem.value = Color.clear;
                        colorElem.Add(CreateUndefinedLabel(variable.Value));
                    }
                    colorElem.RegisterValueChangedCallback(e => variable.Value = e.newValue);
                    variable.ValueChanged += v => ValueChanged(variable, (Color)v, colorElem);
                    view.Add(colorElem);
                    break;
                case ValuesStorage.ValueType.Vector3:
                    var vector3Elem = new Vector3Field()
                    {
                        label = variable.Name,
                    };
                    if(variable.Value is Vector3 vValue)
                    {
                        vector3Elem.value = vValue;
                    }
                    else
                    {
                        vector3Elem.value = Vector3.zero;
                        vector3Elem.Add(CreateUndefinedLabel(variable.Value));
                    }
                    vector3Elem.RegisterValueChangedCallback(e => variable.Value = e.newValue);
                    variable.ValueChanged += v => ValueChanged(variable, (Vector3)v, vector3Elem);
                    view.Add(vector3Elem);
                    break;
                case ValuesStorage.ValueType.Object:
                    var objElem = new ObjectField(variable.Name);
                    objElem.objectType = typeof(UnityEngine.Object);
                    objElem.value = variable.Value as UnityEngine.Object;
                    objElem.RegisterValueChangedCallback(e => variable.Value = e.newValue);
                    variable.ValueChanged += v => ValueChanged(variable, v as UnityEngine.Object, objElem);
                    view.Add(objElem);
                    break;
            }


            m_variables[variable.Name] = (variable, view);

            VariablesContainer.Q("no-variables-label")?.RemoveFromHierarchy();
            VariablesContainer.Add(view);

            MarkAsChanged(view);
        }

        private VisualElement CreateUndefinedLabel(object value)
        {
            Label undefined = new Label($"Undefined <{value}>");
            undefined.AddToClassList("undefined-label");
            return undefined;
        }

        private void RemoveUndefinedLabel(VisualElement field)
        {
            field.Q(className: "undefined-label")?.RemoveFromHierarchy();
        }

        private void ValueChanged(ValuesStorage.Variable variable, UnityEngine.Object v, ObjectField field)
        {
            field.SetValueWithoutNotify(v);
            MarkAsChanged(variable);
        }
        
        private void ValueChanged(ValuesStorage.Variable variable, string v, TextField field)
        {
            field.SetValueWithoutNotify(v);
            RemoveUndefinedLabel(field);
            MarkAsChanged(variable);
        }
        
        private void ValueChanged(ValuesStorage.Variable variable, Color v, ColorField field)
        {
            field.SetValueWithoutNotify(v);
            RemoveUndefinedLabel(field);
            MarkAsChanged(variable);
        }

        private void ValueChanged(ValuesStorage.Variable variable, Vector3 v, Vector3Field field)
        {
            field.SetValueWithoutNotify(v);
            RemoveUndefinedLabel(field);
            MarkAsChanged(variable);
        }

        private void ValueChanged(ValuesStorage.Variable variable, int v, IntegerField field)
        {
            field.SetValueWithoutNotify(v);
            RemoveUndefinedLabel(field);
            MarkAsChanged(variable);
        }

        private void ValueChanged(ValuesStorage.Variable variable, bool v, Toggle field)
        {
            field.SetValueWithoutNotify(v);
            RemoveUndefinedLabel(field);
            MarkAsChanged(variable);
        }

        private void ValueChanged(ValuesStorage.Variable variable, float v, FloatField field)
        {
            field.SetValueWithoutNotify(v);
            RemoveUndefinedLabel(field);
            MarkAsChanged(variable);
        }

        private void MarkAsChanged(ValuesStorage.Variable variable)
        {
            if(m_variables.TryGetValue(variable.Name, out (ValuesStorage.Variable, VisualElement view) item))
            {
                
                MarkAsChanged(item.view);
            }
        }

        private void MarkAsChanged(VisualElement view)
        {
            AnimateChange(view);
        }

        private void AnimateChange(VisualElement view)
        {
            if (m_animations.TryGetValue(view, out IVisualElementScheduledItem animation))
            {
                animation.Pause();
            }
            if (!view.customStyle.TryGetValue(new CustomStyleProperty<Color>("--highlight-color"), out Color highlightColor))
            {
                highlightColor = Color.green;
            }
            view.Query(className: "change-listener").ForEach(v => v.style.backgroundColor = highlightColor);
            m_animations[view] = view.schedule.Execute(timer => AnimateChange(view, timer)).Until(() => AnimationHasEnded(view));
        }

        private void AnimateChange(VisualElement view, TimerState timer)
        {
            if(!view.customStyle.TryGetValue(new CustomStyleProperty<float>("--change-duration"), out float duration))
            {
                duration = 1;
            }
            var delta = timer.deltaTime / (1000f * duration);
            view.Query(className: "change-listener").ForEach(v => v.style.backgroundColor = Color.Lerp(v.style.backgroundColor.value, Color.clear, delta));
        }

        private bool AnimationHasEnded(VisualElement view)
        {
            return view.style.backgroundColor == Color.clear;
        }

        private static Color MoveTowards(Color a, Color b, float delta)
        {
            return new Color(Mathf.MoveTowards(a.r, b.r, delta),
                             Mathf.MoveTowards(a.g, b.g, delta),
                             Mathf.MoveTowards(a.b, b.b, delta),
                             Mathf.MoveTowards(a.a, b.a, delta));
        }

        private void GlobalValues_VariableRemoved(ValuesStorage.Variable variable)
        {
            if(m_variables.TryGetValue(variable.Name, out (ValuesStorage.Variable, VisualElement elem) view))
            {
                view.elem.RemoveFromHierarchy();
                m_variables.Remove(variable.Name);
            }
        }

        private void GlobalValues_VariableAdded(ValuesStorage.Variable variable)
        {
            BuildVariable(variable);
        }
    }
}
