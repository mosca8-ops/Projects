using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using UnityEditor.Callbacks;
using UnityEngine;


namespace TXT.WEAVR.Core
{

    public static class LayerManager
    {
        //[DidReloadScripts]
        static void FixMissingLayers() {
            CreateRequiredLayers();
        }

        private static void CreateRequiredLayers() {
            foreach (var type in EditorTools.GetAllAssemblyTypes()) {
                var attribute = type.GetCustomAttribute<RequireLayersAttribute>();
                if (attribute != null) {
                    foreach (var layer in attribute.Layers) {
                        CreateLayer(layer);
                    }
                }
            }
        }

        /// <summary>
        /// Create a layer at the next available index. Returns silently if layer already exists.
        /// </summary>
        /// <param name="name">Name of the layer to create</param>
        /// <param name="collideWithOthers"> Whether to collide with other layers or not</param>
        public static void CreateLayer(string name, bool collideWithOthers = true) {
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentNullException("name", "New layer name string is either null or empty.");

            var tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layerProps = tagManager.FindProperty("layers");
            var propCount = layerProps.arraySize;

            UnityEditor.SerializedProperty firstEmptyProp = null;

            for (var i = 0; i < propCount; i++) {
                var layerProp = layerProps.GetArrayElementAtIndex(i);

                var stringValue = layerProp.stringValue;

                if (stringValue == name) return;

                if (i < 8 || stringValue != string.Empty) continue;

                if (firstEmptyProp == null)
                    firstEmptyProp = layerProp;
            }

            if (firstEmptyProp == null) {
                Debug.LogError("Maximum limit of " + propCount + " layers exceeded. Layer \"" + name + "\" not created.");
                return;
            }

            firstEmptyProp.stringValue = name;
            tagManager.ApplyModifiedProperties();
            if (collideWithOthers)
            {
                int layerId = LayerMask.NameToLayer(name);
                for (var i = 0; i < propCount; i++)
                {
                    var layerProp = layerProps.GetArrayElementAtIndex(i);

                    var stringValue = layerProp.stringValue;

                    Physics.IgnoreLayerCollision(LayerMask.NameToLayer(stringValue), layerId, false);
                }
            }
        }
    }
}
