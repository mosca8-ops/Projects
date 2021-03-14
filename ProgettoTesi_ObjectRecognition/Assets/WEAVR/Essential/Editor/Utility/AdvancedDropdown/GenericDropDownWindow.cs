using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Controls;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Controls
{

    [InitializeOnLoad]
    public class GenericDropDownWindow : AdvancedDropdownWindow
    {
        internal static GenericDropDownWindow s_GetComponentWindow = null;

        internal GameObject[] m_GameObjects;
        internal string searchString => m_searchKeyword;

        private DateTime m_ComponentOpenTime;
        private const string kComponentSearch = "GenericItemSearch";
        private const int kMaxWindowHeight = 395 - 80;

        protected override bool setInitialSelectionPosition { get; } = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            showHeader = true;
            m_searchKeyword = EditorPrefs.GetString(kComponentSearch, "");
        }

        protected override void OnDisable()
        {
            s_GetComponentWindow = null;
            EditorPrefs.SetString(kComponentSearch, m_searchKeyword);
        }

        protected override Vector2 CalculateWindowSize(Rect buttonRect)
        {
            return new Vector2(buttonRect.width, kMaxWindowHeight);
        }

        public static bool Show<T>(Rect rect, IEnumerable<T> items, 
                                     Action<T> selectedCallback,
                                     Func<T, string> nameGetter,
                                     Func<T, string> pathGetter,
                                     Func<T, Texture2D> iconGetter = null,
                                     string intentName = null)
        {
            CloseAllOpenWindows<GenericDropDownWindow>();

            Event.current.Use();
            s_GetComponentWindow = CreateAndInit<GenericDropDownWindow>
                (rect, new GenericItemDataSource<T>(items, selectedCallback, nameGetter, pathGetter, iconGetter, intentName));
            return true;
        }

        protected override PopupLocation[] GetLocationPriority()
        {
            return new[]
            {
                PopupLocation.Below,
                PopupLocation.Above,
            };
        }
    }
}