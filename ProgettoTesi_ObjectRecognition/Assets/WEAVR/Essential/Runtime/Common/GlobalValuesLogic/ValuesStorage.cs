using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    public delegate void OnVariableDelegate(ValuesStorage storage, ValuesStorage.Variable variable);

    public class ValuesStorage
    {
        public event OnVariableDelegate VariableAdded;
        public event OnVariableDelegate VariableRemoved;

        public enum ValueType { Any, Bool, Float, Integer, String, Color, Vector3, Object }
        public enum AccessType { Read, Write, ReadWrite }

        private Dictionary<string, Variable> m_variables = new Dictionary<string, Variable>();

        public IEnumerable<Variable> AllVariables => m_variables.Values;

        public int Count => m_variables.Count;

        public void SetVariable(string name)
        {
            GetOrCreateVariable(name, ValueType.Any).Value = null;
        }

        private Variable AddVariable(Variable variable)
        {
            if (m_variables.TryGetValue(variable.Name, out Variable existing))
            {
                VariableRemoved?.Invoke(this, existing);
            }
            m_variables[variable.Name] = variable;
            VariableAdded?.Invoke(this, variable);

            return variable;
        }

        public void RemoveVariable(string name)
        {
            if (m_variables.TryGetValue(name, out Variable variable))
            {
                m_variables.Remove(name);
                VariableRemoved?.Invoke(this, variable);
            }
        }

        public bool VariableExists(string name) => m_variables.ContainsKey(name);

        public void SetValue(string name, bool value)
        {
            GetOrCreateVariable(name, ValueType.Bool, value).Value = value;
        }

        public void SetValue(string name, int value)
        {
            GetOrCreateVariable(name, ValueType.Integer, value).Value = value;
        }

        internal void ResetVariable(string name)
        {
            if(m_variables.TryGetValue(name, out Variable var))
            {
                switch (var.Type)
                {
                    case ValueType.Bool: var.Value = false; break;
                    case ValueType.Float: var.Value = 0; break;
                    case ValueType.Integer: var.Value = 0; break;
                    case ValueType.Color: var.Value = Color.clear; break;
                    case ValueType.Vector3: var.Value = Vector3.zero; break;
                    case ValueType.String: var.Value = string.Empty; break;
                    case ValueType.Any: RemoveVariable(name); break;
                    default: var.Value = null; break;
                }
            }
        }

        public void SetValue(string name, float value)
        {
            GetOrCreateVariable(name, ValueType.Float, value).Value = value;
        }

        public void SetValue(string name, string value)
        {
            GetOrCreateVariable(name, ValueType.String, value).Value = value;
        }

        public void SetValue(string name, Color value)
        {
            GetOrCreateVariable(name, ValueType.Color, value).Value = value;
        }

        public void SetValue(string name, Vector3 value)
        {
            GetOrCreateVariable(name, ValueType.Vector3, value).Value = value;
        }

        public void SetValue<T>(string name, T value) where T : class
        {
            GetOrCreateVariable(name, ValueType.Object, value).Value = value;
        }

        public object GetValue(string name) => m_variables.TryGetValue(name, out Variable variable) ? variable.Value : null;
        public bool GetValue(string name, bool fallbackValue) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Bool ? (bool)variable.Value : fallbackValue;
        public int GetValue(string name, int fallbackValue) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Integer ? (int)variable.Value : fallbackValue;
        public float GetValue(string name, float fallbackValue) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Float ? (float)variable.Value : fallbackValue;
        public string GetValue(string name, string fallbackValue) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.String ? variable.Value as string : fallbackValue;
        public Color GetValue(string name, Color fallbackValue) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Color && variable.Value is Color c ? c : fallbackValue;
        public Vector3 GetValue(string name, Vector3 fallbackValue) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Vector3 && variable.Value is Vector3 v ? v : fallbackValue;
        public T GetValue<T>(string name, T fallbackValue) => m_variables.TryGetValue(name, out Variable variable) && variable.Value is T tValue ? tValue : fallbackValue;
        public T GetComponent<T>(string name, T fallbackValue) where T : Component
        {
            if (!m_variables.TryGetValue(name, out Variable variable))
            {
                return fallbackValue;
            }
            if(variable.Value is T tValue)
            {
                return tValue;
            }
            if(variable.Value is GameObject go)
            {
                return go.GetComponent<T>();
            }
            else if(variable.Value is Component c)
            {
                return c.GetComponent<T>();
            }
            return null;
        }

        public GameObject GetGameObject(string name, GameObject fallbackValue)
        {
            if (!m_variables.TryGetValue(name, out Variable variable))
            {
                return fallbackValue;
            }
            if (variable.Value is GameObject go)
            {
                return go;
            }
            else if (variable.Value is Component c)
            {
                return c.gameObject;
            }
            return null;
        }

        public bool? GetBool(string name) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Bool ? (bool)variable.Value : (bool?)null;
        public int? GetInt(string name) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Integer ? (int)variable.Value : (int?)null;
        public float? GetFloat(string name) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Float ? (float)variable.Value : (float?)null;
        public string GetString(string name) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.String ? variable.Value as string : null;
        public Color? GetColor(string name) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Color && variable.Value is Color c ? c : (Color?)null;
        public Vector3? GetVector3(string name) => m_variables.TryGetValue(name, out Variable variable) && variable.Type == ValueType.Vector3 && variable.Value is Vector3 v ? v : (Vector3?)null;

        public Variable GetVariable(string name) => m_variables.TryGetValue(name, out Variable variable) ? variable : null;
        public Variable GetOrCreateVariable(string name) => m_variables.TryGetValue(name, out Variable variable) ? variable : AddVariable(new Variable()
        {
            Name = name,
            Type = ValueType.Any,
            Value = null,
        });

        public Variable GetOrCreateVariable(string name, ValueType valueType)
        {
            if (m_variables.TryGetValue(name, out Variable var) && var.Type == valueType)
            {
                return var;
            }
            return AddVariable(new Variable()
            {
                Name = name,
                Type = valueType
            });
        }

        public Variable GetOrCreateVariable(string name, ValueType valueType, object defaultValue)
        {
            if (m_variables.TryGetValue(name, out Variable var) && var.Type == valueType)
            {
                return var;
            }
            return AddVariable(new Variable()
            {
                Name = name,
                Type = valueType,
                Value = defaultValue,
            });
        }


        public class Variable
        {
            public string Name;
            public ValueType Type;

            private object m_value;
            public object Value
            {
                get => m_value;
                set
                {
                    if(!Equals(m_value, value))
                    {
                        m_value = value;
                        ValueChanged?.Invoke(m_value);
                    }
                }
            }

            public event Action<object> ValueChanged;
        }
    }
}
