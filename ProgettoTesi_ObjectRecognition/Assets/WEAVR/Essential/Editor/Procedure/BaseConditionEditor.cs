using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    //[CustomEditor(typeof(BaseCondition), true)]
    public abstract class BaseConditionEditor : ProcedureObjectEditor
    {
        protected const float k_defaultHeaderHeight = 12;

        public partial class Styles : BaseStyles
        {
            public GUIStyle notToggle;
            public GUIStyle closeButton;
            public GUIStyle separatorLabel;
            //public GUIStyle box;
            public GUIStyle addButton;
            public GUIStyle textToggle;
            public GUIStyle header;

            public GUIStyle negativeBox;
            public GUIStyle positiveBox;
            public GUIStyle positiveGlobalBox;
            public GUIStyle selectedBox;
            public GUIStyle isGlobalToggle;

            protected override void InitializeStyles(bool isProSkin)
            {
                notToggle = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_notToggle");
                isGlobalToggle = WeavrStyles.EditorSkin2.FindStyle("graphObjectEditor_isGlobalToggle");
                closeButton = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_closeButton") ??
                                        new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                separatorLabel = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_separatorLabel") ??
                                        new GUIStyle(EditorStyles.centeredGreyMiniLabel);

                negativeBox = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_NegativeBox") ?? new GUIStyle("Box");
                positiveBox = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_PositiveBox") ?? new GUIStyle("Box");
                positiveGlobalBox = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_PositiveGlobalBox") ?? positiveBox;
                selectedBox = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_SelectedBox");

                //box = negativeBox;

                addButton = WeavrStyles.EditorSkin2.FindStyle("conditionIntraAddButton") ?? new GUIStyle("Button");
                textToggle = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_TextToggle") ?? WeavrStyles.MiniToggleTextOn;

                header = new GUIStyle(EditorStyles.centeredGreyMiniLabel);

                AdditionalInitialization(isProSkin);
            }

            partial void AdditionalInitialization(bool isProSkin);
        }

        protected static Styles s_styles = new Styles();

        public static Styles AllStyles
        {
            get
            {
                s_styles?.Refresh();
                return s_styles;
            }
        }

        public abstract float GetHeight();
    }
}
