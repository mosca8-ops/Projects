using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Manipulators/Transparency Switcher")]
    public class VR_TransparencySwitcher : VR_Manipulator
    {
        public enum TransparencyMode { TransparentOnHover, OpaqueOnHover }

        public TransparencyMode mode = TransparencyMode.TransparentOnHover;
        public GameObject manipulate;
        [Range(0, 1)]
        public float transparency = 0.4f;

#if WEAVR_VR

        private void Start()
        {
            if(manipulate == null)
            {
                manipulate = gameObject;
            }  
        }

        private static void MakeTransparent(GameObject gameobject, float alpha)
        {
            foreach (var renderer in gameobject.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled)
                {
                    MakeTransparent(renderer, alpha);
                }
            }
        }

        private static void MakeOpaque(GameObject gameobject)
        {
            foreach (var renderer in gameobject.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                }
                MakeOpaque(renderer);
            }
        }

        private static void MakeTransparent(Renderer renderer, float transparency)
        {
            if (renderer.material.GetInt("_Mode") == 0)
            {
                renderer.material.SetInt("_Mode", 2);

                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }

            Color col = renderer.material.GetColor("_Color");
            col.a = transparency;
            renderer.material.SetColor("_Color", col);
        }

        private static void MakeOpaque(Renderer renderer)
        {
            if (renderer.material.GetFloat("_Mode") == 2)
            {
                renderer.material.SetFloat("_Mode", 0);

                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                renderer.material.SetInt("_ZWrite", 1);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.DisableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = -1;
            }
        }

        public override bool CanHandleData(object value)
        {
            return true;
        }

        public override void UpdateValue(float value)
        {
            
        }

        protected override void UpdateManipulator(Hand hand, Interactable interactable)
        {
            
        }

        protected override void BeginManipulating(Hand hand, Interactable interactable)
        {
            base.BeginManipulating(hand, interactable);
            switch (mode)
            {
                case TransparencyMode.OpaqueOnHover:
                    MakeOpaque(manipulate);
                    break;
                case TransparencyMode.TransparentOnHover:
                    MakeTransparent(manipulate, transparency);
                    break;
            }
        }

        public override void StopManipulating(Hand hand, Interactable interactable)
        {
            base.StopManipulating(hand, interactable);
            switch (mode)
            {
                case TransparencyMode.OpaqueOnHover:
                    MakeTransparent(manipulate, transparency);
                    break;
                case TransparencyMode.TransparentOnHover:
                    MakeOpaque(manipulate);
                    break;
            }
        }

#endif
    }
}
