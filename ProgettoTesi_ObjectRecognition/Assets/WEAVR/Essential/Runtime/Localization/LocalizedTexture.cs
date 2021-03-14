using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [System.Serializable]
    public class LocalizedTexture : LocalizedItem<Texture>
    {
        [System.Serializable]
        private class DictionaryOfStringAndTexture : SerializableDictionary<string, Texture> { }

        [SerializeField]
        private DictionaryOfStringAndTexture m_values;

        public override SerializableDictionary<string, Texture> Values
        {
            get
            {
                if (m_values == null)
                {
                    m_values = new DictionaryOfStringAndTexture();
                }
                return m_values;
            }
        }
    }

    [System.Serializable]
    public class LocalizedTexture2D : LocalizedItem<Texture2D>
    {
        [System.Serializable]
        private class DictionaryOfStringAndTexture2D : SerializableDictionary<string, Texture2D> { }

        [SerializeField]
        private DictionaryOfStringAndTexture2D m_values;

        public override SerializableDictionary<string, Texture2D> Values
        {
            get
            {
                if (m_values == null)
                {
                    m_values = new DictionaryOfStringAndTexture2D();
                }
                return m_values;
            }
        }
    }
}
