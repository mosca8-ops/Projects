using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

using System.Reflection;
using System.Linq;
using UnityEditor;

namespace TXT.WEAVR.Procedure
{
    class StickyNote : GraphElement
    {
        public enum Theme
        {
            Classic,
            Black,
            Orange,
            Green,
            Blue,
            Red,
            Teal,
        }

        Theme m_Theme = Theme.Classic;
        public Theme theme
        {
            get
            {
                return m_Theme;
            }
            set
            {
                if (m_Theme != value)
                {
                    m_Theme = value;
                    UpdateThemeClasses();
                }
            }
        }

        public enum TextSize
        {
            Small,
            Medium,
            Large,
            Huge
        }

        TextSize m_TextSize = TextSize.Medium;

        public TextSize textSize
        {
            get { return m_TextSize; }
            set
            {
                if (m_TextSize != value)
                {
                    m_TextSize = value;
                    UpdateSizeClasses();
                }
            }
        }

        public virtual void OnStartResize()
        {
        }

        public virtual void OnResized()
        {
        }

        Vector2 AllExtraSpace(VisualElement element)
        {
            return new Vector2(
                element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight + element.resolvedStyle.paddingLeft + element.resolvedStyle.paddingRight + element.resolvedStyle.borderRightWidth + element.resolvedStyle.borderLeftWidth,
                element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom + element.resolvedStyle.paddingTop + element.resolvedStyle.paddingBottom + element.resolvedStyle.borderBottomWidth + element.resolvedStyle.borderTopWidth
            );
        }

        void OnFitToText(DropdownMenuAction a)
        {
            FitText(false);
        }

        public void FitText(bool onlyIfSmaller)
        {
            Vector2 preferredTitleSize = Vector2.zero;
            if (!string.IsNullOrEmpty(m_Title.text))
                preferredTitleSize = m_Title.MeasureTextSize(m_Title.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined); // This is the size of the string with the current title font and such

            preferredTitleSize += AllExtraSpace(m_Title);
            preferredTitleSize.x += m_Title.ChangeCoordinatesTo(this, Vector2.zero).x + resolvedStyle.width - m_Title.ChangeCoordinatesTo(this, new Vector2(m_Title.layout.width, 0)).x;

            Vector2 preferredContentsSizeOneLine = m_Contents.MeasureTextSize(m_Contents.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);

            Vector2 contentExtraSpace = AllExtraSpace(m_Contents);
            preferredContentsSizeOneLine += contentExtraSpace;

            Vector2 extraSpace = new Vector2(resolvedStyle.width, resolvedStyle.height) - m_Contents.ChangeCoordinatesTo(this, new Vector2(m_Contents.layout.width, m_Contents.layout.height));
            extraSpace += m_Title.ChangeCoordinatesTo(this, Vector2.zero);
            preferredContentsSizeOneLine += extraSpace;

            float width = 0;
            float height = 0;
            // The content in one line is smaller than the current width.
            // Set the width to fit both title and content.
            // Set the height to have only one line in the content
            if (preferredContentsSizeOneLine.x < Mathf.Max(preferredTitleSize.x, resolvedStyle.width))
            {
                width = Mathf.Max(preferredContentsSizeOneLine.x, preferredTitleSize.x);
                height = preferredContentsSizeOneLine.y + preferredTitleSize.y;
            }
            else // The width is not enough for the content: keep the width or use the title width if bigger.
            {
                width = Mathf.Max(preferredTitleSize.x + extraSpace.x, resolvedStyle.width);
                float contextWidth = width - extraSpace.x - contentExtraSpace.x;
                Vector2 preferredContentsSize = m_Contents.MeasureTextSize(m_Contents.text, contextWidth, MeasureMode.Exactly, 0, MeasureMode.Undefined);

                preferredContentsSize += contentExtraSpace;

                height = preferredTitleSize.y + preferredContentsSize.y + extraSpace.y;
            }
            if (!onlyIfSmaller || resolvedStyle.width < width)
                style.width = width;
            if (!onlyIfSmaller || resolvedStyle.height < height)
                style.height = height;
            OnResized();
        }

        void UpdateThemeClasses()
        {
            foreach (Theme value in System.Enum.GetValues(typeof(Theme)))
            {
                if (m_Theme != value)
                {
                    RemoveFromClassList("theme-" + value.ToString().ToLower());
                }
                else
                {
                    AddToClassList("theme-" + value.ToString().ToLower());
                }
            }
        }

