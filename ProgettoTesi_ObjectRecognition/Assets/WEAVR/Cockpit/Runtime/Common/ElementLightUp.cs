namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("")]
    public class ElementLightUp : MonoBehaviour
    {
        public Color emission;

        private List<Material> _materials;
        private MeshRenderer _renderer;
        private Color _noEmission;

        private Material[] _originalMaterials;

        private void Awake() {
            _noEmission = Color.black;
            _noEmission.a = 0;

            _renderer = GetComponent<MeshRenderer>();
            if(_renderer == null) {
                return;
            }
            _materials = new List<Material>();
            _originalMaterials = new Material[_renderer.materials.Length];
            int matIndex = 0;
            foreach(var mat in _renderer.materials) {
                _originalMaterials[matIndex++] = mat;
                var newMaterial = new Material(mat);
                newMaterial.SetColor("_EmissionColor", emission);
                newMaterial.EnableKeyword("_EMISSION");
                _materials.Add(newMaterial);
            }
        }

        public void LightsOn() {
            if (_renderer != null) {
                _renderer.materials = _materials.ToArray();
            }
        }

        public void LightsOff() {
            RestoreOriginalMaterials();
        }

        private void RestoreOriginalMaterials() {
            if (_renderer != null) {
                _renderer.materials = _originalMaterials;
            }
        }

        private void OnEnable() {
            //if(_renderer != null) {
            //    _renderer.materials = _materials.ToArray();
            //}
        }

        private void OnDisable() {
            RestoreOriginalMaterials();
        }

        private void OnDestroy() {
            RestoreOriginalMaterials();
        }
    }
}
