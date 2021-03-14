namespace TXT.WEAVR.Common
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("WEAVR/Components/Distance Fader")]
    public class DistanceFader : MonoBehaviour
    {
        [Header("Targets")]
        [Draggable]
        public Transform[] targets;
        [Range(0.1f, 20)]
        public float triggerDistance = 0.5f;

        [Header("Other Objects")]
        public GameObject[] enableObjects;
        private bool[] _enableObjectsDefaultValues;
        public GameObject[] disableObjects;
        private bool[] _disableObjectsDefaultValues;

        [Header("Materials")]
        public bool forceFadeMaterials = false;
        public bool dynamicMaterialSearch = true;
        [Range(0, 1)]
        public float minFade = 0.5f;
        public Material[] materialsToFade;
        private float[] _rangeFadeValues;
        private float[] _normalizedFadeValues;


        private bool _isInRange;
        private bool? _hasMaterials;
        private Transform _thisTransform;

        private void OnValidate()
        {
            _hasMaterials = _hasMaterials ?? true;
            if(_hasMaterials == true && (materialsToFade == null || materialsToFade.Length == 0))
            {
                var renderer = GetComponent<Renderer>();
                if(renderer == null) {
                    _hasMaterials = false;
                    return;
                }
                materialsToFade = new Material[renderer.materials.Length]; 
                for (int i = 0; i < materialsToFade.Length; i++)
                {
                    materialsToFade[i] = renderer.materials[i];
                }
            }
        }

        private void Start()
        {
            _rangeFadeValues = new float[materialsToFade.Length];
            _normalizedFadeValues = new float[materialsToFade.Length];
            _enableObjectsDefaultValues = new bool[enableObjects.Length];
            _disableObjectsDefaultValues = new bool[disableObjects.Length];
            _thisTransform = transform;
            _isInRange = false;

            if (forceFadeMaterials)
            {
                foreach(var material in materialsToFade)
                {
                    ChangeRenderMode(material, BlendMode.Fade);
                }
            }

            UpdateMaterialsFadeValues();
        }

        private void UpdateMaterialsFadeValues()
        {
            for (int i = 0; i < materialsToFade.Length; i++)
            {
                _normalizedFadeValues[i] = materialsToFade[i].color.a;
                _rangeFadeValues[i] = materialsToFade[i].color.a - minFade;
            }
        }

        private void Update()
        {
            float sqrTriggerDistance = triggerDistance * triggerDistance;
            float minSqrDistance = sqrTriggerDistance;

            bool alreadyInRange = _isInRange;
            _isInRange = false;

            foreach(var target in targets)
            {
                float sqrDistance = (_thisTransform.position - target.position).sqrMagnitude;
                _isInRange |= sqrDistance < sqrTriggerDistance;
                if(minSqrDistance > sqrDistance)
                {
                    minSqrDistance = sqrDistance;
                }
            }

            if (_isInRange)
            {
                if (!alreadyInRange) {
                    RememberObjectsDefaultValues();
                }
                UpdateEnableDisableObjects(true);

                // Save the fade values of materials if it is the first frame
                if (!alreadyInRange)
                {
                    if (dynamicMaterialSearch) {
                        UpdateMaterials();
                    }
                    else {
                        UpdateMaterialsFadeValues();
                    }
                }

                if (materialsToFade.Length > 0)
                {
                    float distanceNormalized = (triggerDistance - Mathf.Sqrt(minSqrDistance)) / triggerDistance;
                    SetFadeOfMaterials(distanceNormalized);
                }
            }
            else if (alreadyInRange)
            {
                UpdateEnableDisableObjects(false);
                for (int i = 0; i < materialsToFade.Length; i++)
                {
                    Color color = materialsToFade[i].color;
                    color.a = _normalizedFadeValues[i];
                    materialsToFade[i].color = color;
                }
            }
        }

        private void SetFadeOfMaterials(float distanceNormalized)
        {
            for (int i = 0; i < materialsToFade.Length; i++)
            {
                Color color = materialsToFade[i].color;
                color.a = _normalizedFadeValues[i] - _rangeFadeValues[i] * distanceNormalized;
                materialsToFade[i].color = color;
            }
        }

        private void RememberObjectsDefaultValues() {
            for (int i = 0; i < enableObjects.Length; i++) {
                _enableObjectsDefaultValues[i] = !enableObjects[i].activeInHierarchy;
            }
            for (int i = 0; i < disableObjects.Length; i++) {
                _disableObjectsDefaultValues[i] = disableObjects[i].activeInHierarchy;
            }
        }

        private void UpdateEnableDisableObjects(bool enable)
        {
            for (int i = 0; i < disableObjects.Length; i++) {
                disableObjects[i].SetActive(!enable && _disableObjectsDefaultValues[i]);
            }
            for (int i = 0; i < enableObjects.Length; i++) {
                enableObjects[i].SetActive(enable && _enableObjectsDefaultValues[i]);
            }
            //foreach (var objectToDisable in disableObjects)
            //{
            //    objectToDisable.SetActive(!enable);
            //}
            //foreach (var objectToEnable in enableObjects)
            //{
            //    objectToEnable.SetActive(enable);
            //}
        }

        private void UpdateMaterials() {
            var renderer = GetComponent<Renderer>();
            if (renderer == null) {
                _hasMaterials = false;
                materialsToFade = new Material[0];
            }
            else {
                materialsToFade = new Material[renderer.materials.Length];
            }
            for (int i = 0; i < materialsToFade.Length; i++) {
                materialsToFade[i] = renderer.materials[i];
            }

            _rangeFadeValues = new float[materialsToFade.Length];
            _normalizedFadeValues = new float[materialsToFade.Length];
            UpdateMaterialsFadeValues();
        }

        private void OnDisable() {
            Debug.Log("Disabling");
            if (_isInRange) {
                UpdateEnableDisableObjects(false);
                for (int i = 0; i < materialsToFade.Length; i++) {
                    Color color = materialsToFade[i].color;
                    color.a = _normalizedFadeValues[i];
                    materialsToFade[i].color = color;
                }
            }
        }

        private enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent
        }

        private static void ChangeRenderMode(Material standardShaderMaterial, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    standardShaderMaterial.SetInt("_ZWrite", 1);
                    standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    standardShaderMaterial.SetInt("_ZWrite", 1);
                    standardShaderMaterial.EnableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = 2450;
                    break;
                case BlendMode.Fade:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    standardShaderMaterial.SetInt("_ZWrite", 0);
                    standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.EnableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = 3000;
                    break;
                case BlendMode.Transparent:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    standardShaderMaterial.SetInt("_ZWrite", 0);
                    standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = 3000;
                    break;
            }
        }
    }
}