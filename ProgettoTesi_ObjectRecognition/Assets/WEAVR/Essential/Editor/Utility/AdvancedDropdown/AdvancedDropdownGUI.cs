using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Event = UnityEngine.Event;

namespace TXT.WEAVR.Controls
{
    public class AdvancedDropdownGUI
    {
        private static class Styles
        {
            private const float k_searchFieldHeight = 16;

            public static GUIStyle itemStyle = "DD ItemStyle";
            public static GUIStyle header = "DD HeaderStyle";
            public static GUIStyle headerArrow = "DefaultCenteredLargeText";
            public static GUIStyle checkMark = "DD ItemCheckmark";
            public static GUIStyle lineSeparator = "DefaultLineSeparator";
            public static GUIStyle rightArrow = "ArrowNavigationRight";
            public static GUIStyle leftArrow = "ArrowNavigationLeft";

            public static GUIStyle toolbarSearchField = new GUIStyle("ToolbarSeachTextField");
            public static GUIStyle toolbarSearchFieldPopup = new GUIStyle("ToolbarSeachTextFieldPopup");
            public static GUIStyle toolbarSearchFieldCancelButton = new GUIStyle("ToolbarSeachCancelButton");
            public static GUIStyle toolbarSearchFieldCancelButtonEmpty = new GUIStyle("ToolbarSeachCancelButtonEmpty");

            public static GUIContent checkMarkContent = new GUIContent("✔");

            static Styles()
            {
                toolbarSearchField.padding.top = 2;
                toolbarSearchField.fixedHeight = k_searchFieldHeight;
                toolbarSearchFieldCancelButton.fixedHeight = k_searchFieldHeight;
                toolbarSearchFieldCancelButtonEmpty.fixedHeight = k_searchFieldHeight;
            }
        }

        //This should ideally match line height
        private Vector2 s_IconSize = new Vector2(13, 13);
        private AdvancedDropdownDataSource m_DataSource;

        protected Rect m_SearchRect;
        protected Rect m_HeaderRect;

        public virtual float searchHeight => m_SearchRect.height;
        public virtual float headerHeight => m_HeaderRect.height;
        public virtual GUIStyle lineStyle => Styles.itemStyle;
        public virtual Vector2 iconSize => s_IconSize;

        public AdvancedDropdownGUI(AdvancedDropdownDataSource dataSource)
        {
            m_DataSource = dataSource;
        }

        public virtual void DrawItem(AdvancedDropdownItem item, bool selected, bool hasSearch)
        {
            if (item.IsSeparator())
            {
                DrawLineSeparator();
                return;
            }

            var content = !hasSearch ? item.content : item.contentWhenSearching;
            var imgTemp = content.image;
            //we need to pretend we have an icon to calculate proper width in case
            if (content.image == null)
                content.image = Texture2D.whiteTexture;
            var rect = GUILayoutUtility.GetRect(content, lineStyle, GUILayout.ExpandWidth(true));
            content.image = imgTemp;

            if (item.IsSeparator() || Event.current.type != EventType.Repaint)
                return;

            var imageTemp = content.image;
            if (m_DataSource.selectedIds.Any() && m_DataSource.selectedIds.Contains(item.id))
            {
                var checkMarkRect = new Rect(rect);
                checkMarkRect.width = iconSize.x + 1;
                Styles.checkMark.Draw(checkMarkRect, Styles.checkMarkContent, false, false, selected, selected);
                rect.x += iconSize.x + 1;
                rect.width -= iconSize.x + 1;

                //don't draw the icon if the check mark is present
                content.image = null;
            }
            else if (content.image == null)
            {
                lineStyle.Draw(rect, GUIContent.none, false, false, selected, selected);
                rect.x += iconSize.x + 1;
                rect.width -= iconSize.x + 1;
            }

            bool wasEnabled = GUI.enabled;
            GUI.enabled &= item.enabled;
            lineStyle.Draw(rect, content, false, false, selected, selected);
            content.image = imageTemp;
            if (item.drawArrow)
            {
                var yOffset = (lineStyle.fixedHeight - Styles.rightArrow.fixedHeight) / 2;
                Rect arrowRect = new Rect(
                    rect.xMax - Styles.rightArrow.fixedWidth - Styles.rightArrow.margin.right,
                    rect.y + yOffset,
                    Styles.rightArrow.fixedWidth,
                    Styles.rightArrow.fixedHeight);
                Styles.rightArrow.Draw(arrowRect, false, false, false, false);
            }
            GUI.enabled = wasEnabled;
        }

        protected virtual void DrawLineSeparator()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.lineSeparator, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;
            Color orgColor = GUI.color;
            Color tintColor = (EditorGUIUtility.isProSkin) ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
            GUI.color = GUI.color * tintColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;
        }

