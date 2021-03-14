using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityObject = UnityEngine.Object;

namespace TXT.WEAVR
{
    public static class Extensions
    {
        const BindingFlags k_BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static T GetObjectFromProperty<T>(this SerializedProperty property) where T : UnityEngine.Object
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
            {
                return (T)property.objectReferenceValue;
            }
            return null;
        }

        public static GameObject GetGameObject(this SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
            {
                if (property.objectReferenceValue is GameObject go)
                {
                    return go;
                }
                else if (property.objectReferenceValue is Component component)
                {
                    return component.gameObject;
                }
            }
            return null;
        }

        public static bool IsEditingInPrefabMode(this GameObject go)
        {
            if (EditorUtility.IsPersistent(go))
            {
                // if the game object is stored on disk, it is a prefab of some kind, despite not returning true for IsPartOfPrefabAsset =/
                return true;
            }
            else
            {
                // If the GameObject is not persistent let's determine which stage we are in first because getting Prefab info depends on it
                var mainStage = StageUtility.GetMainStageHandle();
                var currentStage = StageUtility.GetStageHandle(go);
                if (currentStage != mainStage)
                {
                    var prefabStage = PrefabStageUtility.GetPrefabStage(go);
                    if (prefabStage != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static T GetAttributeInParents<T>(this SerializedProperty property) where T : Attribute
        {
            var path = property.propertyPath;
            while (path != string.Empty)
            {
                var attribute = property.GetAttribute<T>();
                if (attribute != null)
                {
                    return attribute;
                }
                int index = path.LastIndexOf('.');
                if (index < 0)
                {
                    return null;
                }
                path = path.Substring(0, index);
                property = property.serializedObject.FindProperty(path);
            }
            return null;
        }

        public static IEnumerable<T> GetAttributesInParents<T>(this SerializedProperty property) where T : Attribute
        {
            var splits = property.propertyPath.Split('.');
            List<T> attributes = new List<T>();
            property = property.serializedObject.FindProperty(splits[0]);
            property.GetAttributesInParents(attributes);
            for (int i = 1; i < splits.Length; i++)
            {
                property = property.FindPropertyRelative(splits[i]);
                property.GetAttributesInParents(attributes);
            }
            return attributes;
        }

        private static void GetAttributesInParents<T>(this SerializedProperty property, List<T> list) where T : Attribute
        {
            var attributes = property.GetFieldInfo().GetCustomAttributes();
            foreach (var attribute in attributes)
            {
                if (attribute is T tAttr)
                {
                    list.Add(tAttr);
                }
            }
        }

        public static T GetAttribute<T>(this SerializedProperty property) where T : Attribute
        {
            return property.GetFieldInfo()?.GetAttribute<T>();
        }

        public static SerializedProperty GetParent(this SerializedProperty property)
        {
            if(property.depth == 0) { return null; }
            return property.serializedObject.FindProperty(property.propertyPath.Substring(0, property.propertyPath.LastIndexOf(property.name) - 1));
        }

        public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            Type type = property.serializedObject.targetObject.GetType();
            var splits = property.propertyPath.Split('.');
            FieldInfo field = null;
            for (int i = 0; i < splits.Length; i++)
            {
                var split = splits[i];

                if (split == "Array" && splits.Length > i + 1 && splits[i + 1].StartsWith("data"))
                {
                    // We have an array here...
                    if (type.IsArray)
                    {

                    }
                    else if (typeof(IList).IsAssignableFrom(type))
                    {

                    }
                }
                else
                {
                    field = type.GetField(split, k_BindingFlags);

                    while (field == null && type != null)
                    {
                        type = type.BaseType;
                        field = type?.GetField(split, k_BindingFlags);
                    }
                }

                if (field == null)
                {
                    return null;
                }

                type = field.FieldType;
            }

            return field;
        }

        public static Func<object, object> GetValueGetter(this SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetType().FieldPathGet(property.propertyPath);
        }

        public static Action<object, object> GetValueSetter(this SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetType().FieldPathSet(property.propertyPath);
        }

        public static Type GetPropertyType(this SerializedProperty property)
        {
            Type type = property.serializedObject.targetObject.GetType();
            var splits = property.propertyPath.Split('.');
            FieldInfo field = null;
            for (int i = 0; i < splits.Length; i++)
            {
                var split = splits[i];

                if (split == "Array" && splits.Length > i + 1 && splits[i + 1].StartsWith("data"))
                {
                    // We have an array here...
                    if (type.IsArray && type.HasElementType)
                    {
                        type = type.GetElementType();
                        i++;
                        continue;
                    }
                    else if (typeof(IList).IsAssignableFrom(type) && type.GenericTypeArguments.Length > 0)
                    {
                        type = type.GenericTypeArguments[0];
                        i++;
                        continue;
                    }
                }
                else
                {
                    field = type.GetField(split, k_BindingFlags);

                    while (field == null && type != null)
                    {
                        type = type.BaseType;
                        field = type?.GetField(split, k_BindingFlags);
                    }
                }

                if (field == null)
                {
                    return null;
                }

                type = field.FieldType;
            }

            return type;
        }

        public static void CleanUpAsset(this UnityObject obj)
        {
            if (!AssetDatabase.Contains(obj)) { return; }

            var assetPath = AssetDatabase.GetAssetPath(obj);
            HashSet<UnityObject> allAssets = new HashSet<UnityObject>(AssetDatabase.LoadAllAssetsAtPath(assetPath));

            CheckAssetsDependencies(AssetDatabase.LoadMainAssetAtPath(assetPath), new HashSet<UnityObject>(), allAssets);

            foreach (var asset in allAssets)
            {
                UnityObject.DestroyImmediate(asset, true);
            }
        }

        private static void CheckAssetsDependencies(UnityObject obj, HashSet<UnityObject> alreadyChecked, HashSet<UnityObject> refs)
        {
            if (alreadyChecked.Contains(obj) || !obj) { return; }

            alreadyChecked.Add(obj);
            refs.Remove(obj);

            SerializedObject serObj = new SerializedObject(obj);
            serObj.Update();
            var property = serObj.GetIterator();
            while (property.Next(property.propertyType == SerializedPropertyType.Generic))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                {
                    CheckAssetsDependencies(property.objectReferenceValue, alreadyChecked, refs);
                }
                //else if (property.isArray)
                //{

                //}
            }
        }

        public static string GetFullAssetPath(this UnityObject obj)
        {
            return Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets"), 6) + AssetDatabase.GetAssetPath(obj);
        }

        public static string GetFullAssetDirectory(this UnityObject obj)
        {
            return Path.GetDirectoryName(Path.Combine(Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets"), 6), AssetDatabase.GetAssetPath(obj)));
        }

        // Scriptable Object Extensions
        public static void FullSave(this ScriptableObject obj)
        {
            SaveToAssets(obj, obj);
        }

        public static void SaveToAssets(this ScriptableObject parent, ScriptableObject child, HideFlags childHideFlags = HideFlags.HideInHierarchy)
        {
            if (parent == null || !AssetDatabase.Contains(parent)) { return; }

            FullSaveRecursive(parent, child, new HashSet<ScriptableObject>(), childHideFlags);
        }

        private static void FullSaveRecursive(ScriptableObject parent, ScriptableObject obj, HashSet<ScriptableObject> alreadySaved, HideFlags hideFlags)
        {
            if (alreadySaved.Contains(obj)) { return; }
            alreadySaved.Add(obj);

            var serObj = new SerializedObject(obj);
            serObj.Update();
            var property = serObj.GetIterator();
            while (property.Next(property.propertyType == SerializedPropertyType.Generic))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue is ScriptableObject sObj)
                {
                    if (!AssetDatabase.Contains(property.objectReferenceValue))
                    {
                        property.objectReferenceValue.hideFlags = hideFlags;
                        AssetDatabase.AddObjectToAsset(property.objectReferenceValue, parent);
                    }
                    FullSaveRecursive(obj, sObj, alreadySaved, hideFlags);
                }
                //else if (property.isArray)
                //{

                //}
            }
            serObj.ApplyModifiedProperties();
        }

        public static void DestroyAsset(this ScriptableObject obj)
        {
            if (!obj) { return; }
            if (AssetDatabase.Contains(obj))
            {

            }
            Undo.DestroyObjectImmediate(obj);
        }

        public static GameObject GetGameObject(this UnityObject obj)
        {
            return !obj ? null : obj is GameObject go ? go : obj is Component c ? c.gameObject : null;
        }

        public static void CopyTo<T>(this T a, T b) where T : Component
        {
            var serA = new SerializedObject(a);
            var serB = new SerializedObject(b);

            var iter = serA.GetIterator();
            iter = serA.FindProperty("m_Name");
            while (iter.Next(iter.propertyType == SerializedPropertyType.Generic))
            {
                serB.CopyFromSerializedPropertyIfDifferent(iter);
            }
        }

        //######################################   [  PROPERTY  ]   #########################################

        private const string k_propertyPathPattern = @"\[(.*)\]\.(.*)";
        private static Regex m_pathRegex;

        public static Regex PropertyPathRegex
        {
            get
            {
                if (m_pathRegex == null)
                {
                    m_pathRegex = new Regex(k_propertyPathPattern);
                }
                return m_pathRegex;
            }
        }

        public static void Initialize(this Property property)
        {
            if (!string.IsNullOrEmpty(property.Path) && property.MemberInfo != null)
            {
                return;
            }

            property.MemberInfo = property.GetComponentType()?.GetMemberInfoFromPath(property.GetClearPath());
        }

        public static Component TryExtractComponent(this Property property, UnityObject target = null)
        {
            var componentType = GetComponentType(property);
            Component component = target as Component ?? property.Target as Component;
            if (componentType == null || (component != null && component.GetType().IsAssignableFrom(componentType)))
            {
                return component;
            }
            var gameObject = target?.GetGameObject() ?? property.Target?.GetGameObject();
            return gameObject ? gameObject.GetComponent(componentType) : null;
        }

        public static GameObject TryExtractGameObject(this Property property, UnityObject target = null)
        {
            return property.TryExtractComponent(target)?.gameObject;
        }

        public static Type GetComponentType(this Property property)
        {
            var match = PropertyPathRegex.Match(property.Path);
            if (!match.Success)
            {
                return null;
            }
            return Type.GetType(match.Groups[1].Value);
        }

        public static string GetClearPath(this Property property)
        {
            var match = PropertyPathRegex.Match(property.Path);
            return match.Success ? match.Groups[2].Value : property.Path;
        }

        //######################################   [  TYPE  ]   #########################################


        //######################################   [  GUI STYLE  ]   #########################################

        public static float CalcScreenHeight(this GUIStyle style, float contentHeight)
        {
            return style.border.vertical + style.margin.vertical + style.padding.vertical + contentHeight;
        }

        public static Rect GetContentRect(this GUIStyle style, Rect styleRect)
        {
            styleRect.width -= style.border.horizontal + style.margin.horizontal + style.padding.horizontal;
            styleRect.height -= style.border.vertical + style.margin.vertical + style.padding.vertical;
            styleRect.x += style.border.left + style.margin.left + style.padding.left;
            styleRect.y += style.border.top + style.margin.top + style.padding.top;

            return styleRect;
        }


        //######################################   [  MATERIALS  ]   #########################################

        public static void MakeTransparent(this Renderer renderer)
        {
            foreach (var material in renderer.materials)
            {
                MakeTransparent(material);
            }
        }

        public static void MakeTransparent(this Renderer renderer, float alpha)
        {
            foreach (var material in renderer.materials)
            {
                MakeTransparent(material, alpha);
            }
        }

        public static void ChangeAlpha(this Renderer renderer, float newAlpha)
        {
            foreach (var material in renderer.materials)
            {
                ChangeAlpha(material, newAlpha);
            }
        }

        public static void MakeAllTransparent(this IEnumerable<Material> materials)
        {
            foreach (var material in materials)
            {
                MakeTransparent(material);
            }
        }

        public static void ChangeAlphaToAll(this IEnumerable<Material> materials, float alpha)
        {
            foreach (var material in materials)
            {
                ChangeAlpha(material, alpha);
            }
        }

        public static void MakeTransparent(this Material material)
        {
            if (material.GetInt("_Mode") == 0 || material.GetInt("_Mode") == 1)
            {
                material.SetInt("_Mode", 2);

                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }

        public static void MakeTransparent(this Material material, float alpha)
        {
            if (material.GetInt("_Mode") == 0 || material.GetInt("_Mode") == 1)
            {
                material.SetInt("_Mode", 2);

                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            var color = material.color;
            color.a = Mathf.Clamp01(alpha);
            material.color = color;
        }

        public static void ChangeAlpha(this Material material, float newAlpha)
        {
            var color = material.color;
            color.a = Mathf.Clamp01(newAlpha);
            material.color = color;
        }

        //######################################   [  RECT  ]   #########################################

        public static bool Contains(this Rect rect, Rect toTest)
        {
            return rect.xMin <= toTest.xMin && rect.yMin <= toTest.yMin
                && rect.xMax >= toTest.xMax && rect.yMax >= toTest.yMax;
        }

        public static bool Contains(this Rect rect, Rect toTest, float margin)
        {
            return rect.xMin - margin <= toTest.xMin && rect.yMin - margin <= toTest.yMin
                && rect.xMax + margin >= toTest.xMax && rect.yMax + margin >= toTest.yMax;
        }

        public static bool ContainsAbs(this Rect rect, Rect toTest, float margin)
        {
            return rect.xMin - margin <= toTest.xMin && rect.yMin - margin <= toTest.yMin
                && rect.xMax + margin >= toTest.xMax && rect.yMax + margin >= toTest.yMax;
        }

        public static Rect MakeAbsolute(this Rect rect)
        {
            return new Rect(Mathf.Min(rect.xMin, rect.xMax), Mathf.Min(rect.yMin, rect.yMax), Mathf.Abs(rect.width), Mathf.Abs(rect.height));
        }

        //######################################   [  ASSETS  ]   #########################################

        public enum ImageFilterMode : int
        {
            Nearest = 0,
            Biliner = 1,
            Average = 2
        }

        public static Texture2D RescaleTexture(this Texture2D pSource, ImageFilterMode pFilterMode, float pScale)
        {

            //*** Variables
            int i;

            //*** Get All the source pixels
            Color[] aSourceColor = pSource.GetPixels(0);
            Vector2 vSourceSize = new Vector2(pSource.width, pSource.height);

            //*** Calculate New Size
            float xWidth = Mathf.RoundToInt(pSource.width * pScale);
            float xHeight = Mathf.RoundToInt(pSource.height * pScale);

            //*** Make New
            Texture2D oNewTex = new Texture2D((int)xWidth, (int)xHeight, TextureFormat.RGBA32, false);

            //*** Make destination array
            int xLength = (int)xWidth * (int)xHeight;
            Color[] aColor = new Color[xLength];

            Vector2 vPixelSize = new Vector2(vSourceSize.x / xWidth, vSourceSize.y / xHeight);

            //*** Loop through destination pixels and process
            Vector2 vCenter = new Vector2();
            for (i = 0; i < xLength; i++)
            {

                //*** Figure out x&y
                float xX = i % xWidth;
                float xY = Mathf.Floor(i / xWidth);

                //*** Calculate Center
                vCenter.x = (xX / xWidth) * vSourceSize.x;
                vCenter.y = (xY / xHeight) * vSourceSize.y;

                //*** Do Based on mode
                //*** Nearest neighbour (testing)
                if (pFilterMode == ImageFilterMode.Nearest)
                {

                    //*** Nearest neighbour (testing)
                    vCenter.x = Mathf.Round(vCenter.x);
                    vCenter.y = Mathf.Round(vCenter.y);

                    //*** Calculate source index
                    int xSourceIndex = (int)((vCenter.y * vSourceSize.x) + vCenter.x);

                    //*** Copy Pixel
                    aColor[i] = aSourceColor[xSourceIndex];
                }

                //*** Bilinear
                else if (pFilterMode == ImageFilterMode.Biliner)
                {

                    //*** Get Ratios
                    float xRatioX = vCenter.x - Mathf.Floor(vCenter.x);
                    float xRatioY = vCenter.y - Mathf.Floor(vCenter.y);

                    //*** Get Pixel index's
                    int xIndexTL = (int)((Mathf.Floor(vCenter.y) * vSourceSize.x) + Mathf.Floor(vCenter.x));
                    int xIndexTR = (int)((Mathf.Floor(vCenter.y) * vSourceSize.x) + Mathf.Ceil(vCenter.x));
                    int xIndexBL = (int)((Mathf.Ceil(vCenter.y) * vSourceSize.x) + Mathf.Floor(vCenter.x));
                    int xIndexBR = (int)((Mathf.Ceil(vCenter.y) * vSourceSize.x) + Mathf.Ceil(vCenter.x));

                    //*** Calculate Color
                    aColor[i] = Color.Lerp(
                        Color.Lerp(aSourceColor[xIndexTL], aSourceColor[xIndexTR], xRatioX),
                        Color.Lerp(aSourceColor[xIndexBL], aSourceColor[xIndexBR], xRatioX),
                        xRatioY
                    );
                }

                //*** Average
                else if (pFilterMode == ImageFilterMode.Average)
                {

                    //*** Calculate grid around point
                    int xXFrom = (int)Mathf.Max(Mathf.Floor(vCenter.x - (vPixelSize.x * 0.5f)), 0);
                    int xXTo = (int)Mathf.Min(Mathf.Ceil(vCenter.x + (vPixelSize.x * 0.5f)), vSourceSize.x);
                    int xYFrom = (int)Mathf.Max(Mathf.Floor(vCenter.y - (vPixelSize.y * 0.5f)), 0);
                    int xYTo = (int)Mathf.Min(Mathf.Ceil(vCenter.y + (vPixelSize.y * 0.5f)), vSourceSize.y);

                    //*** Loop and accumulate
                    Color oColorTemp = new Color();
                    float xGridCount = 0;
                    for (int iy = xYFrom; iy < xYTo; iy++)
                    {
                        for (int ix = xXFrom; ix < xXTo; ix++)
                        {

                            //*** Get Color
                            oColorTemp += aSourceColor[(int)((iy * vSourceSize.x) + ix)];

                            //*** Sum
                            xGridCount++;
                        }
                    }

                    //*** Average Color
                    aColor[i] = oColorTemp / xGridCount;
                }
            }

            //*** Set Pixels
            oNewTex.SetPixels(aColor);
            oNewTex.Apply();

            //*** Return
            return oNewTex;
        }

        public static Texture2D RescaleTexture(this Texture2D pSource, int desiredHeight)
        {
            return !pSource.isReadable || pSource.height <= desiredHeight ? pSource : RescaleTexture(pSource, ImageFilterMode.Nearest, (float)desiredHeight / pSource.height);
        }

        //######################################   [  ASSETS  ]   #########################################

        public static void AddAsAsset<T>(this ICollection<T> list, T element, UnityEngine.Object parentAsset, bool updateAssets = false) where T : UnityEngine.Object
        {
            list.Add(element);
            if (parentAsset != null && element != null && !AssetDatabase.Contains(element))
            {
                AssetDatabase.AddObjectToAsset(element, parentAsset);
                if (element is ISaveAsAsset)
                {
                    ((ISaveAsAsset)element).OnSavedAsAsset();
                }
                if (updateAssets)
                {
                    EditorUtility.SetDirty(parentAsset);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public static bool RemoveAsAsset<T>(this ICollection<T> list, T element, bool updateAssets = false) where T : UnityEngine.Object
        {
            if (list.Remove(element) && element != null)
            {
                if (element is IRemovable<T>)
                {
                    Undo.DestroyObjectImmediate(((IRemovable<T>)element).OnRemove());
                }
                else
                {
                    Undo.DestroyObjectImmediate(element);
                }
                //UnityEngine.Object.DestroyImmediate(element, true);
                if (updateAssets)
                {
                    AssetDatabase.SaveAssets();
                }
                return true;
            }
            return false;
        }

        public static void SaveAsAsset(this Object obj, Object parentAsset, bool refreshAssets)
        {
            if (parentAsset != null && !AssetDatabase.Contains(obj))
            {
                AssetDatabase.AddObjectToAsset(obj, parentAsset);
                if (obj is ISaveAsAsset)
                {
                    ((ISaveAsAsset)obj).OnSavedAsAsset();
                }
                if (refreshAssets)
                {
                    EditorUtility.SetDirty(parentAsset);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public static void SaveAsAsset(this Object obj, Object parentAsset)
        {
            SaveAsAsset(obj, parentAsset, false);
        }

        public static bool DestroyAsAsset<T>(this IRemovable<T> removable, bool refreshAssets) where T : Object
        {
            if (AssetDatabase.Contains((Object)removable))
            {
                Undo.DestroyObjectImmediate(removable.OnRemove());
                //Object.DestroyImmediate(removable as Object, true);
                if (refreshAssets) AssetDatabase.Refresh();
                return true;
            }
            return false;
        }

        public static void ClearUndoProperty<T>(this Object owner, IRemovable<T> value, System.Action<T> setter) where T : Object
        {
            if (value != null)
            {
                Undo.RecordObject(owner, "Cleared property");
                setter(null);
                value.DestroyAsAsset();
            }
        }

        public static void ClearUndoProperty<T>(this Object owner, ICollection<T> value) where T : Object
        {
            var tempList = new List<T>(value);
            Undo.RecordObject(owner, "Cleared property");
            value.Clear();
            foreach (var elem in tempList)
            {
                if (elem is IRemovable<T>)
                {
                    ((IRemovable<T>)elem).DestroyAsAsset();
                }
                else if (AssetDatabase.Contains(elem))
                {
                    Undo.DestroyObjectImmediate(elem);
                }
            }
        }

        public static void ClearUndoProperty<T>(this Object owner, T[] value, System.Action<T[]> setter) where T : Object
        {
            var tempList = new List<T>(value);
            Undo.RecordObject(owner, "Cleared property");
            setter(null);
            foreach (var elem in tempList)
            {
                if (elem is IRemovable<T>)
                {
                    ((IRemovable<T>)elem).DestroyAsAsset();
                }
                else if (AssetDatabase.Contains(elem))
                {
                    Undo.DestroyObjectImmediate(elem);
                }
            }
        }

        public static bool DestroyAsAsset<T>(this IRemovable<T> removable) where T : Object
        {
            return DestroyAsAsset(removable, false);
        }
    }
}