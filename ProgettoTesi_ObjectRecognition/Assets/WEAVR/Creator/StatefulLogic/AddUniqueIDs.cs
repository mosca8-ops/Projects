using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TXT.WEAVR.Core;
using TXT.WEAVR.Interaction;

namespace TXT.WEAVR.Procedure
{
    public class AddUniqueIDs : MonoBehaviour
    {
        //[MenuItem("WEAVR/Procedures/AddUniqueIDs")]
        public static void AddUniqueIDinScene()
        {
            var gameObjects = new List<GameObject>();

            var rigidbodiesInScene = SceneTools.GetComponentsInScene<Rigidbody>().ToList();
            var interactionsInScene = SceneTools.GetComponentsInScene<AbstractInteractiveBehaviour>().ToList();

            for (int i = 0; i < rigidbodiesInScene.Count; i++)
            {
                if (!gameObjects.Contains(rigidbodiesInScene[i].gameObject))
                {
                    if (rigidbodiesInScene[i].GetComponent<UniqueID>() == null)
                        gameObjects.Add(rigidbodiesInScene[i].gameObject);
                }
            }

            for (int i = 0; i < interactionsInScene.Count; i++)
            {
                if (!gameObjects.Contains(interactionsInScene[i].gameObject))
                {
                    if (interactionsInScene[i].GetComponent<UniqueID>() == null)
                        gameObjects.Add(interactionsInScene[i].gameObject);
                }
            }

            for (int i = 0; i < gameObjects.Count; i++)
            {
                gameObjects[i].AddComponent<UniqueID>();
            }
        }
    } 
}
