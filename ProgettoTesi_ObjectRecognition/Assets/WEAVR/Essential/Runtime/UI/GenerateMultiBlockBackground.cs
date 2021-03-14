using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player
{

    [AddComponentMenu("")]
    public class GenerateMultiBlockBackground : MonoBehaviour
    {
        [Header("Generation")]
        [Draggable]
        public Renderer sample;
        [Draggable]
        public Texture texture;
        public Vector2 worldSize = new Vector2(4, 3);
        public Vector2Int matrixSize = new Vector2Int(8, 6);
        [Header("Dynamics")]
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

        private List<Transform> m_elements;

        private float[] m_targets;

        private void Start()
        {
            m_elements = new List<Transform>();
            var offset = new Vector3(worldSize.x / matrixSize.x, worldSize.x / matrixSize.x, worldSize.y / matrixSize.y);
            var texOffset = new Vector2(1f / matrixSize.x, 1f / matrixSize.y);
            var origin = transform.position;
            for (int x = 0; x < matrixSize.x; x++)
            {
                for (int y = 0; y < matrixSize.y; y++)
                {
                    var elem = Instantiate(sample);
                    elem.gameObject.SetActive(true);
                    elem.transform.SetParent(transform, false);
                    elem.transform.localScale = Vector3.Scale(elem.transform.localScale, offset);
                    elem.transform.localPosition = new Vector3(origin.x + offset.x * x, origin.y, origin.z + offset.z * y);
                    elem.material.mainTexture = texture;
                    elem.material.mainTextureScale = texOffset;
                    elem.material.mainTextureOffset = new Vector2(texOffset.x * x, texOffset.y * y);

                    m_elements.Add(elem.transform);
                }
            }

            m_targets = new float[m_elements.Count];
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.frameCount % framesSkip == 0)
            {
                int randStart = Random.Range(0, m_elements.Count / 5);
                int randSpan = Random.Range(minSpan, maxSpan);
                float randMove = Random.Range(move * 0.5f, move) * Mathf.Sign(Random.Range(-1, 1));
                for (int i = randStart; i < m_elements.Count; i += randSpan)
                {
                    m_targets[i] = Mathf.Clamp(randMove + m_elements[i].localPosition.y, -maxMove, maxMove);
                }
            }
            float speed = Time.deltaTime * speedFactor;
            for (int i = 0; i < m_elements.Count; i++)
            {
                var position = m_elements[i].localPosition;
                position.y = Mathf.Lerp(position.y, m_targets[i], Time.deltaTime);
                m_elements[i].localPosition = position;
            }
        }
    }
}
