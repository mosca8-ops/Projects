using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Controls
{
    public class AdvancedDropdownItem : IComparable
    {
        internal static AdvancedDropdownItem s_SeparatorItem = new SeparatorDropdownItem();

        protected GUIContent m_content;
        public virtual GUIContent content => m_content;

        protected GUIContent m_contentWhenSearching;
        public virtual GUIContent contentWhenSearching => m_contentWhenSearching;

        private string m_Name;
        public string name => m_Name;

        private string m_LocalizedName;
        public string localizedName => m_LocalizedName;

        public string searchableName => m_Name;
        public string searchableNameLocalized => m_LocalizedName;

        private string m_Id;
        public string id => m_Id;

        private AdvancedDropdownItem m_Parent;
        public virtual AdvancedDropdownItem parent => m_Parent;

        private List<AdvancedDropdownItem> m_Children = new List<AdvancedDropdownItem>();
        public virtual List<AdvancedDropdownItem> children => m_Children;

        public bool hasChildren => children.Any();
        public virtual bool drawArrow => hasChildren;

        public virtual bool enabled { get; set; } = true;

        internal int m_Index = -1;
        internal Vector2 m_Scroll;
        private int m_SelectedItem = -1;
        internal bool selectionExists;

        public int selectedItem
        {
            get { return m_SelectedItem; }
            set
            {
                selectionExists = true;
                if (value < 0)
                {
                    m_SelectedItem = 0;
                }
                else if (value >= children.Count)
                {
                    m_SelectedItem = children.Count - 1;
                }
                else
                {
                    m_SelectedItem = value;
                }
            }
        }

        public AdvancedDropdownItem(string name, int index) : this(name, name, index)
        {
        }

        public AdvancedDropdownItem(string name, string localizedName, int index) : this(name, localizedName, name, index)
        {
        }

        public AdvancedDropdownItem(string name, string localizedName, string id, int index)
        {
            m_content = new GUIContent(localizedName);
            m_contentWhenSearching = new GUIContent(localizedName);
            m_Name = name;
            m_LocalizedName = localizedName;
            m_Id = id;
            m_Index = index;
        }

        public AdvancedDropdownItem(GUIContent content, int index) : this(content, content, index)
        {
        }

        public AdvancedDropdownItem(GUIContent content, GUIContent contentWhenSearching, int index)
        {
            m_content = content;
            m_contentWhenSearching = contentWhenSearching;
            m_Name = content.text;
            m_Id = contentWhenSearching.text;
            m_Index = index;
        }

        public void AddChild(AdvancedDropdownItem item)
        {
            children.Add(item);
        }

        public void SetParent(AdvancedDropdownItem item)
        {
            m_Parent = item;
        }

        public void AddSeparator()
        {
            children.Add(s_SeparatorItem);
        }

        public virtual bool IsSeparator()
        {
            return false;
        }

        public virtual bool OnAction()
        {
            return enabled;
        }

        public AdvancedDropdownItem GetSelectedChild()
        {
            if (children.Count == 0 || m_SelectedItem < 0)
                return null;
            return children[m_SelectedItem];
        }

        public int GetSelectedChildIndex()
        {
            var i = children[m_SelectedItem].m_Index;
            if (i >= 0)
            {
                return i;
            }
            return m_SelectedItem;
        }

        public virtual int CompareTo(object o)
        {
            return id.CompareTo((o as AdvancedDropdownItem).id);
        }

        public void MoveDownSelection()
        {
            var selectedIndex = selectedItem;
            do
            {
                ++selectedIndex;
            }
            while (selectedIndex < children.Count && children[selectedIndex].IsSeparator());

            if (selectedIndex < children.Count)
                selectedItem = selectedIndex;
        }

        public void MoveUpSelection()
        {
            if (selectedItem < 0)
            {
                selectedItem = children.Count;
                return;
            }
            var selectedIndex = selectedItem;
            do
            {
                --selectedIndex;
            }
            while (selectedIndex >= 0 && children[selectedIndex].IsSeparator());
            if (selectedIndex >= 0)
                selectedItem = selectedIndex;
        }

        class SeparatorDropdownItem : AdvancedDropdownItem
        {
            public SeparatorDropdownItem() : base("SEPARATOR", -1)
            {
            }

            public override bool IsSeparator()
            {
                return true;
            }

            public override bool OnAction()
            {
                return false;
            }
        }
    }
}
