using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Procedure
{
    public abstract class BaseActionEditor : ProcedureObjectEditor
    {
        protected partial class ActionStyles : BaseStyles
        {
            public GUIStyle textButton;
            public GUIStyle textToggle;
            public GUIStyle headerLabel;
            public GUIStyle headerFocusedLabel;
            public GUIStyle descriptionLabel;
            public GUIStyle miniPreviewWindow;
            public GUIStyle miniPreviewLabel;
            public GUIStyle miniPreviewProgressBar;
            public GUIStyle isGlobalToggle;

            public float fullLineHeight;

            protected override void InitializeStyles(bool isProSkin)
            {
                textButton = WeavrStyles.EditorSkin2.FindStyle("actionEditor_TextButton") ?? WeavrStyles.MiniToggleTextOn;
                textToggle = WeavrStyles.EditorSkin2.FindStyle("actionEditor_TextToggle") ?? WeavrStyles.MiniToggleTextOn;
                headerLabel = WeavrStyles.EditorSkin2.FindStyle("actionEditor_HeaderLabel") ?? WeavrStyles.LeftGreyMiniLabel;
                descriptionLabel = WeavrStyles.EditorSkin2.FindStyle("actionEditor_DescriptionLabel") ?? new GUIStyle("Label");
                miniPreviewWindow = WeavrStyles.EditorSkin2.FindStyle("actionEditor_MiniPreviewWindow") ?? new GUIStyle("Box");
                miniPreviewLabel = WeavrStyles.EditorSkin2.FindStyle("actionEditor_MiniPreviewLabel") ?? WeavrStyles.LeftGreyMiniLabel;
                miniPreviewProgressBar = WeavrStyles.EditorSkin2.FindStyle("actionEditor_MiniPreviewProgressBar") ?? WeavrStyles.LeftGreyMiniLabel;
                isGlobalToggle = WeavrStyles.EditorSkin2.FindStyle("graphObjectEditor_isGlobalToggle");

                fullLineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                AdditionalInitialization(isProSkin);
            }

            partial void AdditionalInitialization(bool isProSkin);
        }

        protected static ActionStyles s_baseStyles = new ActionStyles();

        public abstract float GetHeight();
    }
}
