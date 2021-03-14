using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [System.Serializable]
    public abstract class LocalizedItem
    {
        [SerializeField]
        protected string m_key;

        public string Key => m_key;

        public abstract void UpdateLanguages();
    }

    [System.Serializable]
    public abstract class LocalizedItem<T> : LocalizedItem, ISerializationCallbackReceiver
    {
        [SerializeField]
        protected T m_defaultValue;
        [SerializeField]
        protected bool m_hasInitializerValue;
        [System.NonSerialized]
        protected T m_initializerValue;
        // [SerializeField]
        //protected SerializableDictionary<string, T> m_values;

        public T CurrentValue {
            get {
                if (!LocalizationManager.IsReady || !LocalizationManager.Current.CurrentLanguage)
                {
                    return m_defaultValue;
                }
                if (m_hasInitializerValue)
                {
                    T returnValue = m_initializerValue;
                    Values[LocalizationManager.Current.CurrentLanguage.Name] = m_initializerValue;
                    InitializerValue = default;
                    return returnValue;

                }
                return this[LocalizationManager.Current.CurrentLanguage.Name];
            }
        }

        public T DefaultValue => m_defaultValue;

        protected T InitializerValue {
            get => m_initializerValue;
            set {
                m_initializerValue = value;
                m_hasInitializerValue = !Equals(m_initializerValue, default);
            }
        }

        protected LocalizedItem(T initialValue)
        {
            InitializerValue = initialValue;
            m_defaultValue = initialValue;
        }

        public LocalizedItem() : this(default) { }

        public abstract SerializableDictionary<string, T> Values {
            get;
        }

        protected virtual bool IsDefaultValue(T value) => Equals(value, default);
        protected virtual bool IsDefaultValue(ref T value) => Equals(value, default);

        public override void UpdateLanguages()
        {
            if (!LocalizationManager.IsReady) { return; }

            var table = LocalizationManager.Current.Table;

            if (!table) { return; }

            table.Refresh();
            var copyDictionary = new Dictionary<string, T>(Values);
            Values.Clear();
            foreach (var lang in table.Languages)
            {
                Values.Add(lang.Name, copyDictionary.TryGetValue(lang.Name, out T value) ? value : default);
            }
            if (m_hasInitializerValue)
            {
                Values[LocalizationManager.Current?.CurrentLanguage.Name] = m_initializerValue;
                InitializerValue = default;
            }
        }

        public static implicit operator T(LocalizedItem<T> localizedItem)
        {
            return localizedItem != null ? localizedItem.CurrentValue : default;
        }

        public virtual T this[string languageName] {
            get {
                return languageName != null && Values.TryGetValue(languageName, out T value) && !IsDefaultValue(ref value) ? value : m_defaultValue;
            }
        }

        public virtual T this[Language language] => this[language?.Name];

        protected void CopyTo<TL>(TL item) where TL : LocalizedItem<T>
        {
            item.m_key = m_key;
            item.m_defaultValue = m_defaultValue;
            item.m_hasInitializerValue = m_hasInitializerValue;
            item.Values.Clear();
            foreach (var pair in Values)
            {
                item.Values.Add(pair.Key, pair.Value);
            }
        }

        public override string ToString()
        {
            return CurrentValue?.ToString();
        }

        public void OnBeforeSerialize()
        {
            if (IsDefaultValue(ref m_defaultValue))
            {
                if (LocalizationManager.IsReady
                    && LocalizationManager.Current.DefaultLanguage
                    && Values.TryGetValue(LocalizationManager.Current.DefaultLanguage.Name, out T v)
                    && !IsDefaultValue(v))
                {
                    m_defaultValue = Values[LocalizationManager.Current.DefaultLanguage.Name];
                }
                else
                {
                    foreach (var key in Values.Keys)
                    {
                        if (!IsDefaultValue(Values[key]))
                        {
                            m_defaultValue = Values[key];
                            break;
                        }
                    }
                }
            }
        }

        public void OnAfterDeserialize()
        {
            // Nothing to do
        }
    }
}
