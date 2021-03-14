using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player
{

    [AddComponentMenu("")]
    public class MultiBlockBackground : MonoBehaviour
    {
        public float maxMove = 0.5f;
        public float move = 0.5f;
        [Range(2, 10)]
        public int minSpan = 5;
        [Range(20, 100)]
        public int maxSpan = 40;
        [Range(1, 30)]
        public int framesSkip = 5;
        [Range(0, 2)]
        public float speedFactor = 0.1f;
        public Transform[] elements;

        private float[] m_targets;

        private void Reset()
        {
            List<Transform> ts = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                ts.Add(transform.GetChild(i));
            }
            elements = ts.ToArray();
        }

        private void Start()
        {
            m_targets = new float[elements.Length];
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.frameCount % framesSkip == 0)
            {
                int randStart = Random.Range(0, elements.Length / 5);
                int randSpan = Random.Range(minSpan, maxSpan);
                float randMove = Random.Range(move * 0.5f, move) * Mathf.Sign(Random.Range(-1, 1));
                for (int i = randStart; i < elements.Length; i += randSpan)
                {
                    m_targets[i] = Mathf.Clamp(randMove + elements[i].localPosition.y, -maxMove, maxMove);
                }
            }
            float speed = Time.deltaTime * speedFactor;
            for (int i = 0; i < elements.Length; i++)
            {
                var position = elements[i].localPosition;
                position.y = Mathf.Lerp(position.y, m_targets[i], Time.deltaTime);
                elements[i].localPosition = position;
            }
        }
    }
}
