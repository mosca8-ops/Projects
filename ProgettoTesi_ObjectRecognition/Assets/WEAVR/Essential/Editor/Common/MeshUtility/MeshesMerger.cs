using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TXT.WEAVR;
using TXT.WEAVR.Common;

public class MeshesMerger
{
    public const float k_SimilarityEpsilon = 0.01f;

    public MaterialMergeCriteria MaterialMergeCriteria { get; set; }

    public MeshesMerger(MaterialMergeCriteria materialMergeCriteria)
    {
        MaterialMergeCriteria = materialMergeCriteria;
    }

    public (Mesh mesh, Material[] materials) CombineChildren(GameObject source, bool mergeMaterials = false)
    {
        var transform = source.transform;
        var rotation = transform.rotation;
        //var localScale = transform.localScale;
        //var factor = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1 / transform.lossyScale.z);
        //transform.localScale = Vector3.Scale(localScale, factor);
        transform.rotation = Quaternion.identity;
        var meshFilters = source.GetComponentsInChildren<MeshFilter>().Where(m => m && m.sharedMesh).ToArray();
        var meshData = meshFilters.Select(m => new SmartMeshData(m.sharedMesh,
                                                m.GetComponent<Renderer>().sharedMaterials,
                                                m.transform.position - transform.position,
                                                Quaternion.RotateTowards(transform.rotation, m.transform.rotation, 180),
                                                m.transform.lossyScale));

        var indexFormat = meshData.Sum(d => d.mesh.vertexCount) > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;

        Mesh mesh = new Mesh();
        Material[] materials;
        if (mergeMaterials)
        {
            mesh.CombineMeshesSmart(meshData.ToArray(), out materials, AreSimilar, indexFormat);
        }
        else
        {
            mesh.CombineMeshesSmart(meshData.ToArray(), out materials, indexFormat);
        }

        transform.rotation = rotation;
        //transform.localScale = localScale;

        return (mesh, materials);
    }

    public (Mesh mesh, Material[] materials) CombineList(GameObject source, IEnumerable<GameObject> objects, bool mergeMaterials = false)
    {
        var roots = objects.Select(t => t.transform)
            .Where(t => t != source.transform && (!t.parent || !objects.Contains(t.parent.gameObject)))
            .Select(t => InstantiateIdentical(t.gameObject))
            .ToArray();

        source = InstantiateIdentical(source);

        foreach (var root in roots)
        {
            root.transform.SetParent(null, true);
            root.transform.SetParent(source.transform, true);
        }

        var result = CombineChildren(source, mergeMaterials);

        if (Application.isPlaying)
            GameObject.Destroy(source);
        else
            GameObject.DestroyImmediate(source);

        return result;
    }

    public Transform GetFirstParentWithoutMesh(Transform t)
    {
        var parent = t.parent;
        while (parent && parent.GetComponent<MeshFilter>() && parent.GetComponent<MeshFilter>().sharedMesh)
        {
            parent = parent.parent;
        }
        return parent;
    }

    public IEnumerable<Transform> GetLeaves(Transform t)
    {
        List<Transform> leaves = new List<Transform>();
        if (t.childCount > 0)
        {
            GetLeavesRecursive(t, leaves);
        }
        else
        {
            leaves.Add(t);
        }
        return leaves;
    }

