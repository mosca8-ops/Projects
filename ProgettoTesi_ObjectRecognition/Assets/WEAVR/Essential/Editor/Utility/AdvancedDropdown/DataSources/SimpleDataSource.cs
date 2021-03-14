using UnityEngine;

namespace TXT.WEAVR.Controls
{
    internal class SimpleDataSource : AdvancedDropdownDataSource
    {
        private GUIContent[] m_DisplayedOptions;
        public GUIContent[] displayedOptions {set { m_DisplayedOptions = value; }}

        private static int m_SelectedIndex;
        public int selectedIndex { set { m_SelectedIndex = value; } }

        protected override AdvancedDropdownItem FetchData()
        {
            selectedIds.Clear();
            var rootGroup = new AdvancedDropdownItem("", -1);

            for (int i = 0; i < m_DisplayedOptions.Length; i++)
            {
                var option = m_DisplayedOptions[i];

                var element = new AdvancedDropdownItem(option, i);
                element.SetParent(rootGroup);
                rootGroup.AddChild(element);

                if (i == m_SelectedIndex)
                {
                    selectedIds.Add(element.id);
                    rootGroup.selectedItem = i;
                }
            }
            return rootGroup;
        }
    }
}
