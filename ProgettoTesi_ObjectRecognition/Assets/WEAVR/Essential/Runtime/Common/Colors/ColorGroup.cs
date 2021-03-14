using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [Serializable]
    public class ColorGroup
    {
        private const string k_NumberPattern = @"([\w\s\d]+)(\d+)";

        [SerializeField]
        private string m_name;
        [SerializeField]
        private bool m_autoGenerate;
        [SerializeField]
        private List<ColorHolder> m_colors = new List<ColorHolder>();
        [SerializeField]
        private bool m_readonly;
        private Dictionary<string, Color> m_colorsDictionary;
        private Dictionary<string, ColorHolder> m_colorHoldersDictionary;

        public string Name { get => m_name; set => m_name = value; }
        public bool Readonly { get => m_readonly; }
        public bool AutoGenerate { get => m_autoGenerate; set => m_autoGenerate = value; }

        public IReadOnlyList<ColorHolder> ColorHolders => m_colors;

        public int Count => m_colors.Count;

        public ColorGroup() : this(false) { }

        public ColorGroup(bool isReadonly)
        {
            m_readonly = isReadonly;
            if (m_readonly) { m_autoGenerate = true; }
            m_colorsDictionary = new Dictionary<string, Color>();
            m_colorHoldersDictionary = new Dictionary<string, ColorHolder>();
        }

        public void Refresh()
        {
            if(m_colorsDictionary == null)
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
        }

        public void AddColor(string name, Color color)
        {
            if (!m_colorsDictionary.ContainsKey(name))
            {
                m_colors.Add(new ColorHolder(name, color));
                Refresh();
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
        }

        public bool ContainsName(string name)
        {
            return m_colorsDictionary.ContainsKey(name);
        }

        public Color this[string name]
        {
            get
            {
                if(!m_colorsDictionary.TryGetValue(name, out Color color) && m_autoGenerate)
                {
                    color = Color.clear;
                    m_colorsDictionary[name] = color;
                    var holder = new ColorHolder(name, color);
                    m_colors.Add(holder);
                    m_colorHoldersDictionary[name] = holder;
                }
                return color;
            }
            set
            {
                if (m_colorsDictionary.ContainsKey(name))
                {
                    m_colorsDictionary[name] = value;
                    foreach(var holder in m_colors)
                    {
                        if(holder.name == name)
                        {
                            holder.color = value;
                            break;
                        }
                    }
                }
                else if(m_autoGenerate)
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
                if(index >= m_colors.Count && m_autoGenerate)
                {
                    while(index >= m_colors.Count)
                    {
                        m_colors.Add(new ColorHolder($"Elem {m_colors.Count + 1}", value));
                    }
                    Refresh();
                }
                else if(index >= 0)
                {
                    m_colors[index].color = value;
                }
            }
        }

        public ColorHolder GetColorHolder(string name)
        {
            return m_colorHoldersDictionary.TryGetValue(name, out ColorHolder color) ?  color : null;
        }

        public void Remove(ColorHolder holderToEliminate)
        {
            if (m_colors.Remove(holderToEliminate))
            {
                Refresh();
            }
        }
    }
}
