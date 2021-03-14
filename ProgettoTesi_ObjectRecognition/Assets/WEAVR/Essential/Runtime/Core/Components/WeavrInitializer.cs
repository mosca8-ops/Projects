using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Core
{

    [AddComponentMenu("")]
    public class WeavrInitializer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            States.UIComponents.RegisterStates();
            RegisterTypedAccessors();
        }

        private static void RegisterTypedAccessors()
        {
            Property.RegisterTypedAccessors(nameof(Vector2.x), (Vector2 v) => v.x, (Vector2 o, float v) => { o.x = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector2.y), (Vector2 v) => v.y, (Vector2 o, float v) => { o.y = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector2.magnitude), (Vector2 v) => v.magnitude, (Vector2 o, float v) => o);
            Property.RegisterTypedAccessors(nameof(Vector2.sqrMagnitude), (Vector2 v) => v.sqrMagnitude, (Vector2 o, float v) => o);
            Property.RegisterTypedAccessors(nameof(Vector2.normalized), (Vector2 v) => v.normalized, (Vector2 o, Vector2 v) => o);

            Property.RegisterTypedAccessors(nameof(Vector3.x), (Vector3 v) => v.x, (Vector3 o, float v) => { o.x = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector3.y), (Vector3 v) => v.y, (Vector3 o, float v) => { o.y = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector3.z), (Vector3 v) => v.z, (Vector3 o, float v) => { o.z = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector3.magnitude), (Vector3 v) => v.magnitude, (Vector3 o, float v) => o);
            Property.RegisterTypedAccessors(nameof(Vector3.sqrMagnitude), (Vector3 v) => v.sqrMagnitude, (Vector3 o, float v) => o);
            Property.RegisterTypedAccessors(nameof(Vector3.normalized), (Vector3 v) => v.normalized, (Vector3 o, Vector3 v) => o);

            Property.RegisterTypedAccessors(nameof(Vector4.x), (Vector4 v) => v.x, (Vector4 o, float v) => { o.x = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector4.y), (Vector4 v) => v.y, (Vector4 o, float v) => { o.y = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector4.z), (Vector4 v) => v.z, (Vector4 o, float v) => { o.z = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector4.w), (Vector4 v) => v.w, (Vector4 o, float v) => { o.w = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector4.magnitude), (Vector4 v) => v.magnitude, (Vector4 o, float v) => o);
            Property.RegisterTypedAccessors(nameof(Vector4.sqrMagnitude), (Vector4 v) => v.sqrMagnitude, (Vector4 o, float v) => o);
            Property.RegisterTypedAccessors(nameof(Vector4.normalized), (Vector4 v) => v.normalized, (Vector4 o, Vector4 v) => o);

            Property.RegisterTypedAccessors(nameof(Vector2Int.x), (Vector2Int v) => v.x, (Vector2Int o, int v) => { o.x = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector2Int.y), (Vector2Int v) => v.y, (Vector2Int o, int v) => { o.y = v; return o; });

            Property.RegisterTypedAccessors(nameof(Vector3Int.x), (Vector3Int v) => v.x, (Vector3Int o, int v) => { o.x = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector3Int.y), (Vector3Int v) => v.y, (Vector3Int o, int v) => { o.y = v; return o; });
            Property.RegisterTypedAccessors(nameof(Vector3Int.z), (Vector3Int v) => v.z, (Vector3Int o, int v) => { o.z = v; return o; });

            Property.RegisterTypedAccessors(nameof(Quaternion.x), (Quaternion v) => v.x, (Quaternion o, float v) => { o.x = v; return o; });
            Property.RegisterTypedAccessors(nameof(Quaternion.y), (Quaternion v) => v.y, (Quaternion o, float v) => { o.y = v; return o; });
            Property.RegisterTypedAccessors(nameof(Quaternion.z), (Quaternion v) => v.z, (Quaternion o, float v) => { o.z = v; return o; });
            Property.RegisterTypedAccessors(nameof(Quaternion.w), (Quaternion v) => v.w, (Quaternion o, float v) => { o.w = v; return o; });
            Property.RegisterTypedAccessors(nameof(Quaternion.eulerAngles), (Quaternion v) => v.eulerAngles, (Quaternion o, Vector3 v) => { o.eulerAngles = v; return o; });
            Property.RegisterTypedAccessors(nameof(Quaternion.normalized), (Quaternion v) => v.normalized, (Quaternion o, Quaternion v) => o);

            Property.RegisterTypedAccessors(nameof(Color.r), (Color v) => v.r, (Color o, float v) => { o.r = v; return o; });
            Property.RegisterTypedAccessors(nameof(Color.g), (Color v) => v.g, (Color o, float v) => { o.g = v; return o; });
            Property.RegisterTypedAccessors(nameof(Color.b), (Color v) => v.b, (Color o, float v) => { o.b = v; return o; });
            Property.RegisterTypedAccessors(nameof(Color.a), (Color v) => v.a, (Color o, float v) => { o.a = v; return o; });
            Property.RegisterTypedAccessors(nameof(Color.gamma), (Color v) => v.gamma, (Color o, Color v) => o);
            Property.RegisterTypedAccessors(nameof(Color.linear), (Color v) => v.linear, (Color o, Color v) => o);
            Property.RegisterTypedAccessors(nameof(Color.grayscale), (Color v) => v.grayscale, (Color o, float v) => o);

            Property.RegisterTypedAccessors(nameof(Transform.position), (Transform o) => o.position, (Transform o, Vector3 v) => o.position = v);
            Property.RegisterTypedAccessors(nameof(Transform.localPosition), (Transform o) => o.localPosition, (Transform o, Vector3 v) => o.localPosition = v);
            Property.RegisterTypedAccessors(nameof(Transform.eulerAngles), (Transform o) => o.eulerAngles, (Transform o, Vector3 v) => o.eulerAngles = v);
            Property.RegisterTypedAccessors(nameof(Transform.localEulerAngles), (Transform o) => o.localEulerAngles, (Transform o, Vector3 v) => o.localEulerAngles = v);
            Property.RegisterTypedAccessors(nameof(Transform.right), (Transform o) => o.right, (Transform o, Vector3 v) => o.right = v);
            Property.RegisterTypedAccessors(nameof(Transform.up), (Transform o) => o.up, (Transform o, Vector3 v) => o.up = v);
            Property.RegisterTypedAccessors(nameof(Transform.forward), (Transform o) => o.forward, (Transform o, Vector3 v) => o.forward = v);
            Property.RegisterTypedAccessors(nameof(Transform.rotation), (Transform o) => o.rotation, (Transform o, Quaternion v) => o.rotation = v);
            Property.RegisterTypedAccessors(nameof(Transform.localRotation), (Transform o) => o.localRotation, (Transform o, Quaternion v) => o.localRotation = v);
            Property.RegisterTypedAccessors(nameof(Transform.parent), (Transform o) => o.parent, (Transform o, Transform v) => o.parent = v);
            Property.RegisterTypedAccessors(nameof(Transform.worldToLocalMatrix), (Transform o) => o.worldToLocalMatrix, (Transform o, Matrix4x4 v) => { });
            Property.RegisterTypedAccessors(nameof(Transform.localToWorldMatrix), (Transform o) => o.localToWorldMatrix, (Transform o, Matrix4x4 v) => { });
            Property.RegisterTypedAccessors(nameof(Transform.root), (Transform o) => o.root, (Transform o, Transform v) => { });
            Property.RegisterTypedAccessors(nameof(Transform.childCount), (Transform o) => o.childCount, (Transform o, int v) => { });
            Property.RegisterTypedAccessors(nameof(Transform.lossyScale), (Transform o) => o.lossyScale, (Transform o, Vector3 v) => { });
            Property.RegisterTypedAccessors(nameof(Transform.localScale), (Transform o) => o.localScale, (Transform o, Vector3 v) => o.localScale = v);
        }
    }
}
