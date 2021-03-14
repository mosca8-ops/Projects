// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using TXT.WEAVR.Controls;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    internal class ItemDropdownItem : AdvancedDropdownItem, IHasDescription
    {
        private string m_menuPath;
        private Descriptor m_descriptor;
        private Action<Descriptor> m_callback;
        private string m_description;

        public string Description => m_description;

        internal ItemDropdownItem() : base("ROOT", -1)
        {
        }

        public ItemDropdownItem(Descriptor descriptor, Action<Descriptor> callback) : base(descriptor.Name, descriptor.GetHashCode().ToString(), descriptor.Depth)
        {
            m_menuPath = descriptor.FullPath;
            m_content = new GUIContent(descriptor.Name, descriptor.Icon);
            m_contentWhenSearching = new GUIContent(m_content);
            m_callback = callback;
            m_descriptor = descriptor;
            m_description = descriptor is IHasDescription descriptable && !string.IsNullOrEmpty(descriptable.Description) ? descriptable.Description : null;
        }

        public override bool OnAction()
        {
            if(m_callback != null && m_descriptor)
            {
                m_callback(m_descriptor);
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return m_menuPath;
        }
    }
}
