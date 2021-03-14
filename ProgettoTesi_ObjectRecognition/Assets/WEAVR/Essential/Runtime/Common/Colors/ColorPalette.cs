using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "WEAVR/Color Palette")]
    [DefaultExecutionOrder(-28490)]
    public class ColorPalette : ScriptableObject
    {
        public static Action<ColorPalette> s_fullSave;

        private const string k_NumberPattern = @"([\w\s\d]+)(\d+)";

        private static ColorPalette s_global;
        public static ColorPalette Global
        {
            get
            {
                if (Application.isEditor && !s_global)
                {
                    s_global = CreateInstance<ColorPalette>();
                }
                return s_global;
            }
        }

        [SerializeField]
        private string m_title;
        [SerializeField]
        private bool m_autoGenerate;
        [SerializeField]
        private List<ColorGroup> m_groups = new List<ColorGroup>();
        [SerializeField]
        private List<ColorHolder> m_colors = new List<ColorHolder>();

        private Dictionary<string, ColorGroup> m_groupsDictionary;
        private Dictionary<string, ColorHolder> m_colorHoldersDictionary;
        private Dictionary<string, Color> m_colorsDictionary;
        
        public string Title { get => m_title; set => m_title = value; }
        public bool AutoGenerate { get => m_autoGenerate; set => m_autoGenerate = value; }

        public IReadOnlyList<ColorGroup> Groups => m_groups;
        public IReadOnlyList<ColorHolder> ColorHolders => m_colors;

        public event Action Refreshed;

        private void OnEnable()
        {
            if(m_groups == null)
            {
                m_groups = new List<ColorGroup>();
            }
            if(m_colors == null)
            {
                m_colors = new List<ColorHolder>();
            }
            Refresh();
        }

        public void Refresh()
        {
            if (m_colorsDictionary == null)
            {
                m_colorsDictionary = new Dictionary<string, Color>();
            }
            else
            {
                m_colorsDictionary.Clear();
            }
            if (m_colorHoldersDictionary == null)
            {
                m_colorHoldersDictionary = new Dictionary<string, ColorHolder>();
            }
            else
            {
                m_colorHoldersDictionary.Clear();
            }
            foreach (var color in m_colors)
            {
                m_colorsDictionary[color.name] = color.color;
                m_colorHoldersDictionary[color.name] = color;
            }
            if(m_groupsDictionary == null)
            {
                m_groupsDictionary = new Dictionary<string, ColorGroup>();
            }
            else
            {
                m_groupsDictionary.Clear();
            }

            for (int i = 0; i < m_groups.Count; i++)
            {
                var group = m_groups[i];
                if(group == null || string.IsNullOrEmpty(group.Name))
                {
                    m_groups.RemoveAt(i--);
                }
                else
                {
                    group.Refresh();
                    m_groupsDictionary[group.Name] = group;
                }
            }

            Refreshed?.Invoke();
        }

        public void AddColor(string name, Color color)
        {
            if (!m_colorsDictionary.ContainsKey(name))
            {
                m_colors.Add(new ColorHolder(name, color));
                Refresh();

                s_fullSave?.Invoke(this);
            }
        }

        public void AddColor()
        {
            var colorHolder = m_colors.Count > 0 ? new ColorHolder(m_colors[m_colors.Count - 1].name, m_colors[m_colors.Count - 1].color) : new ColorHolder("Color ", Color.black);
            var match = Regex.Match(colorHolder.name, k_NumberPattern);
            int index = 1;
            string name = colorHolder.name;
            if (match.Success)
            {
                name = match.Groups[1].Value;
                index = int.Parse(match.Groups[2].Value) + 1;
            }

            while (m_colorHoldersDictionary.ContainsKey(name + index))
            {
                index++;
            }

            colorHolder.name = name + index;
            m_colors.Add(colorHolder);
            Refresh();

            s_fullSave?.Invoke(this);
        }

        public void AddGroupsFrom(ColorPalette colorPalette)
        {
            foreach(var group in colorPalette.m_groupsDictionary)
            {
                if (!m_groupsDictionary.ContainsKey(group.Key))
                {
                    m_groups.Add(group.Value);
                }
            }
            Refresh();
        }

        public void RemoveColor(ColorHolder holder)
        {
            if (m_colors.Remove(holder))
            {
                Refresh();
                s_fullSave?.Invoke(this);
            }
        }

        public void RemoveColor(string name)
        {
            bool refresh = false;
            for (int i = 0; i < m_colors.Count; i++)
            {
                if(m_colors[i].name == name)
                {
                    m_colors.RemoveAt(i--);
                    refresh = true;
                }
            }
            if (refresh)
            {
                Refresh();
                s_fullSave?.Invoke(this);
            }
        }

        public bool ContainsName(string name)
        {
            return m_colorsDictionary.ContainsKey(name);
        }

        public ColorHolder GetColorHolder(string name)
        {
            return m_colorHoldersDictionary.TryGetValue(name, out ColorHolder color) ? color : null;
        }

        public Color this[string name]
        {
            get
            {
                if (!m_colorsDictionary.TryGetValue(name, out Color color) && m_autoGenerate)
                {
                    color = Color.clear;
                    m_colorsDictionary[name] = color;
                    var colorHolder = new ColorHolder(name, color);
                    m_colors.Add(colorHolder);
                    m_colorHoldersDictionary[name] = colorHolder;
                }
                return color;
            }
            set
            {
                if (m_colorsDictionary.ContainsKey(name))
                {
                    m_colorsDictionary[name] = value;
                    foreach (var holder in m_colors)
                    {
                        if (holder.name == name)
                        {
                            holder.color = value;
                            break;
                        }
                    }
                }
                else if (m_autoGenerate)
                {
                    m_colors.Add(new ColorHolder(name, value));
                    Refresh();
                }
            }
        }

        public Color this[int index]
        {
            get => m_colors[index].color;
            set
            {
                if (index >= m_colors.Count && m_autoGenerate)
                {
                    while (index >= m_colors.Count)
                    {
                        m_colors.Add(new ColorHolder($"Elem {m_colors.Count + 1}", value));
                    }
                    Refresh();
                }
                else if (index >= 0)
                {
                    m_colors[index].color = value;
                }
            }
        }

        public void AddColorGroup(string name)
        {
            if (!m_groupsDictionary.ContainsKey(name))
            {
                m_groups.Add(new ColorGroup() { Name = name });
                Refresh();

                s_fullSave?.Invoke(this);
            }
        }

        public void RemoveColorGroup(ColorGroup group)
        {
            if (m_groups.Remove(group))
            {
                Refresh();
                s_fullSave?.Invoke(this);
            }
        }

        public void RemoveColorGroup(string name)
        {
            bool refresh = false;
            for (int i = 0; i < m_groups.Count; i++)
            {
                if (m_groups[i].Name == name)
                {
                    m_groups.RemoveAt(i--);
                    refresh = true;
                }
            }
            if (refresh)
            {
                Refresh();
                s_fullSave?.Invoke(this);
            }
        }

        public bool ContainsGroupName(string name)
        {
            return m_groupsDictionary.ContainsKey(name);
        }

        public ColorGroup GetGroup(string name)
        {
            if (m_groupsDictionary == null || string.IsNullOrEmpty(name))
            {
                Refresh();
            }
            if (!m_groupsDictionary.TryGetValue(name, out ColorGroup group) && m_autoGenerate)
            {
                group = new ColorGroup();
                group.Name = name;
                m_groupsDictionary[name] = group;
                m_groups.Add(group);

                Refresh();
                s_fullSave?.Invoke(this);
            }
            return group;
        }

        public ColorGroup GetReadonlyGroup(string name)
        {
            if(m_groupsDictionary == null || string.IsNullOrEmpty(name))
            {
                Refresh();
            }

            if (!m_groupsDictionary.TryGetValue(name, out ColorGroup group))
            {
                group = new ColorGroup(true);
                group.Name = name;
                m_groupsDictionary[name] = group;
                m_groups.Add(group);

                Refresh();
                s_fullSave?.Invoke(this);
            }
            return group;
        }

        public ColorGroup GetGroupAt(int index)
        {
            return m_groups[index];
        }
    }
}
