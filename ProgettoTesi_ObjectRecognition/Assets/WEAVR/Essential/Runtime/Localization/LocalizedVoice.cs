using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [Serializable]
    public class LocalizedTTS : LocalizedItem<TextToSpeechPair>
    {
        [SerializeField]
        private DictionaryOfStringAndTTS m_values = new DictionaryOfStringAndTTS();

        public override SerializableDictionary<string, TextToSpeechPair> Values => m_values;

        public static LocalizedTTS Merge(Dictionary<string, string> texts, Dictionary<string, string> voices)
        {
            LocalizedTTS elem = new LocalizedTTS();
            elem.Values.Clear();
            foreach (var lang in LocalizationManager.Current.Table.Languages)
            {
                elem.m_values.Add(lang.Name, new TextToSpeechPair());
            }
            foreach (var pair in texts)
            {
                if (elem.m_values.ContainsKey(pair.Key))
                {
                    elem.m_values[pair.Key].text = pair.Value;
                    if (voices.TryGetValue(pair.Key, out string voice))
                    {
                        elem.m_values[pair.Key].voiceName = voice;
                    }
                }
            }

            return elem;
        }

        public LocalizedTTS Clone()
        {
            LocalizedTTS clone = new LocalizedTTS();
            clone.m_values = new DictionaryOfStringAndTTS();
            foreach(var pair in m_values)
            {
                clone.m_values[pair.Key] = pair.Value?.Clone();
            }
            return clone;
        }
    }

    [Serializable] public class DictionaryOfStringAndTTS : SerializableDictionary<string, TextToSpeechPair> { }

    [Serializable]
    public class TextToSpeechPair
    {
        public string voiceName;
        public string text;
        public string handler;

        public TextToSpeechPair Clone()
        {
            return new TextToSpeechPair()
            {
                voiceName = voiceName,
                text = text,
                handler = handler,
            };
        }
    }
}
