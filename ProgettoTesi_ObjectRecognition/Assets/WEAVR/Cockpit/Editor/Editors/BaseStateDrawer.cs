using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Cockpit {
    [StateDrawer(typeof(BaseState))]
    public class BaseStateDrawer {
        protected static float _lineSkipHeight;

        public BaseState Target { get; private set; }
        public BaseState[] Targets { get; private set; }

        public string WarningMessage { get; protected set; }
        public Action WarningFixAction { get; protected set; }

        protected bool _overridesBinding = false;
        protected Color _overrideBoxColor;

        protected float _leftSideWidth = 80;
        protected float _labelWidth = 60;
        protected float _separatorWidth = 10;

        protected Color _handleColor;

        protected Rect[] _rects;

        public virtual void SetTargets(params BaseState[] targets) {
            WarningMessage = null;
            WarningFixAction = null;

            Targets = targets;
            Target = targets != null ? targets[0] : null;

            _overridesBinding = Target != null && Target.OverrideBinding != null;
            _overrideBoxColor = Color.gray;
            _overrideBoxColor.a = 0.3f;

            _rects = new Rect[3];   // left rect, right rect and line rect

            _lineSkipHeight = EditorGUIUtility.singleLineHeight + 2;
        }

        public virtual void OnInspectorGUI(Rect rect) {
            float lastLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = _labelWidth;
            _rects[0] = rect;
            _rects[0].height = EditorGUIUtility.singleLineHeight;
            _rects[0].width = _leftSideWidth;

            _rects[1] = _rects[0];
            _rects[1].x += _rects[0].width + _separatorWidth;
            _rects[1].width = rect.width - _rects[0].width - _separatorWidth;

            _rects[2] = rect;
            _rects[2].height = _rects[0].height;

            if (Target.IsEditable) {
                Target.Name = EditorGUI.TextField(_rects[0], Target.Name);
            }
            else {
                EditorGUI.SelectableLabel(_rects[0], Target.Name);
            }

            DrawBindingSelection(_rects[1]);

            MoveDownOneLine();

            if (_overridesBinding) {
                if (Target.OverrideBinding == null) {
                    Target.CreateOverrideBinding();
                }
                _rects[0].y += DrawOverrideBinding(_rects[2]);
            }
            else {
                if (Target.OverrideBinding != null) {
                    UnityEngine.Object.DestroyImmediate(Target.OverrideBinding);
                }
                Target.OverrideBinding = null;
            }

            _rects[0].y += 2;
        }

        protected void MoveDownOneLine() {
            _rects[0].y += _lineSkipHeight;
            _rects[2].y = _rects[1].y = _rects[0].y;
        }

        protected virtual float DrawOverrideBinding(Rect rect) {
            rect.height = BindingDrawer.GetHeight(Target.OverrideBinding) + 4;
            EditorGUI.DrawRect(rect, _overrideBoxColor);
            float returnValue = rect.height;
            rect.y += 2;
            rect.x += 2;
            rect.width -= 4;
            rect.height -= 4;
            BindingDrawer.DrawBinding(rect, Target.OverrideBinding, false);

            return returnValue;
        }

        protected virtual void DrawBindingSelection(Rect rect) {
            if (Target.Owner.Bindings.Count > 1) {
                float rectWidth = rect.width;
                rect.width *= 0.9f;
                bool wasEnabled = GUI.enabled;
                GUI.enabled = !_overridesBinding;
                Target.Binding = WeavrGUI.Popup(this, rect, "Bind to", Target.Binding, Target.Owner.Bindings, b => b.id);
                GUI.enabled = wasEnabled;
                rect.x += rect.width;
                _overridesBinding = EditorGUI.Toggle(rect, _overridesBinding);
            }
            else {
                EditorGUIUtility.labelWidth = 100;
                _overridesBinding = EditorGUI.Toggle(rect, "Override Binding", _overridesBinding);
                EditorGUIUtility.labelWidth = _labelWidth;
            }
        }

        public virtual float GetHeight() {
            return _overridesBinding ? EditorGUIUtility.singleLineHeight + BindingDrawer.GetHeight(Target.OverrideBinding) + 4
                                     : EditorGUIUtility.singleLineHeight + 2;
        }

        public virtual void OnSceneGUI() {

        }
    }
}