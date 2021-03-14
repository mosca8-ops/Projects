using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Procedure
{
    public class BaseAnimationBlockEditor : ProcedureObjectEditor
    {
        protected static readonly GUIContent s_dataSourceContent = new GUIContent("Input From");
        protected static readonly GUIContent s_targetSourceContent = new GUIContent("Target From");

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

            public float standardHeight;
            public float standardDoubleHeight;

            protected override void InitializeStyles(bool isProSkin)
            {
                textButton = WeavrStyles.EditorSkin2.FindStyle("actionEditor_TextButton") ?? WeavrStyles.MiniToggleTextOn;
                textToggle = WeavrStyles.EditorSkin2.FindStyle("actionEditor_TextToggle") ?? WeavrStyles.MiniToggleTextOn;
                headerLabel =/* WeavrStyles.EditorSkin2.FindStyle("animationBlock_HeaderLabel") ??*/ new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 11 };
                descriptionLabel = WeavrStyles.EditorSkin2.FindStyle("actionEditor_DescriptionLabel") ?? new GUIStyle("Label");
                miniPreviewWindow = WeavrStyles.EditorSkin2.FindStyle("actionEditor_MiniPreviewWindow") ?? new GUIStyle("Box");
                miniPreviewLabel = WeavrStyles.EditorSkin2.FindStyle("actionEditor_MiniPreviewLabel") ?? WeavrStyles.LeftGreyMiniLabel;
                miniPreviewProgressBar = WeavrStyles.EditorSkin2.FindStyle("actionEditor_MiniPreviewProgressBar") ?? WeavrStyles.LeftGreyMiniLabel;

                AdditionalInitialization(isProSkin);

                standardHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                standardDoubleHeight = standardHeight * 2;
            }

            partial void AdditionalInitialization(bool isProSkin);
        }

        protected static ActionStyles s_baseStyles = new ActionStyles();
    }
}
