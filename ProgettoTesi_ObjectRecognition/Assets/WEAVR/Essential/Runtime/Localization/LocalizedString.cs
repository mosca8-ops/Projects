using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [System.Serializable]
    public class LocalizedString : LocalizedItem<string>
    {
        [SerializeField]
        private DictionaryOfStringAndString m_values;

        public LocalizedString() { }

        public LocalizedString(string value) : base(value) { }

        protected override bool IsDefaultValue(ref string value) => string.IsNullOrEmpty(value);

        public override SerializableDictionary<string, string> Values
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

        public static implicit operator LocalizedString(string value)
        {
            return new LocalizedString(value);
        }

        public static LocalizedString GetFrom(DictionaryOfStringAndString dictionary)
        {
            LocalizedString str = new LocalizedString();
            str.Values.Clear();
            foreach (var lang in LocalizationManager.Current.Table.Languages)
            {
                str.m_values.Add(lang.Name, null);
            }
            foreach (var pair in dictionary)
            {
                if (str.m_values.ContainsKey(pair.Key))
                {
                    str.m_values[pair.Key] = pair.Value;
                }
            }

            return str;
        }
        
        public LocalizedString Clone()
        {
            var newString = new LocalizedString();
            CopyTo(newString);
            return newString;
        }
    }

    [System.Serializable]
    public class OptionalLocalizedString : Optional<LocalizedString>
    {
        public static implicit operator OptionalLocalizedString(LocalizedString value)
        {
            return new OptionalLocalizedString()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator string(OptionalLocalizedString optional)
        {
            return optional.value;
        }

        public static implicit operator LocalizedString(OptionalLocalizedString optional)
        {
            return optional.value;
        }

        public static implicit operator OptionalLocalizedString(string value)
        {
            return new OptionalLocalizedString()
            {
                enabled = true,
                value = new LocalizedString(value)
            };
        }
    }
}
