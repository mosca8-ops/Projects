namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Editor;
    using TXT.WEAVR.Tools;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomEditor(typeof(BaseCockpitElement), true)]
    [CanEditMultipleObjects]
    public class ElementEditor : Editor
    {
        #region [  STATIC  ]

        protected static Dictionary<DiscreteStateAttribute, Type> _discreteStatesTypes;
        protected static Dictionary<ModifierStateAttribute, Type> _modifierStatesTypes;

        #endregion

        #region [  FIELDS  ]

        protected PropertyPathField _outputBindingField;
        protected PropertyPathField _inputBindingField;

        protected ReorderableList _reorderableStatesList;
        protected ReorderableList _reorderableModifiersList;
        protected ReorderableList _reorderableBindingsList;
        protected List<BaseStateDrawer> _stateDrawers;
        protected List<BaseStateDrawer> _modifierDrawers;

        private bool _bindingFoldout;
        private bool _statesFoldout;
        private bool _modifiersFoldout;
        private bool _wasEnabled;

        private UnityEditor.Animations.AnimatorController _animatorController;

        #endregion

        #region [  PROPERTIES  ]

        public virtual ReorderableList ReorderableStatesList {
            get {
                if(_reorderableStatesList == null) {
                    _reorderableStatesList = InstantiateStatesReorderableList(target as BaseCockpitElement);
                }
                return _reorderableStatesList;
            }
        }

        public virtual ReorderableList ReorderableModifiersList {
            get {
                if (_reorderableModifiersList == null) {
                    _reorderableModifiersList = InstantiateModifiersReorderableList(target as BaseCockpitElement);
                }
                return _reorderableModifiersList;
            }
        }

        public virtual ReorderableList ReorderableBindingsList {
            get {
                if (_reorderableBindingsList == null) {
                    _reorderableBindingsList = InstantiateBindingsReorderableList(target as BaseCockpitElement);
                }
                return _reorderableBindingsList;
            }
        }

        #endregion

        #region [  INITIALIZATIONS  ]

        protected virtual void OnEnable() {
            _bindingFoldout = true;
            var element = target as BaseCockpitElement;
            _stateDrawers = new List<BaseStateDrawer>();
            _modifierDrawers = new List<BaseStateDrawer>();
            foreach (var state in element.States) {
                _stateDrawers.Add(StateEditor.GetDrawer(state));
            }
            foreach (var modifier in element.Modifiers) {
                _modifierDrawers.Add(StateEditor.GetDrawer(modifier));
            }
            _wasEnabled = ((BaseCockpitElement)target).enabled;

            _statesFoldout = EditorApplication.isPlaying;
            _modifiersFoldout = EditorApplication.isPlaying;
        }

        protected static void InitializeTypesStructure() {
            if (_discreteStatesTypes == null) {
                _discreteStatesTypes = EditorTools.GetAttributesWithTypes<DiscreteStateAttribute>();
            }
            if (_modifierStatesTypes == null) {
                _modifierStatesTypes = EditorTools.GetAttributesWithTypes<ModifierStateAttribute>();
            }
        }

        #endregion

        #region [  REORDERABLE LISTS  ]

        protected virtual ReorderableList InstantiateStatesReorderableList(BaseCockpitElement element) {
            Color selectedColor = WeavrStyles.Colors.focusedTransparent;
            selectedColor.a = 0.3f;
            Color currentStateColor = WeavrStyles.Colors.green;
            currentStateColor.a = 0.3f;
            ReorderableList reorderableList = new ReorderableList(_stateDrawers, typeof(BaseStateDrawer)) {
                onAddDropdownCallback = (r, i) => GetDiscreteTypesContextMenu().ShowAsContext(),
                drawElementCallback = (rect, index, isActive, isFocused) => {
                    rect.y += 2;
                    rect.height -= 4;
                    _stateDrawers[index].OnInspectorGUI(rect);
                    if (index < element.States.Count - 1) {
                        rect.y += rect.height;
                        if (element.IsCustomizable) {
                            rect.x -= 16;
                            rect.width += 16;
                        }
                        rect.height = 2;
                        EditorGUI.DrawRect(rect, Color.gray);
                    }
                },
                elementHeightCallback = i => _stateDrawers[i].GetHeight() + 4,
                headerHeight = 2,
                drawElementBackgroundCallback = (rect, index, isActive, isFocused) => {
                    // Do not draw anything
                    if (isFocused) {
                        EditorGUI.DrawRect(rect, selectedColor);
                    }
                    else if (index >= 0 && _stateDrawers[index].Target == element.CurrentState) {
                        EditorGUI.DrawRect(rect, currentStateColor);
                    }
                },
                onRemoveCallback = l => {
                    if (0 <= l.index && l.index < _stateDrawers.Count) {
                        var drawerToRemove = _stateDrawers[l.index];
                        if (_stateDrawers.Remove(drawerToRemove)) {
                            element.RemoveState(l.index);
                        }
                    }
                },
                displayAdd = element.IsCustomizable,
                displayRemove = element.IsCustomizable,
                onCanRemoveCallback = i => element.IsCustomizable,
                onCanAddCallback = i => element.IsCustomizable,
            };
            return reorderableList;
        }

        protected virtual ReorderableList InstantiateModifiersReorderableList(BaseCockpitElement element) {
            Color selectedColor = WeavrStyles.Colors.focusedTransparent;
            selectedColor.a = 0.3f;
            ReorderableList reorderableList = new ReorderableList(_modifierDrawers, typeof(BaseStateDrawer)) {
                onAddDropdownCallback = (r, i) => GetModifierTypesContextMenu().ShowAsContext(),
                drawElementCallback = (rect, index, isActive, isFocused) => {
                    rect.y += 2;
                    rect.height -= 4;
                    _modifierDrawers[index].OnInspectorGUI(rect);
                    if (index < element.Modifiers.Count - 1) {
                        rect.y += rect.height;
                        rect.height = 2;
                        EditorGUI.DrawRect(rect, Color.gray);
                    }
                },
                elementHeightCallback = i => _modifierDrawers[i].GetHeight() + 4,
                headerHeight = 2,
                drawElementBackgroundCallback = (rect, index, isActive, isFocused) => {
                    // Do not draw anything
                    if (isFocused) {
                        EditorGUI.DrawRect(rect, selectedColor);
                    }
                },
                onRemoveCallback = l => {
                    if (0 <= l.index && l.index < _modifierDrawers.Count) {
                        var drawerToRemove = _modifierDrawers[l.index];
                        if (_modifierDrawers.Remove(drawerToRemove)) {
                            element.RemoveModifier(l.index);
                        }
                    }
                },
                draggable = false,
                displayAdd = element.IsCustomizable,
                displayRemove = element.IsCustomizable,
                onCanRemoveCallback = i => element.IsCustomizable,
                onCanAddCallback = i => element.IsCustomizable,
            };
            return reorderableList;
        }

        protected virtual ReorderableList InstantiateBindingsReorderableList(BaseCockpitElement element) {
            Color selectedColor = WeavrStyles.Colors.focusedTransparent;
            selectedColor.a = 0.3f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            ReorderableList reorderableList = new ReorderableList(element.Bindings, typeof(Binding)) {
                onAddCallback = l => element.AddBinding("Variable " + element.Bindings.Count),
                drawElementCallback = (rect, index, isActive, isFocused) => {
                    rect.y += 2;
                    rect.height -= 3;
                    BindingDrawer.DrawBinding(rect, element.Bindings[index], !element.IsCustomizable);
                    if (index < element.Bindings.Count - 1) {
                        rect.y += rect.height;
                        rect.height = 1;
                        EditorGUI.DrawRect(rect, Color.gray);
                    }
                },
                elementHeightCallback = i => BindingDrawer.GetHeight(element.Bindings[i]) + 3,
                headerHeight = 2,
                drawElementBackgroundCallback = (rect, index, isActive, isFocused) => {
                    // Do not draw anything
                    if (isFocused) {
                        EditorGUI.DrawRect(rect, selectedColor);
                    }
                },
                draggable = false,
                onRemoveCallback = l => element.RemoveBinding(l.index),
                displayAdd = element.IsCustomizable,
                displayRemove = element.IsCustomizable,
                onCanRemoveCallback = i => element.IsCustomizable && element.Bindings.Count > 1,
                onCanAddCallback = i => element.IsCustomizable,
            };
            return reorderableList;
        }

        #endregion

        #region [  UNITY EDITOR OVERRIDES  ]

        public override void OnInspectorGUI() {
            EditorGUILayout.Space();

            BaseCockpitElement element = target as BaseCockpitElement;

            // Check whether to enable or disable the triggers
            if(_wasEnabled != element.enabled) {
                _wasEnabled = element.enabled;
                Transform parent = element.transform.parent ?? element.transform;
                foreach(var zone in parent.GetComponentsInChildren<StateTriggerZone>(true)) {
                    zone.gameObject.SetActive(_wasEnabled);
                }
            }

            element.ElementType = EditorGUILayout.TextField("Element Type", element.ElementType);

            if (element.States.Count > 0) {
                element.StartState = WeavrGUILayout.Popup(element, "Start State", element.StartState, element.States, s => s.Name);
            }

            DrawDefaultPose(element);

            DrawNotifications(element);

            _bindingFoldout = EditorGUILayout.Foldout(_bindingFoldout, "Binding", WeavrStyles.FoldoutBold);
            if (_bindingFoldout) {
                EditorGUILayout.BeginHorizontal();
                if (HasBindingDuplicateNames(element)) {
                    EditorGUILayout.HelpBox("Bindings with same id found", MessageType.Warning);
                }
                EditorGUILayout.EndHorizontal();
                ReorderableBindingsList.DoLayoutList();
            }

            if (element.IsCustomizable || element.States.Count > 0) {
                _statesFoldout = EditorGUILayout.Foldout(_statesFoldout, "States", WeavrStyles.FoldoutBold);
                if (_statesFoldout) {
                    var animatorController = EditorGUILayout.ObjectField("Animator", element.animator != null ? element.animator.runtimeAnimatorController : null, typeof(UnityEditor.Animations.AnimatorController), true) as UnityEditor.Animations.AnimatorController;
                    if (animatorController != null) {
                        if (element.animator == null) {
                            element.animator = element.GetComponent<Animator>();
                            if (element == null) {
                                element.animator = element.gameObject.AddComponent<Animator>();
                            }
                        }
                        //if(element.animator.runtimeAnimatorController != animatorController || element.AnimatorParameters.Count == 0) {
                        //    element.UpdateAnimatorParameters(animatorController.parameters);
                        //}
                        element.animator.runtimeAnimatorController = animatorController;
                        element.UseAnimator = EditorGUILayout.Toggle("Use Animator", element.UseAnimator);
                        element.animator.enabled = element.UseAnimator;
                    }
                    else if (element.animator != null) {
                        element.UseAnimator = false;
                        element.animator.runtimeAnimatorController = null;
                    }
                    if (HasStateDuplicateNames(element)) {
                        EditorGUILayout.HelpBox("States with same name found", MessageType.Warning);
                    }
                    ReorderableStatesList.DoLayoutList();
                }
            }

            if (element.IsCustomizable || element.Modifiers.Count > 0) {
                _modifiersFoldout = EditorGUILayout.Foldout(_modifiersFoldout, "Modifiers", WeavrStyles.FoldoutBold);
                if (_modifiersFoldout) {
                    if (HasModifiersDuplicateNames(element)) {
                        EditorGUILayout.HelpBox("Modifiers with same name found", MessageType.Warning);
                    }
                    ReorderableModifiersList.DoLayoutList();
                }
            }
            //base.OnInspectorGUI();

            EditorGUILayout.Space();
        }

        private void OnSceneGUI() {
            if (((BaseCockpitElement)target).enabled) {
                foreach (var stateDrawer in _stateDrawers) {
                    stateDrawer.OnSceneGUI();
                }
            }
        }

        #endregion

        #region [  DRAWINGS  ]

        private static void DrawDefaultPose(BaseCockpitElement element) {
            EditorGUILayout.BeginHorizontal();
            {
                bool wasEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.Vector3Field("Default Position", element.defaultLocalPosition);
                    EditorGUILayout.Vector3Field("Default Rotation", element.defaultLocalRotation.eulerAngles);
                }
                EditorGUILayout.EndVertical();
                GUI.enabled = wasEnabled;
                if (GUILayout.Button("Set", GUILayout.Height(32), GUILayout.Width(32))) {
                    Undo.RecordObject(element, "Updated default pose");
                    UpdateDefaultPose(element);
                }
                //if (GUILayout.Button("x", GUILayout.ExpandHeight(true), GUILayout.Width(20))) {
                //    Undo.RecordObject(element, "Updated default pose");
                //    element.defaultLocalPosition = Vector3.zero;
                //    element.defaultLocalRotation = Quaternion.identity;
                //}
            }
            EditorGUILayout.EndHorizontal();
            DrawCenteredButton("Reposition To Defaults", () => {
                Undo.RecordObject(element.transform, "Repositioned to defaults");
                element.transform.localPosition = element.defaultLocalPosition;
                element.transform.localRotation = element.defaultLocalRotation;
            });
        }

        private static void DrawCenteredButton(string text, Action action) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(text)) {
                action();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected virtual void DrawBindings(BaseCockpitElement element) {
            bool separate = false;
            foreach (var binding in element.Bindings) {
                if (separate) { EditorGUILayout.Separator(); }
                BindingDrawer.DrawBinding(binding, false);
                separate = true;
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Binding")) {
                element.AddBinding("Binding " + element.Bindings.Count);
            }
            EditorGUILayout.EndHorizontal();
            //EditorGUI.indentLevel--;
        }

        #endregion

        #region [  OTHER METHODS  ]

        private static void UpdateDefaultPose(BaseCockpitElement element) {
            element.defaultLocalPosition = element.transform.localPosition;
            element.defaultLocalRotation = element.transform.localRotation;
        }

        #endregion

        #region [  DUPLICATES DETECTION  ]

        protected virtual bool HasStateDuplicateNames(IEnumerable<BaseCockpitElement> element) {
            foreach (var elem in element) {
                if (HasStateDuplicateNames(elem)) {
                    return true;
                }
            }
            return false;
        }

        protected virtual bool HasStateDuplicateNames(BaseCockpitElement element) {
            for (int i = 0; i < element.States.Count - 1; i++) {
                for (int j = i + 1; j < element.States.Count; j++) {
                    if(element.States[i].Name == element.States[j].Name) {
                        return true;
                    }
                }
            }
            return false;
        }

        protected virtual bool HasModifiersDuplicateNames(BaseCockpitElement element) {
            for (int i = 0; i < element.Modifiers.Count - 1; i++) {
                for (int j = i + 1; j < element.Modifiers.Count; j++) {
                    if (element.Modifiers[i].Name == element.Modifiers[j].Name) {
                        return true;
                    }
                }
            }
            return false;
        }

        protected virtual bool HasBindingDuplicateNames(IEnumerable<BaseCockpitElement> element) {
            foreach (var elem in element) {
                if (HasBindingDuplicateNames(elem)) {
                    return true;
                }
            }
            return false;
        }

        protected virtual bool HasBindingDuplicateNames(BaseCockpitElement element) {
            for (int i = 0; i < element.Bindings.Count - 1; i++) {
                for (int j = i + 1; j < element.Bindings.Count; j++) {
                    if (element.Bindings[i].id == element.Bindings[j].id) {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
        
        #region [  CONTEXT MENUS  ]

        protected virtual GenericMenu GetDiscreteTypesContextMenu() {
            if (_discreteStatesTypes == null) {
                _discreteStatesTypes = EditorTools.GetAttributesWithTypes<DiscreteStateAttribute>();
            }
            GenericMenu menu = new GenericMenu();
            var element = (BaseCockpitElement)target;
            foreach (var keyValuePair in _discreteStatesTypes) {
                if(keyValuePair.Value == typeof(IdleState) && element.States.Any(s => s is IdleState)) {
                    // Only one idle state is available
                    continue;
                }
                menu.AddItem(new GUIContent(keyValuePair.Key.StateTypeName),
                             false,
                             () => {
                                 var newState = CreateInstance(keyValuePair.Value) as BaseDiscreteState;
                                 newState.Name = "State " + (element.States.Count + 1);
                                 element.AddState(newState);
                                 _stateDrawers.Add(StateEditor.GetDrawer(newState));
                             });
            }
            return menu;
        }

        protected virtual GenericMenu GetModifierTypesContextMenu() {
            if (_modifierStatesTypes == null) {
                _modifierStatesTypes = EditorTools.GetAttributesWithTypes<ModifierStateAttribute>();
            }
            GenericMenu menu = new GenericMenu();
            foreach (var keyValuePair in _modifierStatesTypes) {
                menu.AddItem(new GUIContent(keyValuePair.Key.StateTypeName),
                             false,
                             () => {
                                 var element = (BaseCockpitElement)target;
                                 var newModifier = CreateInstance(keyValuePair.Value) as BaseModifierState;
                                 newModifier.Name = "Modifier " + (element.Modifiers.Count + 1);
                                 element.AddModifier(newModifier);
                                 _modifierDrawers.Add(StateEditor.GetDrawer(newModifier));
                             });
            }
            return menu;
        }

        #endregion

        #region [  NOTIFICATIONS  ]

        protected virtual void DrawNotifications(BaseCockpitElement element) {
            // Get the panel
            var panel = element.GetComponentInParent<BaseCockpitPanel>();
            if(panel != null) {
                // Check if in canonical pose
                ShowCanonicalWarnings(element, panel);
            }
            else {
                EditorGUILayout.HelpBox("NO PANEL FOUND: This cockpit element does not belong to any panel. It is recommended to add it to a panel", MessageType.Warning);
            }

            List<Action> fixActions = new List<Action>();
            // Get warnings from state drawers
            foreach(var stateDrawer in _stateDrawers) {
                if(stateDrawer.WarningMessage != null) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(string.Concat(stateDrawer.Target.Name, ": ", stateDrawer.WarningMessage), MessageType.Warning);
                    if (stateDrawer.WarningFixAction != null) {
                        fixActions.Add(stateDrawer.WarningFixAction);
                        if (GUILayout.Button("Fix", GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2))) {
                            stateDrawer.WarningFixAction();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            foreach (var modifier in _modifierDrawers) {
                if (modifier.WarningMessage != null) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(string.Concat(modifier.Target.Name, ": ", modifier.WarningMessage), MessageType.Warning);
                    if (modifier.WarningFixAction != null) {
                        fixActions.Add(modifier.WarningFixAction);
                        if (GUILayout.Button("Fix", GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2))) {
                            modifier.WarningFixAction();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (fixActions.Count > 1) {
                DrawCenteredButton("Fix All", () => {
                    foreach (var action in fixActions) {
                        action();
                    }
                });
            }
        }

        #endregion

        #region [  CANONICAL ISSUES RELATED  ]

        private static void ShowCanonicalWarnings(BaseCockpitElement element, BaseCockpitPanel panel) {
            // Check if suitable parent is present
            bool isParentPresent = CheckCanonicalParent(element, panel, true);

            bool isPositionCanonical = CheckCanonicalPosition(element, panel, isParentPresent);

            bool isRotationCanonical = CheckCanonicalRotation(element, panel, isPositionCanonical);

            if(!(isParentPresent && isRotationCanonical && isPositionCanonical)) {
                DrawCenteredButton("Attempt AutoFix", () => {
                    if (!isParentPresent) {
                        FixCanonicalParent(element);
                        isPositionCanonical = CheckCanonicalPosition(element, panel, false);
                    }
                    if (!isPositionCanonical) {
                        FixCanonicalPosition(element);
                        isRotationCanonical = CheckCanonicalRotation(element, panel, false);
                    }
                    if (!isRotationCanonical) { FixCanonicalRotation(element, panel); }
                });
            }
        }

        private static bool CheckCanonicalRotation(BaseCockpitElement element, BaseCockpitPanel panel, bool showFixButton) {
            var lastLocalRotation = element.transform.localRotation;
            element.transform.localRotation = element.defaultLocalRotation;
            bool isRotationCanonical = Mathf.Approximately(Vector3.Dot(element.transform.up, panel.transform.up), 1);
            element.transform.localRotation = lastLocalRotation;

            if (!isRotationCanonical) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("ROTATION NOT CANONICAL: This element is not in a canonical rotation", MessageType.Warning);
                if (showFixButton && GUILayout.Button("Fix", GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2))) {
                    FixCanonicalRotation(element, panel);
                }
                EditorGUILayout.EndHorizontal();
            }

            return isRotationCanonical;
        }

        private static void FixCanonicalRotation(BaseCockpitElement element, BaseCockpitPanel panel) {
            WeavrObjectUtility.AlignMeshesToTransform(panel.transform, element.gameObject);
            WeavrObjectUtility.TransferRotationToParent(element.transform);
            UpdateDefaultPose(element);
        }

        private static bool CheckCanonicalPosition(BaseCockpitElement element, BaseCockpitPanel panel, bool showFixButton) {
            var panelBounds = WeavrObjectUtility.GetBounds(panel.transform, 0.01f);
            var elementBounds = WeavrObjectUtility.GetBounds(element.transform, 0.01f);
            bool isPositionCanonical = panelBounds.Intersects(elementBounds) || elementBounds.Contains(element.transform.position);

            if (!isPositionCanonical) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("POSITION NOT CANONICAL: This element is not in a canonical position", MessageType.Warning);
                if (showFixButton && GUILayout.Button("Fix", GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2))) {
                    FixCanonicalPosition(element);
                }
                EditorGUILayout.EndHorizontal();
            }

            return isPositionCanonical;
        }

        private static void FixCanonicalPosition(BaseCockpitElement element) {
            WeavrObjectUtility.ResetPivotPoint(WeavrObjectUtility.PivotPointOrigin.Center, element.gameObject);
            WeavrObjectUtility.TransferPositionToParent(element.transform);
            UpdateDefaultPose(element);
        }

        private static bool CheckCanonicalParent(BaseCockpitElement element, BaseCockpitPanel panel, bool showFixButton) {
            bool isParentPresent = element.transform.parent != null
                                            && element.transform.parent != panel.transform
                                            && element.transform.parent.childCount <= 2;
            if (isParentPresent && element.transform.parent.childCount == 2) {
                var otherChild = element.transform.parent.GetChild(0) == element.transform ?
                                 element.transform.parent.GetChild(1) : element.transform.parent.GetChild(0);

                isParentPresent = otherChild.GetComponentInChildren<StateTriggerZone>() != null || otherChild.gameObject.name.EndsWith("Triggers");
            }

            if (!isParentPresent) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("NO SUITABLE PARENT: This element does not have a suitable parent", MessageType.Warning);
                if (showFixButton && GUILayout.Button("Fix", GUILayout.Width(40), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2))) {
                    FixCanonicalParent(element);
                }
                EditorGUILayout.EndHorizontal();
            }

            return isParentPresent;
        }

        private static void FixCanonicalParent(BaseCockpitElement element) {
            WeavrObjectUtility.WrapInParent(element.transform);
            UpdateDefaultPose(element);
        }

        #endregion

    }
}
 