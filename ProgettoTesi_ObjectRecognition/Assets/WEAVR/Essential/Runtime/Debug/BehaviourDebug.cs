using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Debugging
{

    [AddComponentMenu("WEAVR/Debug/Behaviour Debug")]
    public class BehaviourDebug : MonoBehaviour, IDebugBehaviour
    {
        [Draggable]
        [GenericComponent]
        public MonoBehaviour behaviour;
        [Draggable]
        public DebugLine lineSample;

        public DebugColors colors = new DebugColors()
        {
            fieldColor = Color.white,
            propertyColor = Color.yellow,
            eventColor = Color.cyan,
            methodColor = Color.magenta,
            changeColor = Color.green,
            nullColor = Color.red,
        };

        [SerializeField]
        [Tooltip("Whether to include all members or not")]
        private bool m_allMembers;

        [SerializeField]
        [HideInInspector]
        private Text m_moduleTitle;

        [SerializeField]
        [InvokeOnChange(nameof(UpdateIsActive))]
        private bool m_isActive = true;

        [SerializeField]
        [HideInInspector]
        private List<DebugLine> m_lines;
        
        private List<MemberInfo> m_members;
        [SerializeField]
        [HideInInspector]
        private List<string> m_visibleMembers;
        [SerializeField]
        [HideInInspector]
        private List<string> m_hiddenMembers;

        [SerializeField]
        [HideInInspector]
        private MonoBehaviour m_lastBehaviour;

        public string ModuleTitle
        {
            get
            {
                if (m_moduleTitle == null)
                {
                    m_moduleTitle = GetComponentInChildren<Text>();
                }
                return m_moduleTitle != null ? m_moduleTitle.text : "";
            }
            set
            {
                if (m_moduleTitle == null)
                {
                    m_moduleTitle = GetComponentInChildren<Text>();
                }
                if(m_moduleTitle != null)
                {
                    m_moduleTitle.text = value;
                }
                name = $"Debug_{value}";
            }
        }

        public bool IsActive
        {
            get
            {
                return m_isActive;
            }
            set
            {
                if(m_isActive != value)
                {
                    m_isActive = value;
                    UpdateIsActive();
                }
            }
        }

        private void Reset()
        {
            m_lastBehaviour = null;
            m_isActive = true;
            colors.fieldColor = Color.white;
            colors.propertyColor = Color.yellow;
            colors.eventColor = Color.cyan;
            colors.methodColor = Color.magenta;
            colors.changeColor = Color.green;
            colors.nullColor = Color.red;
        }

        private void CreateLines()
        {
            if(m_lastBehaviour == behaviour || m_lines == null) { return; }
            if(behaviour == null)
            {
                m_lastBehaviour = null;
                while(m_lines.Count > 0)
                {
                    DestroyImmediate(m_lines[m_lines.Count - 1]);
                    m_lines.RemoveAt(m_lines.Count - 1);
                }
            }
            m_lastBehaviour = behaviour;

            m_members = new List<MemberInfo>();
            var behaviourType = behaviour.GetType();
            foreach(var fieldInfo in behaviourType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if(fieldInfo.GetCustomAttribute<System.ObsoleteAttribute>() != null)
                {
                    continue;
                }
                if(fieldInfo.DeclaringType == behaviourType && (fieldInfo.IsPublic || fieldInfo.GetCustomAttribute<CanDebugAttribute>() != null))
                {
                    m_members.Add(fieldInfo);
                    m_visibleMembers.Add(fieldInfo.Name);
                }
                else if(fieldInfo.GetCustomAttribute<SerializeField>() != null)
                {
                    m_hiddenMembers.Add(fieldInfo.Name);
                    if (m_allMembers)
                    {
                        m_visibleMembers.Add(fieldInfo.Name);
                    }
                }
            }

            foreach (var propertyInfo in behaviour.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!propertyInfo.CanRead || propertyInfo.GetCustomAttribute<System.ObsoleteAttribute>() != null)
                {
                    continue;
                }
                if (propertyInfo.DeclaringType == behaviourType && propertyInfo.GetMethod.IsPublic)
                {
                    m_members.Add(propertyInfo);
                    m_visibleMembers.Add(propertyInfo.Name);
                }
                else if(propertyInfo.GetCustomAttribute<CanDebugAttribute>() != null)
                {
                    m_hiddenMembers.Add(propertyInfo.Name);
                    if (m_allMembers)
                    {
                        m_visibleMembers.Add(propertyInfo.Name);
                    }
                }
            }

            m_lines.Clear();
            m_lines.AddRange(GetComponentsInChildren<DebugLine>());

            while (m_lines.Count < m_members.Count)
            {
                var newLine = Instantiate(lineSample.gameObject) as GameObject;
                newLine.transform.SetParent(transform, false);
                newLine.SetActive(true);
                m_lines.Add(newLine.GetComponent<DebugLine>());
            }

            while(m_lines.Count > 0 && m_lines.Count > m_members.Count)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_lines[m_lines.Count - 1]);
                }
                else
                {
                    DestroyImmediate(m_lines[m_lines.Count - 1]);
                }
                m_lines.RemoveAt(m_lines.Count - 1);
            }
        }

        public void AddLine(MemberInfo memberInfo) {
            if (!Contains(memberInfo)) {
                var newLine = Instantiate(lineSample.gameObject) as GameObject;
                newLine.SetActive(true);
                newLine.transform.SetParent(transform, false);
                var debugLine = newLine.GetComponent<DebugLine>();
                if (debugLine.Save(memberInfo)) {
                    debugLine.UpdateColors(colors);
                    m_lines.Add(debugLine);
                } else {
                    Destroy(newLine);
                }
            }
        }

        public void RemoveLine(DebugLine line)
        {
            if (m_lines.Remove(line))
            {
                if (Application.isPlaying)
                {
                    Destroy(line.gameObject);
                }
                else
                {
                    DestroyImmediate(line.gameObject);
                }
            }
        }

        public bool Contains(MemberInfo memberInfo) {
            foreach(var line in m_lines) {
                if (line.HasMember(memberInfo)) {
                    return true;
                }
            }
            return false;
        }

        private void FillLines()
        {
            if(m_members == null) { return; }
            for (int i = 0; i < m_members.Count && i < m_lines.Count; i++)
            {
                m_lines[i].Save(m_members[i]);
            }
            if(behaviour != null && m_members.Count > 0)
            {
                for (int i = 0; i < m_lines.Count; i++)
                {
                    m_lines[i].PrepareData(behaviour);
                    m_lines[i].UpdateColors(colors);
                }
            }
            m_members = null;
        }

        public void OnValidate()
        {
            FillLines();
            CreateLines();
            if(behaviour != null)
            {
                ModuleTitle = $"{behaviour.GetType().Name} [{behaviour.gameObject.name}]";
            }
        }

        public void ChangeBehaviour(MonoBehaviour behaviour)
        {
            this.behaviour = behaviour;
            OnValidate();
        }

        // Use this for initialization
        void Start()
        {
            if (behaviour != null)
            {
                foreach (var line in m_lines)
                {
                    line.PrepareData(behaviour);
                }
            }
            var toggle = GetComponentInChildren<Toggle>();
            if(toggle != null)
            {
                toggle.isOn = m_isActive;
            }
        }

        public void ToggleLines(bool expand)
        {
            m_isActive = expand;
            foreach (var line in m_lines)
            {
                if (line != null)
                {
                    line.gameObject.SetActive(expand);
                }
            }
        }

        public void UpdateInfo()
        {
            if (behaviour != null)
            {
                foreach (var line in m_lines)
                {
                    line.UpdateInfo(behaviour, 1);
                }
            }
        }

        public void UpdateInfo(int updateRate)
        {
            if (behaviour != null)
            {
                foreach (var line in m_lines)
                {
                    line.UpdateColors(colors);
                    line.UpdateInfo(behaviour, updateRate);
                }
            }
        }

        private void UpdateIsActive()
        {
            ToggleLines(m_isActive);
        }

        [System.Serializable]
        public struct DebugColors
        {
            public Color fieldColor;
            public Color propertyColor;
            public Color eventColor;
            public Color methodColor;
            public Color changeColor;
            public Color nullColor;
        }
    }

}
