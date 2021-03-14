using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Controls;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    internal class GetComponentDataSource : AdvancedDropdownDataSource
    {

        private IEnumerable<Component> m_components;
        private Action<Component> m_selectedCallback;
        private string m_intentName;

        public GetComponentDataSource()
        {

        }

        public GetComponentDataSource(IEnumerable<Component> components, Action<Component> selectedCallback, string intentName = null)
        {
            m_intentName = intentName;
            m_components = components;
            m_selectedCallback = selectedCallback;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return m_components != null ? RebuildTreeWithComponents() : RebuildTreeWithCommands();
        }

        private AdvancedDropdownItem RebuildTreeWithComponents()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            AdvancedDropdownItem root = m_intentName != null ? new ComponentDropdownItem(m_intentName) : new ComponentDropdownItem();

            var noneElement = new ComponentDropdownItem("None", L10n.Tr("None"), "None", null, m_selectedCallback);
            noneElement.SetParent(root);
            root.AddChild(noneElement);

            foreach(var component in m_components)
            {
                var element = new ComponentDropdownItem(component, m_selectedCallback);
                element.SetParent(root);
                root.AddChild(element);
                m_SearchableElements.Add(element);
            }
            //root = root.children.Single();
            root.SetParent(null);
            return root;
        }

        protected AdvancedDropdownItem RebuildTreeWithCommands()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            AdvancedDropdownItem root = new ComponentDropdownItem();
            var menuDictionary = GetMenuDictionary();
            menuDictionary.Sort(CompareItems);
            for (var i = 0; i < menuDictionary.Count; i++)
            {
                var menu = menuDictionary[i];
                if (menu.Value == "ADD")
                {
                    continue;
                }

                var menuPath = menu.Key;
                var paths = menuPath.Split('/');

                var parent = root;
                for (var j = 0; j < paths.Length; j++)
                {
                    var path = paths[j];
                    if (j == paths.Length - 1)
                    {
                        var element = new ComponentDropdownItem(path, L10n.Tr(path), menuPath, menu.Value);
                        element.SetParent(parent);
                        parent.AddChild(element);
                        m_SearchableElements.Add(element);
                        continue;
                    }
                    var group = parent.children.SingleOrDefault(c => c.name == path);
                    if (group == null)
                    {
                        group = new ComponentDropdownItem(path, L10n.Tr(path), -1);
                        group.SetParent(parent);
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            root = root.children.Single();
            root.SetParent(null);
            return root;
        }

        private static List<KeyValuePair<string, string>> GetMenuDictionary()
        {
            var menus = Unsupported.GetSubmenus("Component");
            var commands = Unsupported.GetSubmenusCommands("Component");

            var menuDictionary = new Dictionary<string, string>(menus.Length);
            for (var i = 0; i < menus.Length; i++)
            {
                menuDictionary.Add(menus[i], commands[i]);
            }
            return menuDictionary.ToList();
        }

        private int CompareItems(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}
