using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Speech
{

    public class TextToSpeechManager
    {
        #region [  STATIC PART  ]

        private static TextToSpeechManager s_instance;
        public static TextToSpeechManager Current
        {
            get
            {
                if(s_instance == null)
                {
                    s_instance = new TextToSpeechManager();
                }
                return s_instance;
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        [InitializeOnLoadMethod]
        static void EditorInitialization()
        {
            if (s_instance == null)
            {
                s_instance = new TextToSpeechManager();
            }
        }

        #endregion

        private List<ITextToSpeechHandler> m_handlers;
        private Dictionary<string, ITextToSpeechHandler> m_handlersTable;

        public IReadOnlyList<ITextToSpeechHandler> Handlers => m_handlers;
        public IReadOnlyDictionary<string, ITextToSpeechHandler> HandlersTable => m_handlersTable;

        private PersistentVoicesDictionary s_lastVoices;
        public PersistentVoicesDictionary LastVoices
        {
            get
            {
                if (s_lastVoices == null)
                {
                    s_lastVoices = new PersistentVoicesDictionary();
                }
                return s_lastVoices;
            }
        }


        public ITextToSpeechHandler SafeGetHandler(string handlerType, string voiceName)
            => m_handlersTable.TryGetValue(handlerType, out ITextToSpeechHandler handler) ? 
                handler : 
                string.IsNullOrEmpty(voiceName) ? 
                    m_handlers.FirstOrDefault() : 
                    (m_handlers.FirstOrDefault(h => h.AllVoices.Any(v => v.Name == voiceName)) ?? m_handlers.FirstOrDefault());

        public IEnumerable<ITextToSpeechHandler> GetCompatibleHandlers(string voiceName) => m_handlers.Where(h => h.AllVoices.Any(v => v.Name == voiceName));

        public ITextToSpeechHandler GetCompatibleOrDefaultHandler(string voiceName) => m_handlers.FirstOrDefault(h => h.AllVoices.Any(v => v.Name == voiceName)) ?? m_handlers.FirstOrDefault();

        private TextToSpeechManager()
        {
            m_handlers = new List<ITextToSpeechHandler>();
            m_handlersTable = new Dictionary<string, ITextToSpeechHandler>();
            foreach (var handlerType in typeof(ITextToSpeechHandler).GetAllImplementations())
            {
                try
                {
                    var handler = Activator.CreateInstance(handlerType);
                    if (handler is ITextToSpeechHandler h)
                    {
                        m_handlers.Add(h);
                        m_handlersTable.Add(handlerType.Name, h);
                    }
                }
                catch(Exception e)
                {
                    WeavrDebug.LogError(this, $"Unable to instantiate {handlerType.Name}. Exception: {e.Message} \n {e.StackTrace}");
                }
            }
        }

        public class PersistentVoicesDictionary : IDictionary<string, (string handler, string voice)>
        {
            private Dictionary<string, (string, string)> m_dictionary = new Dictionary<string, (string, string)>();

            public (string handler, string voice) this[string key] {
                get
                {
                    if(!m_dictionary.TryGetValue(key, out (string handler, string voice) item) && EditorPrefs.HasKey(key + "_h") && EditorPrefs.HasKey(key + "_h"))
                    {
                        item.handler = EditorPrefs.GetString(key + "_h");
                        item.voice = EditorPrefs.GetString(key + "_v");
                        m_dictionary[key] = item;
                    }
                    return item;
                }
                set
                {
                    m_dictionary[key] = value;
                    EditorPrefs.SetString(key + "_h", value.handler);
                    EditorPrefs.SetString(key + "_v", value.voice);
                }
            }

            public ICollection<string> Keys => m_dictionary.Keys;

            public ICollection<(string, string)> Values => m_dictionary.Values;

            public int Count => m_dictionary.Count;

            public bool IsReadOnly => false;
            
            public void Add(string key, (string handler, string voice) value)
            {
                m_dictionary.Add(key, value);
                EditorPrefs.SetString(key + "_h", value.handler);
                EditorPrefs.SetString(key + "_v", value.voice);
            }

            public void Add(KeyValuePair<string, (string, string)> item)
            {
                Add(item.Key, item.Value);
            }

            public void Clear()
            {
                foreach(var key in m_dictionary.Keys)
                {
                    EditorPrefs.DeleteKey(key + "_h");
                    EditorPrefs.DeleteKey(key + "_v");
                }
                m_dictionary.Clear();
            }

            public bool Contains(KeyValuePair<string, (string, string)> item) => m_dictionary.Contains(item);

            public bool ContainsKey(string key)
            {
                if (m_dictionary.ContainsKey(key))
                {
                    return true;
                }
                else if(EditorPrefs.HasKey(key + "_h") && EditorPrefs.HasKey(key + "_v"))
                {
                    m_dictionary[key] = (EditorPrefs.GetString(key + "_h"), EditorPrefs.GetString(key + "_v"));
                    return true;
                }
                return false;
            }

            public void CopyTo(KeyValuePair<string, (string, string)>[] array, int arrayIndex)
            {
                
            }

            public IEnumerator<KeyValuePair<string, (string, string)>> GetEnumerator() => m_dictionary.GetEnumerator();

            public bool Remove(string key)
            {
                if (m_dictionary.Remove(key))
                {
                    EditorPrefs.DeleteKey(key + "_h");
                    EditorPrefs.DeleteKey(key + "_v");
                    return true;
                }
                return false;
            }

            public bool Remove(KeyValuePair<string, (string, string)> item) => Remove(item.Key);

            public bool TryGetValue(string key, out (string, string) value)
            {
                if(m_dictionary.TryGetValue(key, out value))
                {
                    return true;
                }
                else if (EditorPrefs.HasKey(key + "_h") && EditorPrefs.HasKey(key + "_v"))
                {
                    value = (EditorPrefs.GetString(key + "_h"), EditorPrefs.GetString(key + "_v"));
                    m_dictionary[key] = value;
                    return true;
                }
                return false;
            }

            IEnumerator IEnumerable.GetEnumerator() => m_dictionary.GetEnumerator();
        }
    }
}
