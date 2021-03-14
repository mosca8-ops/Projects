using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class SearchHub
    {
        public delegate void OnSearchValueChange(string newValue);
        public delegate bool SearchCallback(object target, string value);
        public delegate bool SearchCallback<T>(T target, string value);

        #region [  STATIC PART  ]

        private static List<string> k_selfList = new List<string>() { "__from_callback__" };

        private static SearchHub s_instance;
        public static SearchHub Current
        {
            get
            {
                if(s_instance == null)
                {
                    s_instance = new SearchHub();
                }
                return s_instance;
            }
        }

        #endregion

        private bool m_isEmpty = true;
        private string m_searchValue;
        private Dictionary<object, SearchResult> m_searchResults;
        private Dictionary<Type, SearchCallback> m_searchCallbacks; 

        public string CurrentSearchValue
        {
            get => m_searchValue;
            set
            {
                if(m_searchValue != value)
                {
                    m_searchValue = value?.Trim() ?? string.Empty;
                    m_isEmpty = string.IsNullOrEmpty(m_searchValue);
                    m_searchResults.Clear();
                    SearchValueChanged?.Invoke(m_searchValue);
                }
            }
        }
        public event OnSearchValueChange SearchValueChanged;

        public IEnumerable<object> FoundObjects => m_searchResults.Keys;


        private SearchHub()
        {
            m_searchResults = new Dictionary<object, SearchResult>();
            m_searchCallbacks = new Dictionary<Type, SearchCallback>();
        }

        public void RegisterSearchCallback<T>(SearchCallback<T> callback)
        {
            m_searchCallbacks[typeof(T)] = (o, v) => callback((T)o, v);
        }

        public void UnregisterSearchCallback<T>()
        {
            m_searchCallbacks.Remove(typeof(T));
        }

        public void Reset()
        {
            CurrentSearchValue = string.Empty;
        }

        public IReadOnlyList<string> GetFoundProperties(object target)
        {
            return target != null && m_searchResults.TryGetValue(target, out SearchResult result) ? result.Properties : null;
        }

        public SearchResult SearchCached(object target)
        {
            if(target == null || m_isEmpty) { return null; }

            if(!m_searchResults.TryGetValue(target, out SearchResult result))
            {
                result = Search(target);
                m_searchResults[target] = result;
            }
            return result;
        }

        public SearchResult Search(object target)
        {
            if(target == null || m_isEmpty) { return null; }
            var type = target.GetType();
            
            if(m_searchCallbacks.TryGetValue(type, out SearchCallback callback) && callback(target, CurrentSearchValue))
            {
                return new SearchResult(target, k_selfList);
            }
            if (target is UnityEngine.Object obj && obj)
            {
                List<string> properties = new List<string>();
                using (SerializedObject serObj = new SerializedObject(obj))
                {
                    if (serObj != null)
                    {
                        string value = CurrentSearchValue.ToLower();
                        var property = serObj.FindProperty("m_Script");
                        while (property.NextVisible(property.propertyType == SerializedPropertyType.Generic))
                        {
                            if(FindInProperty(property, value))
                            {
                                properties.Add(property.propertyPath);
                            }
                        }
                    }
                }
                return new SearchResult(target, properties);
            }

            return null;
        }

        
        public bool FastSearch(object target)
        {
            if (target == null || m_isEmpty) { return false; }

            if (m_searchResults.TryGetValue(target, out SearchResult result) && result != null && result.IsValid)
            {
                return true;
            }
            if (m_searchCallbacks.TryGetValue(target.GetType(), out SearchCallback callback) && callback(target, CurrentSearchValue))
            {
                return true;
            }
            if (target is UnityEngine.Object obj)
            {
                using (SerializedObject serObj = new SerializedObject(obj))
                {
                    if (serObj != null)
                    {
                        string value = CurrentSearchValue.ToLower();
                        var property = serObj.FindProperty("m_Script");
                        while (property.NextVisible(property.propertyType == SerializedPropertyType.Generic))
                        {
                            if (FindInProperty(property, value))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void RemoveFromCache(object target)
        {
            m_searchResults.Remove(target);
        }
        
        private static bool FindInProperty(SerializedProperty property, string value)
        {
            string result = null;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    result = property.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    result = property.floatValue.ToString();
                    break;
                case SerializedPropertyType.Integer:
                    result = property.intValue.ToString();
                    break;
                case SerializedPropertyType.String:
                    result = property.stringValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    result = property.objectReferenceValue?.ToString();
                    break;
                case SerializedPropertyType.Enum:
                    result = property.enumValueIndex >= 0 ? property.enumNames[property.enumValueIndex] : null;
                    break;
                case SerializedPropertyType.Character:
                    result = ((char)property.intValue).ToString();
                    break;
                case SerializedPropertyType.ExposedReference:
                    result = property.exposedReferenceValue?.ToString();
                    break;
            }

            return !string.IsNullOrEmpty(result) && (result.ToLower().Contains(value) || result.Replace(" ", "").ToLower().Contains(value));
        }


        public class SearchResult
        {
            private object m_target;
            private List<string> m_properties;

            public object Target => m_target;
            public IReadOnlyList<string> Properties => m_properties;

            public bool IsValid => m_properties != null && m_properties.Count > 0;

            public SearchResult(object target, List<string> properties)
            {
                m_target = target;
                m_properties = properties;
            }
        }
    }
}