        public void DrawHeader(AdvancedDropdownItem group, Action backButtonPressed)
        {
            var content = group.content;
            m_HeaderRect = GUILayoutUtility.GetRect(content, Styles.header, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                Styles.header.Draw(m_HeaderRect, content, false, false, false, false);

            // Back button
            if (group.parent != null)
            {
                var yOffset = (m_HeaderRect.height - Styles.leftArrow.fixedWidth) / 2;
                var arrowRect = new Rect(
                    m_HeaderRect.x + Styles.leftArrow.margin.left,
                    m_HeaderRect.y + yOffset,
                    Styles.leftArrow.fixedWidth,
                    Styles.leftArrow.fixedHeight);
                if (Event.current.type == EventType.Repaint)
                    Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && m_HeaderRect.Contains(Event.current.mousePosition))
                {
                    backButtonPressed();
                    Event.current.Use();
                }
            }
        }

        public void DrawSearchField(bool isSearchFieldDisabled, string searchString, Action<string> searchChanged)
        {
            if (!isSearchFieldDisabled)
            {
                EditorGUI.FocusTextInControl("ComponentSearch");
            }

            using (new EditorGUI.DisabledScope(isSearchFieldDisabled))
            {
                GUI.SetNextControlName("ComponentSearch");

                var newSearch = DrawSearchFieldControl(searchString);

                if (newSearch != searchString)
                {
                    searchChanged(newSearch);
                }
            }
        }

        public virtual string DrawSearchFieldControl(string searchString)
        {
            var paddingX = 8f;
            var paddingY = 2f;
            var rect = GUILayoutUtility.GetRect(0, 0, Styles.toolbarSearchField);
            rect.x += paddingX;
            rect.y += paddingY + 1; // Add one for the border
            rect.height += Styles.toolbarSearchField.fixedHeight + paddingY * 3;
            rect.width -= paddingX * 2;
            m_SearchRect = rect;
            searchString = ToolbarSearchField(m_SearchRect, searchString, false);
            return searchString;
        }

        public static string ToolbarSearchField(Rect position, string text, bool showWithPopupArrow)
        {
            Rect textRect = position;
            const float k_CancelButtonWidth = 14f;
            textRect.width -= k_CancelButtonWidth;

            text = GUI.TextField(textRect, text, showWithPopupArrow ? Styles.toolbarSearchFieldPopup : Styles.toolbarSearchField);

            Rect buttonRect = position;
            buttonRect.x += position.width - k_CancelButtonWidth;
            buttonRect.width = k_CancelButtonWidth;
            if (GUI.Button(buttonRect, GUIContent.none, text != "" ? Styles.toolbarSearchFieldCancelButton : Styles.toolbarSearchFieldCancelButtonEmpty) && text != "")
            {
                text = "";
                GUIUtility.keyboardControl = 0;
            }

            return text;
        }

        public Rect GetAnimRect(Rect position, float anim)
        {
            // Calculate rect for animated area
            var rect = new Rect(position);
            rect.x = position.x + position.width * anim;
            rect.y += searchHeight;
            rect.height -= searchHeight;
            return rect;
        }

        public Vector2 CalculateContentSize(AdvancedDropdownDataSource dataSource)
        {
            float maxWidth = 0;
            float maxHeight = 0;
            bool includeArrow = false;
            float arrowWidth = Styles.rightArrow.fixedWidth;

            foreach (var child in dataSource.mainTree.children)
            {
                var content = child.content;
                var a = lineStyle.CalcSize(content);
                a.x += iconSize.x + 1;

                if (maxWidth < a.x)
                {
                    maxWidth = a.x + 1;
                    includeArrow |= child.hasChildren;
                }
                if (child.IsSeparator())
                {
                    maxHeight += Styles.lineSeparator.CalcHeight(content, maxWidth) + Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    maxHeight += lineStyle.CalcHeight(content, maxWidth);
                }
            }
            if (includeArrow)
            {
                maxWidth += arrowWidth;
            }
            return new Vector2(maxWidth, maxHeight);
        }

        public float GetSelectionHeight(AdvancedDropdownDataSource dataSource, Rect buttonRect)
        {
            if (dataSource.mainTree.selectedItem == -1)
                return 0;
            float heigth = 0;
            for (int i = 0; i < dataSource.mainTree.children.Count; i++)
            {
                var child = dataSource.mainTree.children[i];
                var content = child.content;

                if (dataSource.mainTree.selectedItem == i)
                {
                    var diff = (lineStyle.CalcHeight(content, 0) - buttonRect.height) / 2f;
                    return heigth + diff;
                }
                if (child.IsSeparator())
                {
                    heigth += Styles.lineSeparator.CalcHeight(content, 0) + Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    heigth += lineStyle.CalcHeight(content, 0);
                }
            }
            return heigth;
        }
    }
}
