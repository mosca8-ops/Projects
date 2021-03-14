using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [CreateAssetMenu(fileName = "LocalizationTable", menuName = "WEAVR/LocalizationTable")]
    public class LocalizationTable : ScriptableObject
    {
        [SerializeField]
        [ArrayElement(nameof(m_languages), notNull: true)]
        private Language m_defaultLanguage;
        [SerializeField]
        private List<Language> m_languages;

        [Space]
        [SerializeField]
        private List<Row> m_rows;

        public List<Row> Rows => m_rows;
        public List<Language> Languages => m_languages;

        public Language DefaultLanguage
        {
            get
            {
                if(m_defaultLanguage == null && m_languages?.Count > 0)
                {
                    m_defaultLanguage = m_languages[0];
                }
                return m_defaultLanguage;
            }
        }

        private Dictionary<string, Row> m_keysDictionary;
        private Dictionary<string, Language> m_languagesDictionary;

        public static LocalizationTable Create(IEnumerable<Language> languages)
        {
            var table = CreateInstance<LocalizationTable>();
            foreach (var lang in languages)
            {
                if (lang)
                {
                    table.m_languages.Add(lang);
                }
            }
            table.Refresh();
            return table;
        }

        private void OnEnable()
        {
            if (m_rows == null)
            {
                m_rows = new List<Row>();
            }
            if (m_languages == null)
            {
                m_languages = new List<Language>();
            }
            Refresh();
        }

        public void Refresh()
        {
            for (int i = 0; i < m_languages.Count; i++)
            {
                if (!m_languages[i])
                {
                    m_languages.RemoveAt(i--);
                }
            }
            UpdateDictionaries();
        }

        public void UpdateDictionaries()
        {
            if (m_keysDictionary == null)
            {
                m_keysDictionary = new Dictionary<string, Row>();
            }
            else
            {
                m_keysDictionary.Clear();
            }
            foreach (var row in m_rows)
            {
                m_keysDictionary[row.key] = row;
            }

            if (m_languagesDictionary == null)
            {
                m_languagesDictionary = new Dictionary<string, Language>();
            }
            else
            {
                m_languagesDictionary.Clear();
            }
            foreach (var lang in m_languages)
            {
                m_languagesDictionary[lang.Name] = lang;
            }
        }

        public bool Contains(Language language)
        {
            return m_languages.Contains(language);
        }

        public bool ContainsLanguage(string languageName)
        {
            for (int i = 0; i < m_languages.Count; i++)
            {
                if(m_languages[i].Name == languageName)
                {
                    return true;
                }
            }
            return false;
        }

        public Language GetLanguage(string name)
        {
            return m_languagesDictionary.TryGetValue(name, out Language lang) ? lang : null;
        }

        public string this[string key]
        {
            get
            {
                return this[key, DefaultLanguage];
            }
            set
            {
                this[key, DefaultLanguage] = value;
            }
        }

        public string this[string key, string language]
        {
            get
            {
                return m_keysDictionary.TryGetValue(key, out Row row) ? row.values[language] : null;
            }
            set
            {

            }
        }

        public string this[string key, Language language]
        {
            get
            {
                return this[key, language?.Name];
            }
            set
            {
                this[key, language?.Name] = value;
            }
        }

        [System.Serializable]
        public class Row
        {
            [SerializeField]
            public string m_key;

            public string key => m_key;

            [SerializeField]
            private DictionaryOfStringAndString m_values;
            public DictionaryOfStringAndString values
            {
                get
                {
                    if(m_values == null)
                    {
                        m_values = new DictionaryOfStringAndString();
                    }
                    return m_values;
                }
            }

            public Row(string key)
            {
                m_key = key;
                m_values = new DictionaryOfStringAndString();
            }
        }
    }
}
