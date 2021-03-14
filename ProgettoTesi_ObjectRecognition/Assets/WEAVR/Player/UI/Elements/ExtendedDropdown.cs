using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TXT.WEAVR.Interaction;

namespace TXT.WEAVR.UI
{


    public class ExtendedDropdown : TMP_Dropdown
    {
        protected override void DestroyDropdownList(GameObject dropdownList)
        {
            base.DestroyDropdownList(dropdownList);
            EventSystem.current.SetSelectedGameObject(null);
        }

        protected override GameObject CreateDropdownList(GameObject template)
        {
            var list = base.CreateDropdownList(template);
            AddWorlPointerCanvas(list);
            return list;

        }

        protected override GameObject CreateBlocker(Canvas rootCanvas)
        {
            var block = base.CreateBlocker(rootCanvas);
            AddWorlPointerCanvas(block);
            Vector3 newBlockPosition = block.transform.position;
            newBlockPosition.z = transform.position.z;
            block.transform.position = newBlockPosition;
            return block;
        }

        private void AddWorlPointerCanvas(GameObject list)
        {
            var canvas = list.GetComponentInParent<Canvas>();
            if (!canvas.GetComponent<WorldPointerCanvas>())
            {
                canvas.gameObject.AddComponent<WorldPointerCanvas>();
            }
        }

    }
}
