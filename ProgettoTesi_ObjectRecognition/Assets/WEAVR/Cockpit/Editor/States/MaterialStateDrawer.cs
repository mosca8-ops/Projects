namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    [StateDrawer(typeof(MaterialState))]
    public class MaterialStateDrawer : BaseDiscreteStateDrawer
    {
        private static GUIContent _deltaContent;
        
        private static Vector2 _snapVector;
        private MaterialState _materialState;

        protected bool _showAdditionalInfo;

        public override void SetTargets(params BaseState[] targets) {
            WarningMessage = null;

            base.SetTargets(targets);

            if(_deltaContent == null) {
                _deltaContent = new GUIContent("Delta Pose", "Whether to use delta position and rotation or local ones");
            }
            _lineSkipHeight = EditorGUIUtility.singleLineHeight + 2;
            _materialState = (MaterialState)Target;

            _handleColor = Color.magenta;
            _snapVector = new Vector2(0.005f, 0.005f);

            if(_materialState.triggerZone == null) {
                WarningMessage = "Trigger zone not set";
            }
        }

        public override void OnInspectorGUI(Rect rect) {
            base.OnInspectorGUI(rect);


            if (_materialState.Owner.UseAnimator) {
                return;
            }

            MoveDownOneLine();

            _showAdditionalInfo = EditorGUI.Foldout(_rects[2], _showAdditionalInfo, "Advanced");
            if(!_showAdditionalInfo) { return; }

            MoveDownOneLine();

            bool wasEnabled = GUI.enabled;
            
            _materialState.materialEnabled = EditorGUI.ToggleLeft(_rects[0], "Material", _materialState.materialEnabled);
            GUI.enabled = _materialState.materialEnabled;
            Rect vectorRect = _rects[1];
            Rect buttonRect = _rects[1];
            vectorRect.width -= 40;
            buttonRect.width = 36;
            buttonRect.x += vectorRect.width + 4;
            _materialState.material = (UnityEngine.Material)EditorGUI.ObjectField(vectorRect, "" , _materialState.material, typeof(UnityEngine.Material), false);

        }


        public override float GetHeight() {
            return _materialState.Owner.UseAnimator ? base.GetHeight() + _lineSkipHeight : 
                (_showAdditionalInfo ? base.GetHeight() + _lineSkipHeight * 4 :  base.GetHeight() + _lineSkipHeight * 2);
        }




    }
}
