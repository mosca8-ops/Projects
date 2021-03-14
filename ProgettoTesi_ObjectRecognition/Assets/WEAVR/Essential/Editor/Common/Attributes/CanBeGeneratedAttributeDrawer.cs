namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Common;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [CustomPropertyDrawer(typeof(CanBeGeneratedAttribute))]
    public class CanBeGeneratedAttributeDrawer : PropertyDrawer
    {
        private bool _firstRun = true;
        private UnityEngine.Object m_lastReference;
        private Type _typeToCreate;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            float width = property.objectReferenceValue != null ? 50 : 100;
            position.width -= width + 5;
            EditorGUI.PropertyField(position, property, label);

            CanBeGeneratedAttribute attr = attribute as CanBeGeneratedAttribute;

            _typeToCreate = attr.CreatedType ?? fieldInfo.FieldType;

            UnityEngine.Object target = property.serializedObject.targetObject;
            Transform targetTransform = null;
            if(property.serializedObject.targetObject is Component) {
                targetTransform = ((Component)property.serializedObject.targetObject).transform;
            }
            else if(property.serializedObject.targetObject is GameObject) {
                targetTransform = ((GameObject)property.serializedObject.targetObject).transform;
            }
            else {
                Debug.LogErrorFormat("{0}: Can be used only on Components or GameObjects", GetType().Name);
                return;
            }

            var lastColor = GUI.backgroundColor;

            position.x += position.width + 5;
            position.width = width;
            if (property.objectReferenceValue != null)
            {
                GUI.backgroundColor = Color.red;
                if (GUI.Button(position, "Delete"))
                {
                    var referenceGameObject = property.objectReferenceValue is Component ?
                                              ((Component)property.objectReferenceValue).gameObject :
                                              property.objectReferenceValue as GameObject;
                    DestroyGenerated(attr, targetTransform.gameObject, referenceGameObject);
                    property.objectReferenceValue = null;
                }
            }
            else if (m_lastReference != null)
            {
                // A delete or change operation have been detected
                var generated = GetGeneratedObject(m_lastReference);
                if (generated != null)
                {
                    generated.UnregisterUser(this);
                    if (generated.Users <= 0)
                    {
                        DestroyGenerated(attr, targetTransform.gameObject, generated.gameObject);
                    }
                }
            }
            else
            {
                position.width = position.width * 0.5f;
                bool mayContinue = true;
                if (GUI.Button(position, "Get", EditorStyles.miniButtonLeft)/* _firstRun*/)
                {
                    _firstRun = false;
                    // Check if there are relations with other objects in the hierarchy
                    TryGetReference(property, attr, target, targetTransform);
                    GetGeneratedObject(property.objectReferenceValue)?.RegisterUser(this);
                    mayContinue = false;
                }
                position.x += position.width;
                GUI.backgroundColor = Color.green;
                if (GUI.Button(position, "Create", EditorStyles.miniButtonRight) && mayContinue)
                {
                    GameObject newGO = GetRelatedGameObject(attr, target, targetTransform, property.serializedObject, attr.FallbackName ?? label.text);

                    if (_typeToCreate == typeof(Transform))
                    {
                        property.objectReferenceValue = newGO.transform;
                    }
                    else if (_typeToCreate.IsSubclassOf(typeof(Component)))
                    {
                        var newComponent = newGO.AddComponent(_typeToCreate);
                        property.objectReferenceValue = newComponent;
                    }

                    if (property.serializedObject.targetObject is Component)
                    {
                        var generatedComponent = newGO.AddComponent<GeneratedObject>();
                        generatedComponent.RegisterUser(this);
                        generatedComponent.Generator = target as Component;
                    }
                }
            }

            GUI.backgroundColor = lastColor;

            m_lastReference = property.objectReferenceValue;
        }

        private static void DestroyGenerated(CanBeGeneratedAttribute attr, GameObject generator, GameObject referenceGameObject) {
            if (referenceGameObject == null) {
                return;
            }
            var generatedComponent = referenceGameObject.GetComponent<GeneratedObject>();
            if (generatedComponent != null && generatedComponent.Generator != null && generatedComponent.Generator.gameObject == generator) {
                switch (attr.CreateAs) {
                    case Relationship.Child:
                        UnityEngine.Object.DestroyImmediate(referenceGameObject);
                        break;
                    case Relationship.Sibling:
                        UnityEngine.Object.DestroyImmediate(referenceGameObject);
                        break;
                    case Relationship.Unrelated:
                        UnityEngine.Object.DestroyImmediate(referenceGameObject);
                        break;
                }
            }
        }

        private GeneratedObject GetGeneratedObject(UnityEngine.Object obj)
        {
            if(obj == null) { return null; }
            if(obj is Component)
            {
                return ((Component)obj).GetComponent<GeneratedObject>();
            }
            if(obj is GameObject)
            {
                return ((GameObject)obj).GetComponent<GeneratedObject>();
            }
            return null;
        }

        private GameObject GetRelatedGameObject(CanBeGeneratedAttribute attr, UnityEngine.Object target, Transform targetTransform, SerializedObject serObject, string fallbackName) {
            GameObject newGO = target is Component ? ((Component)target).gameObject : target as GameObject;
            string defaultName = GetGeneratedPrefixName(attr, serObject, fallbackName);
            switch (attr.CreateAs) {
                case Relationship.Child:
                    newGO = new GameObject(defaultName + "_" + target.GetType().Name + attr.Suffix);
                    newGO.transform.SetParent(targetTransform, false);
                    break;
                case Relationship.Parent:
                    if (targetTransform.parent) {
                        newGO = targetTransform.parent.gameObject;
                    }
                    break;
                case Relationship.Sibling:
                    newGO = new GameObject(defaultName + "_" + target.GetType().Name + attr.Suffix);
                    if (targetTransform.parent) {
                        newGO.transform.SetParent(targetTransform.parent, false);
                    }
                    break;
                case Relationship.Unrelated:
                    newGO = new GameObject(defaultName + "_" + target.GetType().Name + attr.Suffix);
                    break;
            }

            return newGO;
        }

        private string GetGeneratedPrefixName(CanBeGeneratedAttribute attr, SerializedObject serObject, string fallbackName) {
            if (string.IsNullOrEmpty(attr.NameSourcePath)) {
                return fallbackName;
            }
            var otherProperty = serObject.FindProperty(attr.NameSourcePath);
            return otherProperty != null && !string.IsNullOrEmpty(otherProperty.stringValue) ? otherProperty.stringValue : string.IsNullOrEmpty(fallbackName) ? attr.NameSourcePath : fallbackName;
        }

        private void TryGetReference(SerializedProperty property, CanBeGeneratedAttribute attr, UnityEngine.Object target, Transform targetTransform) {
            switch (attr.CreateAs) {
                case Relationship.Child:
                    foreach (var generated in targetTransform.GetComponentsInChildren<GeneratedObject>()) {
                        if (generated.Generator  != null && generated.Generator.gameObject == targetTransform.gameObject && generated.GetComponent(_typeToCreate) != null) {
                            if (attr.Ownership != Ownership.Individual || generated.Generator == target)
                            {
                                property.objectReferenceValue = generated.GetComponent(_typeToCreate);
                            }
                            break;
                        }
                    }
                    break;
                case Relationship.Parent:
                    if (targetTransform.parent != null && targetTransform.parent.GetComponent(_typeToCreate) != null) {
                        property.objectReferenceValue = targetTransform.parent.GetComponent(_typeToCreate);
                    }
                    break;
                case Relationship.Sibling:
                    var siblings = targetTransform.parent != null ?
                                   targetTransform.parent.GetComponentsInChildren<GeneratedObject>() :
                                   SceneManager.GetActiveScene().GetRootGameObjects().Select(go => go.GetComponent<GeneratedObject>());
                    foreach (var generated in siblings) {
                        if (generated.gameObject != targetTransform.gameObject
                            && generated.Generator != null && generated.Generator.gameObject == targetTransform.gameObject
                            && generated.GetComponent(_typeToCreate) != null) {
                            if (attr.Ownership != Ownership.Individual || generated.Generator == target)
                            {
                                property.objectReferenceValue = generated.GetComponent(_typeToCreate);
                            }
                            break;
                        }
                    }
                    break;
                case Relationship.Unrelated:
                    foreach (var generated in GameObject.FindObjectsOfType<GeneratedObject>()) {
                        if (generated.gameObject != targetTransform.gameObject
                            && generated.Generator != null && generated.Generator.gameObject == targetTransform.gameObject
                            && generated.GetComponent(_typeToCreate) != null) {
                            property.objectReferenceValue = generated.GetComponent(_typeToCreate);
                            break;
                        }
                    }
                    break;
                case Relationship.Self:
                    property.objectReferenceValue = targetTransform.GetComponent(_typeToCreate);
                    break;
            }
        }
    }
}