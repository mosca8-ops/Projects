using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [System.Serializable]
    public class LocalizedAudioClip : LocalizedItem<AudioClip>
    {
        [System.Serializable]
        private class DictionaryOfStringAndAudioClip : SerializableDictionary<string, AudioClip> { }

        [SerializeField]
        private DictionaryOfStringAndAudioClip m_values;

        public LocalizedAudioClip(): base() { }

        private LocalizedAudioClip(AudioClip clip): base(clip) { }

        public override SerializableDictionary<string, AudioClip> Values
        {
            get
            {
                if(m_values == null)
                {
                    m_values = new DictionaryOfStringAndAudioClip();
                }
                return m_values;
            }
        }

        public static implicit operator LocalizedAudioClip(AudioClip value)
        {
            return new LocalizedAudioClip(value);
        }

        public void SetAudioClip(string language, AudioClip clip)
        {
            if (m_values.ContainsKey(language))
            {
                m_values[language] = clip;
            }
        }
    }
}
