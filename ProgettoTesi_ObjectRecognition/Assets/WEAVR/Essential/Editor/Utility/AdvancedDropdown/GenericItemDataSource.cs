using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Controls;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Controls
{
    internal class GenericItemDataSource<T> : AdvancedDropdownDataSource
    {
        private IEnumerable<T> m_items;
        private Action<T> m_selectedCallback;
        private Func<T, string> m_getName;
        private Func<T, string> m_getPath;
        private Func<T, Texture2D> m_getIcon;
        private string m_intentName;

        public GenericItemDataSource(IEnumerable<T> items, Action<T> selectedCallback, 
                                     Func<T, string> nameGetter, 
                                     Func<T, string> pathGetter, 
                                     Func<T, Texture2D> iconGetter = null, 
                                     string intentName = null)
        {
            m_intentName = intentName;
            m_items = items;
            m_selectedCallback = selectedCallback;

            m_getName = nameGetter ?? (e => e?.GetType().Name ?? "None");
            m_getPath = pathGetter ?? m_getName;
            m_getIcon = iconGetter ?? (e => null);
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return RebuildTreeWithItems();
        }

        private AdvancedDropdownItem RebuildTreeWithItems()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            AdvancedDropdownItem root = m_intentName != null ? new GenericDropdownItem(m_intentName) : new GenericDropdownItem();

            var noneElement = new GenericDropdownItem("None", L10n.Tr("None"), "None", null, () => m_selectedCallback(default));
            noneElement.SetParent(root);
            root.AddChild(noneElement);

            foreach(var component in m_items)
            {
                var element = new GenericDropdownItem(m_getName(component), 
                                                      L10n.Tr(m_getName(component)), 
                                                      m_getPath(component), 
                                                      m_getIcon(component), 
                                                      () => m_selectedCallback(component));
                element.SetParent(root);
                root.AddChild(element);
                m_SearchableElements.Add(element);
            }
            //root = root.children.Single();
            root.SetParent(null);
            return root;
        }

        private int CompareItems(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}
