using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [DefaultExecutionOrder(-28400)]
    public class ProcedureDefaults : ScriptableObject
    {
        internal static Action<UnityEngine.Object, ProcedureDefaults> s_Persist;
        internal static string DefaultsPath => WeavrEditor.PATH + "Creator/Resources/defaults/";
        private static ProcedureDefaults s_instance;

        public static ProcedureDefaults Current
        {
            get
            {
                if(!s_instance)
                {
                    s_instance = Retrieve<ProcedureDefaults>("ProcedureDefaults");
                }
                return s_instance;
            }
        }

        [SerializeField]
        private List<Language> m_languages;
        [SerializeField]
        private List<ExecutionMode> m_executionModes;
        [SerializeField]
        private LocalizationTable m_localizationTable;
        [SerializeField]
        private ActionsCatalogue m_actionsCatalogue;
        [SerializeField]
        private ConditionsCatalogue m_conditionsCatalogue;
        [SerializeField]
        private AnimationsCatalogue m_animationBlocksCatalogue;
        [SerializeField]
        private ColorPalette m_colorPalette;
        [SerializeField]
        private List<ProcedureConfig> m_templates;
        [SerializeField]
        private ProcedureCanvasTester m_procedureCanvasTester;

        public LocalizationTable LocalizationTable => m_localizationTable;
        public IReadOnlyList<Language> Languages => m_languages;
        public IReadOnlyList<ExecutionMode> ExecutionModes => m_executionModes;
        public IReadOnlyList<ProcedureConfig> Templates => m_templates;
        public ActionsCatalogue ActionsCatalogue
        {
            get
            {
                if (!m_actionsCatalogue)
                {
                    m_actionsCatalogue = Retrieve<ActionsCatalogue>("DefaultActionsCatalogue");
                }
                return m_actionsCatalogue;
            }
        }
        public ConditionsCatalogue ConditionsCatalogue
        {
            get
            {
                if (!m_conditionsCatalogue)
                {
                    m_conditionsCatalogue = Retrieve<ConditionsCatalogue>("DefaultConditionsCatalogue");
                }
                return m_conditionsCatalogue;
            }
        }
        public AnimationsCatalogue AnimationBlocksCatalogue
        {
            get
            {
                if (!m_animationBlocksCatalogue)
                {
                    m_animationBlocksCatalogue = Retrieve<AnimationsCatalogue>("DefaultAnimationBlocksCatalogue");
                }
                return m_animationBlocksCatalogue;
            }
        }
        public ColorPalette ColorPalette
        {
            get
            {
                if (!m_colorPalette)
                {
                    m_colorPalette = Retrieve<ColorPalette>("DefaultColorPalette");
                }
                return m_colorPalette;
            }
        }
        public ProcedureCanvasTester ProcedureCanvasTester
        {
            get
            {
                if (!m_procedureCanvasTester)
                {
                    m_procedureCanvasTester = Retrieve<ProcedureCanvasTester>("ProcedureCanvasTester");
                }
                return m_procedureCanvasTester;
            }
        }

        private void OnEnable()
        {
            if(m_languages == null)
            {
                m_languages = new List<Language>();
            }
            if(m_executionModes == null)
            {
                m_executionModes = new List<ExecutionMode>();
            }
            if(m_templates == null)
            {
                m_templates = new List<ProcedureConfig>();
            }
            if (ColorPalette.Global)
            {
                ColorPalette.Global.AddGroupsFrom(ColorPalette);
            }
        }

        internal void AddExecutionMode(ExecutionMode mode)
        {
            if (!mode)
            {
                mode = CreateInstance<ExecutionMode>();
                mode.name = $"ExecutionMode {m_executionModes.Count}";
                s_Persist?.Invoke(mode, this);
            }
            m_executionModes.Add(mode);
        }

        internal void AddTemplate(ProcedureConfig template)
        {
            if (!template)
            {
                template = CreateInstance<ProcedureConfig>();
                template.name = $"Template " + m_templates.Count;
                s_Persist?.Invoke(template, this);
            }
            m_templates.Add(template);
        }

        private static T Retrieve<T>(string name) where T : ScriptableObject
        {
            T value = FindObjectOfType<T>();
            if(!value)
            {
                value = AssetDatabase.LoadAssetAtPath<T>(WeavrEditor.PATH + $"Creator/Resources/defaults/{name}.asset");
            }
            if(!value)
            {
                value = CreateInstance<T>();
                AssetDatabase.CreateAsset(value, WeavrEditor.PATH + $"Creator/Resources/defaults/{name}.asset");
            }
            return value;
        }
    }
}
