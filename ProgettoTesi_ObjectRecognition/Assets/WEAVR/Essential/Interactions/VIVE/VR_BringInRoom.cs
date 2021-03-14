using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Interactions/Bring in Room")]
    public class VR_BringInRoom : MonoBehaviour
    {

        public bool moveOnEnable = true;
        public float edgeRatio = 0.9f;
        public float moveTime = 1;
        public Transform[] objectsToMove;

        private bool m_moveNow;
        public bool MoveNow
        {
            get { return m_moveNow; }
            set
            {
                MoveAllObjects();
                //if(m_moveNow != value)
                //{
                //}
            }
        }

        private void Start()
        {
            if (moveOnEnable)
            {
                MoveAllObjects();
            }
        }

#if WEAVR_VR

        private void MoveAllObjects()
        {
            foreach (var obj in objectsToMove)
            {
                StartCoroutine(MoveToChaperon(obj, moveTime));
            }
        }

        IEnumerator MoveToChaperon(Transform t, float time)
        {
            var chaperon = ChaperoneInfo.instance;
            while (!chaperon.initialized)
            {
                yield return null;
            }

            var tPos = t.position;
            var direction = tPos - transform.position;

            float chaperonMinEdgeDistance = Mathf.Min(chaperon.playAreaSizeX, chaperon.playAreaSizeZ) * edgeRatio / 2;
            var targetPoint = transform.position + direction.normalized * chaperonMinEdgeDistance;
            float speed = time > 0 ? Vector3.Magnitude(tPos - targetPoint) / time : float.MaxValue;

            while (Vector3.Magnitude(t.position - targetPoint) > 0.05f)
            {
                var newPosition = Vector3.Lerp(t.position, targetPoint, speed * Time.deltaTime);
                newPosition.y = tPos.y;
                t.position = newPosition;
                yield return null;
            }
        }

#else
        private void MoveAllObjects()
        {

        }

#endif
    }
}
