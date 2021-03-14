using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Localization
{

    [AddComponentMenu("WEAVR/Setup/Localization Manager")]
    public class LocalizationManager : MonoBehaviour, IWeavrSingleton
    {

        #region [  STATIC PART  ]

        private static LocalizationManager s_instance;
        /// <summary>
        /// Gets the current LocalizationManager
        /// </summary>
        public static LocalizationManager Current
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = Weavr.GetInCurrentScene<LocalizationManager>();
                    if (!s_instance)
                    {
                        s_instance = new GameObject("LocalizationManager").AddComponent<LocalizationManager>();
                    }
                    //s_instance = CreateInstance<IDBookkeeper>();
                    //_instance.OnEnable();
                }
                return s_instance;
            }
        }

        public static bool IsReady => s_instance != null;

        public static LocalizationTable CurrentTable => s_instance ? s_instance.Table : null;

        public static string Translate(string value)
        {
            // TODO: Implement the translate part in Localization Manager
            return value;
        }
        
        #endregion

        [SerializeField]
        private LocalizationTable m_table;

        [SerializeField]
        [ArrayElement("m_table.m_languages", notNull: true)]
        private Language m_currentLanguage;

        [Space]
        [SerializeField]
        private UnityEvent m_currentLanguageChanged;
        [SerializeField]
        private UnityEvent m_tableChanged;

        public event Action<Language> CurrentLanguageChanged;
        public event Action<LocalizationTable> TableChanged;

        public Language CurrentLanguage
        {
            get { return m_currentLanguage; }
            set
            {
                if(m_currentLanguage != value)
                {
                    if (m_table)
                    {
                        m_currentLanguage = value && m_table.Contains(value) ? value 
                                          : m_currentLanguage && m_table.Contains(m_currentLanguage) ? m_currentLanguage 
                                          : m_table.DefaultLanguage;
                    }
                    else
                    {
                        m_currentLanguage = value;
                    }
                    m_currentLanguageChanged?.Invoke();
                    CurrentLanguageChanged?.Invoke(m_currentLanguage);
                }
            }
        }

        public Language DefaultLanguage => m_table ? m_table.DefaultLanguage : m_currentLanguage;

        public LocalizationTable Table
        {
            get { return m_table; }
            set
            {
                if(m_table != value)
                {
                    m_table = value;
                    m_tableChanged?.Invoke();
                    TableChanged?.Invoke(m_table);

                    if(m_table != null && (m_currentLanguage == null || !m_table.Contains(m_currentLanguage)))
                    {
                        CurrentLanguage = Table.DefaultLanguage;
                    }
                    else
                    {
                        CurrentLanguage = null;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (s_instance && s_instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(this);
                }
                else
                {
                    DestroyImmediate(this);
                }
                return;
            }
            s_instance = this;
        }

        private void OnDestroy()
        {
            if(s_instance == this)
            {
                s_instance = null;
            }
        }

        private void OnValidate()
        {
            if(m_table == null)
            {
                CurrentLanguage = null;
            }
            else if(CurrentLanguage == null || !m_table.Contains(CurrentLanguage))
            {
                CurrentLanguage = m_table.DefaultLanguage;
            }
        }
    }
}
