using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Controls;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    [InitializeOnLoad]
    class AddItemWindow : AdvancedDropdownWindow
    {
        internal static AddItemWindow s_AddItemWindow = null;
        
        internal string searchString => m_searchKeyword;
        
        private string m_searchKey = "ActionSearchString";
        private const int kMaxWindowHeight = 395 - 80;
        private const int kMaxWindowWidth = 280;
        private const int kMinWindowWidth = 220;

        protected override bool setInitialSelectionPosition { get; } = false;

        public const string OpenAddActionDropdown = "OpenAddActionDropdown";

        protected override bool isSearchFieldDisabled => false;

        public AdvancedDropdownDataSource DataSource
        {
            get => m_dataSource;
            set
            {
                if(m_dataSource != value)
                {
                    if (!string.IsNullOrEmpty(m_searchKey))
                    {
                        m_searchKeyword = EditorPrefs.GetString(m_searchKey, "");
                    }
                    m_dataSource = value;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            //dataSource = new AddComponentDataSource();
            //gui = new AddComponentGUI(dataSource);
            s_AddItemWindow = this;
            m_searchKeyword = EditorPrefs.GetString(m_searchKey, "");
            showHeader = true;
        }

        protected override void OnDisable()
        {
            s_AddItemWindow = null;
            if (!string.IsNullOrEmpty(m_searchKey))
            {
                EditorPrefs.SetString(m_searchKey, m_searchKeyword);
            }
        }

        protected override Vector2 CalculateWindowSize(Rect buttonRect)
        {
            return new Vector2(buttonRect.width, kMaxWindowHeight);
        }

        internal static bool Show(Rect rect, IDescriptorCatalogue catalogue, Action<Descriptor> selectCallback, Func<Descriptor, bool> filterCallback = null)
        {
            CloseAllOpenWindows<AddItemWindow>();


            Event.current.Use();
            s_AddItemWindow = CreateInstance<AddItemWindow>();
            s_AddItemWindow.m_searchKey = catalogue?.ToString();
            s_AddItemWindow.DataSource = new AddItemDataSource(catalogue.Root, selectCallback, filterCallback, null);
            s_AddItemWindow.m_gui = new AddItemGUI(s_AddItemWindow.m_dataSource);

            float centralX = rect.center.x;
            rect.width = Mathf.Clamp(rect.width, kMinWindowWidth, kMaxWindowWidth);
            rect.x = centralX - rect.width * 0.5f;

            s_AddItemWindow.Init(rect);
            //s_AddItemWindow = CreateAndInit<AddItemWindow>(rect, new AddItemDataSource(catalogue.Root, selectCallback));
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