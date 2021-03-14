using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [CreateAssetMenu(fileName = "Language", menuName = "WEAVR/Language")]
    [DefaultExecutionOrder(-29000)]
    public class Language : ScriptableObject
    {
        #region [  STATIC PART  ]
        private static HashSet<Language> s_allLanguages = new HashSet<Language>();
        public static IEnumerable<Language> AllLanguages => s_allLanguages; 

        public static Language Get(string twoLetterIsoName)
        {
            foreach(var lang in s_allLanguages)
            {
                if(lang.TwoLettersISOName == twoLetterIsoName)
                {
                    return lang;
                }
            }
            return null;
        }
        #endregion

        [SerializeField]
        private Texture2D m_icon;
        [SerializeField]
        private string m_name;

        private CultureInfo m_cultureInfo;

        public string Name => m_name;
        public CultureInfo CultureInfo
        {
            get
            {
                if(m_cultureInfo == null && m_name != null)
                {
                    try
                    {
                        var cultureInfo = CultureInfo.GetCultureInfo(m_name);
                        if (cultureInfo != null)
                        {
                            m_cultureInfo = CultureInfo.ReadOnly(cultureInfo);
                        }
                    }
                    catch (CultureNotFoundException)
                    {

                    }
                }
                return m_cultureInfo;
            }
        }


        public Texture2D Icon => m_icon;
        public string DisplayName => CultureInfo?.DisplayName;
        public string EnglishName => CultureInfo?.EnglishName;
        public string NativeName => CultureInfo?.NativeName;
        public string TwoLettersISOName => CultureInfo?.TwoLetterISOLanguageName;

        public void UpdateCultureInfo()
        {
            m_cultureInfo = null;
        }

        private void OnEnable()
        {
            s_allLanguages.Add(this);
        }

        private void OnDestroy()
        {
            s_allLanguages.Remove(this);
        }

        public static Language Create(string language)
        {
            var l = CreateInstance<Language>();
            try
            {
                var cultureInfo = CultureInfo.GetCultureInfo(language);
                if (cultureInfo != null)
                {
                    l.m_cultureInfo = CultureInfo.ReadOnly(cultureInfo);
                }
                l.m_name = language;
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
            return l;
        }

        public override bool Equals(object other)
        {
            return other is Language l && l.m_name == m_name;
        }

        public override int GetHashCode()
        {
            return m_name.GetHashCode();
        }
    }
}