        void UpdateSizeClasses()
        {
            foreach (TextSize value in System.Enum.GetValues(typeof(TextSize)))
            {
                if (m_TextSize != value)
                {
                    RemoveFromClassList("size-" + value.ToString().ToLower());
                }
                else
                {
                    AddToClassList("size-" + value.ToString().ToLower());
                }
            }
        }

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public StickyNote(Vector2 position) : this("uxml/StickyNote", position)
        {
            this.AddStyleSheetPath("Selectable");
            this.AddStyleSheetPath("StickyNote");
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public StickyNote(string uiFile, Vector2 position)
        {
            var tpl = Resources.Load<VisualTreeAsset>(uiFile);

            tpl.CloneTree(this);

            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Selectable;

            m_Title = this.Q<Label>(name: "title");
            if (m_Title != null)
            {
                m_Title.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            }

            m_TitleField = this.Q<TextField>(name: "title-field");
            if (m_TitleField != null)
            {
                m_TitleField.visible = false;
                m_TitleField.RegisterCallback<BlurEvent>(OnTitleBlur);
                m_TitleField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            }


            m_Contents = this.Q<Label>(name: "contents");
            if (m_Contents != null)
            {
                m_ContentsField = m_Contents.Q<TextField>(name: "contents-field");
                if (m_ContentsField != null)
                {
                    m_ContentsField.visible = false;
                    m_ContentsField.multiline = true;
                    m_ContentsField.RegisterCallback<BlurEvent>(OnContentsBlur);
                }
                m_Contents.RegisterCallback<MouseDownEvent>(OnContentsMouseDown);
            }

            SetPosition(new Rect(position, defaultSize));

            AddToClassList("sticky-note");
            AddToClassList("selectable");
            UpdateThemeClasses();
            UpdateSizeClasses();

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is StickyNote)
            {
                /*foreach (Theme value in System.Enum.GetValues(typeof(Theme)))
                {
                    evt.menu.AppendAction("Theme/" + value.ToString(), OnChangeTheme, e => DropdownMenuAction.Status.Normal, value);
                }*/
                if (theme == Theme.Black)
                    evt.menu.AppendAction("Light Theme", OnChangeTheme, e => DropdownMenuAction.Status.Normal, Theme.Classic);
                else
                    evt.menu.AppendAction("Dark Theme", OnChangeTheme, e => DropdownMenuAction.Status.Normal, Theme.Black);

                foreach (TextSize value in System.Enum.GetValues(typeof(TextSize)))
                {
                    evt.menu.AppendAction(value.ToString() + " Text Size", OnChangeSize, e => DropdownMenuAction.Status.Normal, value);
                }
                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Fit To Text", OnFitToText, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnTitleChange(EventBase e)
        {
            Title = m_TitleField.value;
        }

        const string fitTextClass = "fit-text";

        public override void SetPosition(Rect rect)
        {
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }

        public override Rect GetPosition()
        {
            return new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        public string Contents
        {
            get { return m_Contents.text; }
            set
            {
                if (m_Contents != null)
                {
                    m_Contents.text = value;
                }
            }
        }
        public string Title
        {
            get { return m_Title.text; }
            set
            {
                if (m_Title != null)
                {
                    m_Title.text = value;

                    if (!string.IsNullOrEmpty(m_Title.text))
                    {
                        m_Title.RemoveFromClassList("empty");
                    }
                    else
                    {
                        m_Title.AddToClassList("empty");
                    }
                    //UpdateTitleHeight();
                }
            }
        }

        void OnChangeTheme(DropdownMenuAction action)
        {
            theme = (Theme)action.userData;
            NotifyChange(StickyNodeChangeEvent.Change.theme);
        }

        void OnChangeSize(DropdownMenuAction action)
        {
            textSize = (TextSize)action.userData;
            NotifyChange(StickyNodeChangeEvent.Change.textSize);
            panel.InternalValidateLayout();

            FitText(true);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            //UpdateTitleHeight();
        }

        void OnTitleBlur(BlurEvent e)
        {
            //bool changed = m_Title.text != m_TitleField.value;
            Title = m_TitleField.value;
            m_TitleField.visible = false;

            m_Title.UnregisterCallback<GeometryChangedEvent>(OnTitleRelayout);

            //Notify change
            //if( changed)
            {
                NotifyChange(StickyNodeChangeEvent.Change.title);
            }
        }

        void OnContentsBlur(BlurEvent e)
        {
            bool changed = m_Contents.text != m_ContentsField.value;
            m_Contents.text = m_ContentsField.value;
            m_ContentsField.visible = false;

            //Notify change
            if (changed)
            {
                NotifyChange(StickyNodeChangeEvent.Change.contents);
            }
        }

        void OnTitleRelayout(GeometryChangedEvent e)
        {
            UpdateTitleFieldRect();
        }

        void UpdateTitleFieldRect()
        {
            Rect rect = m_Title.layout;
            //if( m_Title != m_TitleField.parent)
            m_Title.parent.ChangeCoordinatesTo(m_TitleField.parent, rect);

            m_TitleField.style.left = rect.xMin /* + m_Title.style.marginLeft*/;
            m_TitleField.style.right = rect.yMin + m_Title.resolvedStyle.marginTop;
            m_TitleField.style.width = rect.width - m_Title.resolvedStyle.marginLeft - m_Title.resolvedStyle.marginRight;
            m_TitleField.style.height = rect.height - m_Title.resolvedStyle.marginTop - m_Title.resolvedStyle.marginBottom;
        }

        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                m_TitleField.RemoveFromClassList("empty");
                m_TitleField.value = m_Title.text;
                m_TitleField.visible = true;
                UpdateTitleFieldRect();
                m_Title.RegisterCallback<GeometryChangedEvent>(OnTitleRelayout);

                m_TitleField.Focus();
                m_TitleField.SelectAll();

                e.StopPropagation();
                e.PreventDefault();
            }
        }

        void NotifyChange(StickyNodeChangeEvent.Change change)
        {
            OnChange?.Invoke(change);
        }

        public System.Action<StickyNodeChangeEvent.Change> OnChange;

        void OnContentsMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                m_ContentsField.value = m_Contents.text;
                m_ContentsField.visible = true;
                m_ContentsField.Focus();
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        Label m_Title;
        protected TextField m_TitleField;
        Label m_Contents;
        protected TextField m_ContentsField;
    }

    class StickyNodeChangeEvent : EventBase<StickyNodeChangeEvent>
    {
        public static StickyNodeChangeEvent GetPooled(StickyNote target, Change change)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.change = change;
            return evt;
        }

        public enum Change
        {
            title,
            contents,
            theme,
            textSize
        }

        public Change change { get; protected set; }
    }
}
