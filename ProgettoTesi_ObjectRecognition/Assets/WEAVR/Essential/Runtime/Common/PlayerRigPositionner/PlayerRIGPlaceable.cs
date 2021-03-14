using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("")]
    public class PlayerRIGPlaceable : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        public GameObject PlayerRig;
        [SerializeField]
        [Draggable]
        public GameObject GroupCanvasPositions;
        [SerializeField]
        [Draggable]
        public GameObject ButtonPrefab;
        [SerializeField]
        [Draggable]
        public List<PlayerPlaces> Positions = new List<PlayerPlaces>();

        private Queue<GameObject> buttons = new Queue<GameObject>();

        public void GoToPosition(PlayerPlaces place)
        {
            if (place != null)
            {
                PlayerRig.transform.position = place.transform.position;
                PlayerRig.transform.rotation = place.transform.rotation;
            }
        }

        private void OnValidate()
        {
            for(int i = 0; i < Positions.Count; i++)
            {
                if(Positions[i].name != "" && Positions[i].transform != null)
                {
                    Positions[i].transform.name = Positions[i].name;
                }
            }
        }

        public void GenerateButtons()
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                if(Positions[i].transform != null)
                {
                    GameObject button = Instantiate(ButtonPrefab);
                    buttons.Enqueue(button);
                    button.GetComponentInChildren<Text>().text = Positions[i].name;

                    var x = Positions[i];
                    button.GetComponent<Button>().onClick.AddListener(() => {GoToPosition(x); });
                    button.transform.parent = GroupCanvasPositions.transform;

                    button.SetActive(true);
                }
            }
        }

        public void DestroyButtons()
        {
            var loopCount = buttons.Count;
            for(int i=0; i < (loopCount) ; i++)
            {
                var currentObject = buttons.Dequeue();
                Destroy(currentObject);
            }
        }

        [Serializable]
        public class PlayerPlaces
        {
            [CanBeGenerated("Player position ", Relationship.Child)]
            public Transform transform;

            public string name;
        }
    }
}