    public void GetLeavesRecursive(Transform t, List<Transform> leaves)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child.childCount > 0)
            {
                GetLeavesRecursive(child, leaves);
            }
            else
            {
                leaves.Add(child);
            }
        }
    }

    public IEnumerable<Transform> GetOutermostLeaves(Transform t)
    {
        var leaves = GetLeavesWithDepths(t);
        var max = leaves.OrderByDescending(p => p.depth).FirstOrDefault();
        return leaves.Where(p => p.depth == max.depth).Select(p => p.t);
    }

    public IEnumerable<(Transform t, int depth)> GetLeavesWithDepths(Transform t)
    {
        List<(Transform, int)> leaves = new List<(Transform, int)>();
        if (t.childCount > 0)
        {
            GetLeavesRecursive(t, 0, leaves);
        }
        else
        {
            leaves.Add((t, 0));
        }
        return leaves;
    }

    public void GetLeavesRecursive(Transform t, int currentDepth, List<(Transform, int)> leaves)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child.childCount > 0)
            {
                GetLeavesRecursive(child, currentDepth + 1, leaves);
            }
            else
            {
                leaves.Add((child, currentDepth + 1));
            }
        }
    }

    public GameObject InstantiateIdentical(GameObject go)
    {
        var clone = GameObject.Instantiate(go);
        clone.transform.SetParent(go.transform.parent, true);
        clone.transform.localPosition = go.transform.localPosition;
        clone.transform.localRotation = go.transform.localRotation;
        clone.transform.localScale = go.transform.localScale;
        return clone;
    }

    public Material[] SmartCloneMaterials(Material[] materials)
    {
        Material[] cloned = new Material[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            int sameMaterialIndex = -1;
            for (int j = 0; j < i; j++)
            {
                if (materials[i] == materials[j])
                {
                    sameMaterialIndex = j;
                    break;
                }
            }

            if (sameMaterialIndex < 0)
            {
                cloned[i] = new Material(materials[i]);
            }
            else
            {
                cloned[i] = cloned[sameMaterialIndex];
            }
        }
        return cloned;
    }

    public Material[] MergeMaterials(Material[] materials)
    {
        Material[] result = new Material[materials.Length];
        List<Material> uniqueMaterials = new List<Material>();
        for (int i = 0; i < materials.Length; i++)
        {
            int similarMaterialIndex = -1;
            for (int j = 0; j < uniqueMaterials.Count; j++)
            {
                if (AreSimilar(materials[i], uniqueMaterials[j]))
                {
                    similarMaterialIndex = j;
                    break;
                }
            }

            if (similarMaterialIndex < 0)
            {
                result[i] = materials[i];
                uniqueMaterials.Add(materials[i]);
            }
            else
            {
                result[i] = materials[similarMaterialIndex];
            }
        }

        return result;
    }

    public Material FindSimilar(Material material, params IEnumerable<Material>[] lists)
    {
        foreach (var list in lists)
        {
            var similar = list.FirstOrDefault(m => AreSimilar(m, material));
            if (similar)
            {
                return similar;
            }
        }
        return material;
    }

    public Material FindMostSimilar(Material material, params IEnumerable<Material>[] lists)
    {
        var mostSimilar = lists.SelectMany(l => l).Select(m => (m, GetSimilararityScore(material, m))).OrderByDescending(m => m.Item2).FirstOrDefault();
        return mostSimilar.Item2 > 0.5f ? mostSimilar.m : material;
    }

    public bool AreSimilar(Material a, Material b)
    {
        if (a == b)
        {
            return true;
        }

        bool pass = !MaterialMergeCriteria.SameShader || a.shader == b.shader;

        if (pass && MaterialMergeCriteria.SameTexture && a.mainTexture != b.mainTexture)
        {
            pass = false;
        }

        if (pass && MaterialMergeCriteria.SameProperties && a.shader == b.shader)
        {
            pass = ArePropertiesEqual(a, b);
        }

        if (pass && MaterialMergeCriteria.SameColor)
        {
            pass = AreColorsEqual(a, b, MaterialMergeCriteria.SameColorEpsilon);
        }

        if (pass && MaterialMergeCriteria.SameName)
        {
            pass = a.name.SimilarityDistanceTo(b.name) <= MaterialMergeCriteria.SameNameDistance;
        }

        return pass;
    }

    private float GetSimilararityScore(Material a, Material b)
    {
        if (a == b)
        {
            return 1;
        }

        bool pass = !MaterialMergeCriteria.SameShader || a.shader == b.shader;

        if (pass && MaterialMergeCriteria.SameTexture && a.mainTexture != b.mainTexture)
        {
            pass = false;
        }

        if (pass && MaterialMergeCriteria.SameProperties && a.shader == b.shader)
        {
            pass = ArePropertiesEqual(a, b);
        }

        float similarity = 1;

        if (pass && (MaterialMergeCriteria.SameName || MaterialMergeCriteria.SameColor))
        {
            similarity = 0;
            float weight = 0;
            var sameColorEpsilon = MaterialMergeCriteria.SameColorEpsilon;
            if (MaterialMergeCriteria.SameColor)
            {
                weight++;
                if (Similar(a.color, b.color, sameColorEpsilon))
                {
                    similarity += 1 - Mathf.Clamp01(ColorsDistanceSquared(a.color, b.color) / sameColorEpsilon);
                }
                else if (a.HasProperty("_BaseColor") && b.HasProperty("_BaseColor") && Similar(a.GetColor("_BaseColor"), b.GetColor("_BaseColor"), sameColorEpsilon))
                {
                    similarity += 1 - Mathf.Clamp01(ColorsDistanceSquared(a.GetColor("_BaseColor"), b.GetColor("_BaseColor")) / sameColorEpsilon);
                }
            }
            if (MaterialMergeCriteria.SameName)
            {
                weight++;
                similarity += 1 - Mathf.Clamp01(a.name.SimilarityDistanceTo(b.name) / (float)MaterialMergeCriteria.SameNameDistance);
            }

            similarity /= weight;
        }

        return similarity;
    }

    private float ColorsDistanceSquared(Color a, Color b) => (a.r - b.r) * (a.r - b.r) - (a.g - b.g) * (a.g - b.g) - (a.b - b.b) * (a.b - b.b) - (a.a - b.a) * (a.a - b.a);

    private bool AreColorsEqual(Material a, Material b, float sameColorEpsilon)
    {
        bool pass = Similar(a.color, b.color, sameColorEpsilon);
        if (!pass && a.HasProperty("_BaseColor") && b.HasProperty("_BaseColor"))
        {
            pass = Similar(a.GetColor("_BaseColor"), b.GetColor("_BaseColor"), sameColorEpsilon);
        }

        return pass;
    }

    private static bool ArePropertiesEqual(Material a, Material b)
    {
        bool pass = true;
        for (int i = 0; i < a.shader.GetPropertyCount(); i++)
        {
            var propertyId = a.shader.GetPropertyNameId(i);
            var type = a.shader.GetPropertyType(i);
            switch (type)
            {
                case ShaderPropertyType.Color:
                    if (a.shader.GetPropertyName(i).ToLower() == "_basecolor" || a.shader.GetPropertyName(i).ToLower() == "_color")
                    {
                        break;
                    }
                    var colorA = a.GetColor(propertyId);
                    var colorB = b.GetColor(propertyId);
                    if (!Similar(colorA, colorB))
                    {
                        pass = false;
                    }
                    break;
                case ShaderPropertyType.Range:
                case ShaderPropertyType.Float:
                    if (!Similar(a.GetFloat(propertyId), b.GetFloat(propertyId)))
                    {
                        pass = false;
                    }
                    break;
                case ShaderPropertyType.Texture:
                    if (a.GetTexture(propertyId) != b.GetTexture(propertyId))
                    {
                        pass = false;
                    }
                    break;
                case ShaderPropertyType.Vector:
                    var vA = a.GetVector(propertyId);
                    var vB = b.GetVector(propertyId);
                    if (!Similar(vA[0], vB[0]) || !Similar(vA[1], vB[1]) || !Similar(vA[2], vB[2]) || !Similar(vA[3], vB[3]))
                    {
                        pass = false;
                    }
                    break;
            }

            if (!pass)
            {
                break;
            }
        }

        return pass;
    }

    private static bool Similar(float a, float b) => Mathf.Abs(a - b) <= k_SimilarityEpsilon;
    private static bool Similar(float a, float b, float epsilon) => Mathf.Abs(a - b) <= epsilon;
    private static bool Similar(Color a, Color b) => Similar(a.r, b.r) && Similar(a.g, b.g) && Similar(a.b, b.b) && Similar(a.a, b.a);
    private static bool Similar(Color a, Color b, float epsilon) => Similar(a.r, b.r, epsilon) && Similar(a.g, b.g, epsilon) && Similar(a.b, b.b, epsilon) && Similar(a.a, b.a, epsilon);

}

public class MaterialMergeCriteria
{
    public bool SameColor { get; set; }
    public bool SameName { get; set; }
    public bool SameProperties { get; set; }
    public bool SameTexture { get; set; }
    public bool SameShader { get; set; }
    public int SameNameDistance { get; set; }
    public float SameColorEpsilon { get; set; }

    public MaterialMergeCriteria(bool sameColor, bool sameName, bool sameProperties, bool sameTexture, bool sameShader, int sameNameDistance, float sameColorEpsilon)
    {
        SameColor = sameColor;
        SameName = sameName;
        SameProperties = sameProperties;
        SameTexture = sameTexture;
        SameShader = sameShader;
        SameNameDistance = sameNameDistance;
        SameColorEpsilon = sameColorEpsilon;
    }

    public MaterialMergeCriteria(bool allParametersValue, int sameNameDistance, float sameColorEpsilon)
    {
        SameColor = allParametersValue;
        SameName = allParametersValue;
        SameProperties = allParametersValue;
        SameTexture = allParametersValue;
        SameShader = allParametersValue;
        SameNameDistance = sameNameDistance;
        SameColorEpsilon = sameColorEpsilon;
    }
}
