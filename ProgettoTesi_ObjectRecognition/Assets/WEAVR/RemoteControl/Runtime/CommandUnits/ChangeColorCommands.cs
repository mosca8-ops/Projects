using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    [AddComponentMenu("WEAVR/Remote Control/Commands/Change Color")]
    public class ChangeColorCommands : BaseCommandUnit
    {
        [RemotelyCalled]
        public void ChangeColor(Guid guid, Color color)
        {
            var renderer = Query.GetComponentByGuid<Renderer>(guid);
            if (renderer)
            {
                renderer.material.color = color;
            }
        }

        [RemotelyCalled]
        public bool ChangeColor(string path, Color color)
        {
            var renderer = Query.Find<Renderer>(QuerySearchType.Scene, path).First();
            if (renderer)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].color = color;
                }
                renderer.materials = materials;
                return true;
            }
            return false;
        }

        [RemotelyCalled]
        public Color GetColor(string path)
        {
            var renderer = Query.Find<Renderer>(QuerySearchType.Scene, path).First();
            return renderer ? renderer.material.color : Color.clear;
        }
    }
}
