using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace TXT.WEAVR.Procedure
{

    class ErrorHandlerView : VisualElement
    {
        private Button m_errorMore;
        private VisualElement m_detailsContainer;
        private Label m_errorSubtitle;
        private Label m_errorTitle;
        private TextField m_errorDetails;
        
        public ErrorHandlerView()
        {
            var tpl = EditorGUIUtility.Load(WeavrEditor.PATH + "Creator/Resources/uxml/ErrorHandlerView.uxml") as VisualTreeAsset;
            tpl?.CloneTree(this);

            AddToClassList("errorContainer");

            m_errorMore = this.Q<Button>("error-more");
            m_errorTitle = this.Q<Label>("error-title");
            m_errorSubtitle = this.Q<Label>("error-subtitle");
            m_errorDetails = this.Q<TextField>("error-details");
            m_errorDetails.isReadOnly = true;
            m_detailsContainer = this.Q("details-container") ?? this;
            if(m_detailsContainer is ScrollView)
            {
                (m_detailsContainer as ScrollView).showHorizontal = false;
                (m_detailsContainer as ScrollView).horizontalScroller.RemoveFromHierarchy();
            }
            m_errorMore.clickable.clicked += ErrorMore_Clicked;
        }

        public void SetException(string title, Exception e)
        {
            m_errorTitle.text = title;
            m_errorSubtitle.text = $"Error: {e.Message}";
            m_errorDetails.value = e.StackTrace;
            //m_errorDetails.visible = false;
            if (m_detailsContainer != this)
            {
                m_detailsContainer.RemoveFromHierarchy();
            }
            m_errorDetails.RemoveFromHierarchy();
            m_errorMore.text = "Show more";
        }

        private void ErrorMore_Clicked()
        {
            if(m_errorDetails.panel == null)
            {
                m_errorMore.text = "Show less";
                if(m_detailsContainer != this)
                {
                    Add(m_detailsContainer);
                }
                m_detailsContainer.Add(m_errorDetails);
            }
            else
            {
                m_errorMore.text = "Show more";
                if(m_detailsContainer != this)
                {
                    m_detailsContainer.RemoveFromHierarchy();
                }
                m_errorDetails.RemoveFromHierarchy();
            }
            //m_errorDetails.visible = !m_errorDetails.visible;
        }
    }
}
