namespace TXT.WEAVR.Cockpit
{
    using UnityEditor;
    using UnityEngine;

    [StateDrawer(typeof(BaseModifier))]
    public class BaseModifierDrawer : BaseModifierStateDrawer
    {
        private BaseModifier _modifier;

        private const int cHeightSpacing = 4;
        private const int cHorizontalSpacing = 4;

        private float m_Height;
        private SerializedObject _serializedObject;
        private SerializedProperty _interpolationPointProperty;
        private SerializedProperty _modifierAxis;
        private SerializedProperty _mappingMode;

        public override void SetTargets(params BaseState[] targets)
        {
            base.SetTargets(targets);
            _modifier = (BaseModifier)Target;
            _serializedObject = new SerializedObject(_modifier);
            _interpolationPointProperty = _serializedObject.FindProperty("m_InterpolationPoints");
            _modifierAxis = _serializedObject.FindProperty("m_ModifierAxis");
            _mappingMode = _serializedObject.FindProperty("m_MappingMode");
        }

        protected virtual float DrawFactorMenu(Rect rect)
        {
            var lastLabelWidth = EditorGUIUtility.labelWidth;
            Rect wValueFactorLabelRect = new Rect(rect.x, rect.y, rect.width * 0.2f, _lineSkipHeight);
            Rect wValueFloatFieldRect = new Rect(wValueFactorLabelRect.x + wValueFactorLabelRect.width, rect.y, rect.width * 0.3f, _lineSkipHeight);
            Rect wAxisRect = new Rect(wValueFloatFieldRect.x + wValueFloatFieldRect.width + cHorizontalSpacing,
                wValueFloatFieldRect.y,
                rect.width * 0.5f,
                _lineSkipHeight);
            Rect wLimitsToggleRect = new Rect(rect.x, rect.y + _lineSkipHeight + 4, rect.width * 0.2f, _lineSkipHeight);

            EditorGUI.LabelField(wValueFactorLabelRect, "Value Factor");
            _modifier.ValueFactor = EditorGUI.FloatField(wValueFloatFieldRect, _modifier.ValueFactor);
            EditorGUI.PropertyField(wAxisRect, _modifierAxis, new GUIContent("Axis"));
            _modifier.HasLimits = EditorGUI.ToggleLeft(wLimitsToggleRect, "Limits", _modifier.HasLimits);

            if (_modifier.HasLimits)
            {
                EditorGUIUtility.labelWidth = 30;
                Rect wMinRect = new Rect(wLimitsToggleRect.x + wLimitsToggleRect.width + cHorizontalSpacing,
                    wLimitsToggleRect.y,
                    rect.width * 0.4f - cHorizontalSpacing,
                    _lineSkipHeight);
                Rect wMaxRect = new Rect(wMinRect.x + wMinRect.width + cHorizontalSpacing,
                    wLimitsToggleRect.y,
                    rect.width * 0.4f - cHorizontalSpacing * 2,
                    _lineSkipHeight);
                _modifier.MinLimit = EditorGUI.FloatField(wMinRect, "Min", _modifier.MinLimit);
                _modifier.MaxLimit = EditorGUI.FloatField(wMaxRect, "Max", _modifier.MaxLimit);
            }
            EditorGUIUtility.labelWidth = lastLabelWidth;
            return (_lineSkipHeight + cHeightSpacing) * 2;
        }


        protected virtual float DrawInterpolationMenu(Rect rect)
        {

            var lastLabelWidth = EditorGUIUtility.labelWidth;
            Rect wInterpolationRect = new Rect(rect.x + 8, rect .y, rect.width * 0.9f, _lineSkipHeight);
            Rect wAxisRect = new Rect(rect.x + rect.width * 0.4f, rect.y, + rect.width * 0.5f, _lineSkipHeight);
            EditorGUIUtility.labelWidth = wInterpolationRect.width * 0.1f;
            EditorGUI.PropertyField(wAxisRect, _modifierAxis, new GUIContent("Axis"));
            EditorGUIUtility.labelWidth = wInterpolationRect.width * 0.4f;
            EditorGUI.PropertyField(wInterpolationRect, _interpolationPointProperty, true);
            EditorGUIUtility.labelWidth = lastLabelWidth;

            return EditorGUI.GetPropertyHeight(_interpolationPointProperty) + cHeightSpacing;
        }

        public override void OnInspectorGUI(Rect rect)
        {
            base.OnInspectorGUI(rect);
            _serializedObject.Update();
            m_Height = _lineSkipHeight + cHeightSpacing;
            var lastLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = rect.width * 0.2f;
            Rect wModeRect = new Rect(_rects[0].x, _rects[0].y + cHeightSpacing, rect.width, _lineSkipHeight);
            EditorGUI.PropertyField(wModeRect, _mappingMode);
            EditorGUIUtility.labelWidth = lastLabelWidth;
            Rect wMappingRect = new Rect(_rects[0].x, wModeRect.y + _lineSkipHeight + cHeightSpacing, rect.width, _lineSkipHeight);
            switch ((BaseModifier.MappingMode) _mappingMode.intValue)
            {
                case BaseModifier.MappingMode.Factor:
                    m_Height += DrawFactorMenu(wMappingRect);
                    break;
                case BaseModifier.MappingMode.Interpolation:
                    m_Height += DrawInterpolationMenu(wMappingRect);
                    break;
            }
            _serializedObject.ApplyModifiedProperties();
        }

        public override float GetHeight()
        {
            return base.GetHeight() + m_Height;
        }
    }
}