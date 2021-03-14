// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using TXT.WEAVR.Controls;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    internal class AddItemGUI : AdvancedDropdownGUI
    {
        private static class Styles
        {
            public static GUIStyle itemStyle = "DD LargeItemStyle";
        }

        private Vector2 m_IconSize = new Vector2(16, 16);

        public override Vector2 iconSize => m_IconSize;
        public override GUIStyle lineStyle => Styles.itemStyle;

        public AddItemGUI(AdvancedDropdownDataSource dataSource) : base(dataSource)
        {
        }
        
        public override string DrawSearchFieldControl(string searchString)
        {
            float padding = 8f;
            m_SearchRect = GUILayoutUtility.GetRect(0, 0);
            m_SearchRect.x += padding;
            m_SearchRect.y = 9;
            m_SearchRect.width -= padding * 2;
            m_SearchRect.height = 30;
            var newSearch = ToolbarSearchField(m_SearchRect, searchString, false);
            return newSearch;
        }
    }
}
