using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Core.DataTypes;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("")]
    public class GlobalValues : MonoBehaviour
    {
        public delegate void VariableDelegate(ValuesStorage.Variable variable);

        public static event VariableDelegate VariableAdded;
        public static event VariableDelegate VariableRemoved;

        private static GlobalValues s_instance;
        public static GlobalValues Current
        {
            get
            {
                if (!s_instance)
                {
                    s_instance = new GameObject("GlobalValues").AddComponent<GlobalValues>();
                    SpecialStateContainers.Register(c => new GlobalValuesState(c as GlobalValues));
                }
                return s_instance;
            }
        }

        public static bool HasAnyValue => s_instance && s_instance.m_storage.AllVariables.Any();

        public static object ValueOf(string variableName) => Current.GetValue(variableName);

        private ValuesStorage m_storage = new ValuesStorage();

        // Start is called before the first frame update
        void Awake()
        {
            if(s_instance && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            m_storage.VariableAdded -= Storage_VariableAdded;
            m_storage.VariableAdded += Storage_VariableAdded;
            m_storage.VariableRemoved -= Storage_VariableRemoved;
            m_storage.VariableRemoved += Storage_VariableRemoved;
            DontDestroyOnLoad(s_instance.gameObject);
        }

        private void OnDestroy()
        {
            if (m_storage != null)
            {
                m_storage.VariableAdded -= Storage_VariableAdded;
                m_storage.VariableRemoved -= Storage_VariableRemoved;
            }
        }

        private void Storage_VariableRemoved(ValuesStorage storage, ValuesStorage.Variable variable)
        {
            if(storage == m_storage)
            {
                VariableRemoved?.Invoke(variable);
            }
        }

        private void Storage_VariableAdded(ValuesStorage storage, ValuesStorage.Variable variable)
        {
            if(storage == m_storage)
            {
                VariableAdded?.Invoke(variable);
            }
        }

        public IEnumerable<ValuesStorage.Variable> AllVariables => m_storage.AllVariables;

        public int Count => m_storage.Count;

        public void SetVariable(string name) => m_storage.SetVariable(name);
        public void ResetVariable(string name) => m_storage.ResetVariable(name);
        public void RemoveVariable(string name) => m_storage.RemoveVariable(name);
        public bool VariableExists(string name) => m_storage.VariableExists(name);
        public void SetValue(string name, bool value) => m_storage.SetValue(name, value);
        public void SetValue(string name, int value) => m_storage.SetValue(name, value);
        public void SetValue(string name, float value) => m_storage.SetValue(name, value);
        public void SetValue(string name, Color value) => m_storage.SetValue(name, value);
        public void SetValue(string name, Vector3 value) => m_storage.SetValue(name, value);
        public void SetValue(string name, string value) => m_storage.SetValue(name, value);
        public void SetValue<T>(string name, T value) where T : class => m_storage.SetValue(name, value);

        public object GetValue(string name) => m_storage.GetValue(name);
        public bool GetValue(string name, bool fallbackValue) => m_storage.GetValue(name, fallbackValue);
        public int GetValue(string name, int fallbackValue) => m_storage.GetValue(name, fallbackValue);
        public float GetValue(string name, float fallbackValue) => m_storage.GetValue(name, fallbackValue);
        public string GetValue(string name, string fallbackValue) => m_storage.GetValue(name, fallbackValue);
        public Color GetValue(string name, Color fallbackValue) => m_storage.GetValue(name, fallbackValue);
        public Vector3 GetValue(string name, Vector3 fallbackValue) => m_storage.GetValue(name, fallbackValue);
        public T GetValue<T>(string name, T fallbackValue) where T : class => m_storage.GetValue(name, fallbackValue);
        public T GetComponentValue<T>(string name, T fallbackValue) where T : Component => m_storage.GetComponent(name, fallbackValue);
        public GameObject GetGameObject(string name, GameObject fallbackValue) => m_storage.GetGameObject(name, fallbackValue);

        public bool? GetBool(string name) => m_storage.GetBool(name);
        public int? GetInt(string name) => m_storage.GetInt(name);
        public float? GetFloat(string name) => m_storage.GetFloat(name);
        public Color? GetColor(string name) => m_storage.GetColor(name);
        public Vector3? GetVector3(string name) => m_storage.GetVector3(name);
        public string GetString(string name) => m_storage.GetString(name);
        public T GetValue<T>(string name) where T : class => m_storage.GetValue(name) as T;

        public ValuesStorage.Variable GetVariable(string name) => m_storage.GetVariable(name);
        public ValuesStorage.Variable GetOrCreateVariable(string name) => m_storage.GetOrCreateVariable(name);
        public ValuesStorage.Variable GetOrCreateVariable(string name, ValuesStorage.ValueType valueType) => m_storage.GetOrCreateVariable(name, valueType);
        public ValuesStorage.Variable GetOrCreateVariable(string name, ValuesStorage.ValueType valueType, object defaultValue) => m_storage.GetOrCreateVariable(name, valueType, defaultValue);
    }
}
