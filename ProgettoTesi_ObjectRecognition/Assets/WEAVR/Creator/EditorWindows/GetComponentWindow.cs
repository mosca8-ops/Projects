using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Controls;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    [InitializeOnLoad]
    class GetComponentWindow : AdvancedDropdownWindow
    {
        internal static GetComponentWindow s_GetComponentWindow = null;

        internal GameObject[] m_GameObjects;
        internal string searchString => m_searchKeyword;

        private DateTime m_ComponentOpenTime;
        private const string kComponentSearch = "GetComponentSearchString";
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

        internal static bool Show(Rect rect, IEnumerable<Component> components, Action<Component> selectedCallback, string intentName = null)
        {
            CloseAllOpenWindows<GetComponentWindow>();

            Event.current.Use();
            s_GetComponentWindow = CreateAndInit<GetComponentWindow>(rect, new GetComponentDataSource(components, selectedCallback, intentName));
            return true;
        }

        internal static bool Show(Rect rect)
        {
            CloseAllOpenWindows<GetComponentWindow>();

            Event.current.Use();
            s_GetComponentWindow = CreateAndInit<GetComponentWindow>(rect, new GetComponentDataSource());
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