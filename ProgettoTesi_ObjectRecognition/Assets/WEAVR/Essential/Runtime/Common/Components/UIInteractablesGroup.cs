using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Groups/UI Interactables")]
    public class UIInteractablesGroup : MonoBehaviour
    {
        public bool distinctOnly = true;
        public bool removeRaycastsOnChildren = true;
        [HiddenBy(nameof(removeRaycastsOnChildren))]
        public bool inactiveChildrenAsWell = false;
        public Selectable[] objects;

        public bool InteractiveAll
        {
            get => objects.All(s => s.gameObject.activeInHierarchy && s.interactable);
            set
            {
                foreach(var o in objects)
                {
                    EnableInteractable(o, value);
                }
            }
        }

        public Selectable OnlyInteractive
        {
            get => objects.SingleOrDefault(s => s.gameObject.activeInHierarchy && s.interactable);
            set
            {
                if(value && objects.Contains(value))
                {
                    foreach (var o in objects)
                    {
                        EnableInteractable(o, value == o);
                    }
                }
            }
        }

        private void EnableInteractable(Selectable obj, bool value)
        {
            obj.interactable = value;
            if (removeRaycastsOnChildren)
            {
                foreach(var child in obj.GetComponentsInChildren<Graphic>(inactiveChildrenAsWell))
                {
                    child.raycastTarget = value;
                }
            }
        }

        public int OnlyInteractiveIndex
        {
            get
            {
                int index = -1;
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].gameObject.activeInHierarchy && objects[i].interactable)
                    {
                        if(index > -1)
                        {
                            return -1;
                        }
                        index = i;
                    }
                }
                return -1;
            }
            set
            {
                InteractiveAll = false;
                if(0 <= value && value < objects.Length)
                {
                    EnableInteractable(objects[value], true);
                }
            }
        }

        private void Reset()
        {
            objects = GetComponentsInChildren<Selectable>(true).Where(t => t.gameObject != gameObject).ToArray();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && distinctOnly)
            {
                objects = objects.Distinct().ToArray();
            }
        }
    }
}
