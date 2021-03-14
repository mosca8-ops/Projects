using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Groups/Animator Group")]
    public class AnimatorGroup : MonoBehaviour
    {

        [Button(nameof(UpdateAnimatorsFromChildren), "AutoGet")]
        [ShowAsReadOnly]
        public int animatorsCount;
        [SerializeField]
        [Draggable]
        private List<Animator> m_animators;

        public IReadOnlyList<Animator> Animators => m_animators;

        public void StartPlayback()
        {
            for (int i = 0; i < m_animators.Count; i++)
            {
                m_animators[i].StartPlayback();
            }
        }

        public void StopPlayback()
        {
            for (int i = 0; i < m_animators.Count; i++)
            {
                m_animators[i].StopPlayback();
            }
        }

        public void SetTrigger(string trigger)
        {
            for (int i = 0; i < m_animators.Count; i++)
            {
                m_animators[i].SetTrigger(trigger);
            }
        }

        public void ResetTrigger(string trigger)
        {
            for (int i = 0; i < m_animators.Count; i++)
            {
                m_animators[i].ResetTrigger(trigger);
            }
        }

        public void SetBoolToTrue(string parameter)
        {
            for (int i = 0; i < m_animators.Count; i++)
            {
                m_animators[i].SetBool(parameter, true);
            }
        }

        public void SetBoolToFalse(string parameter)
        {
            for (int i = 0; i < m_animators.Count; i++)
            {
                m_animators[i].SetBool(parameter, false);
            }
        }

        private void UpdateAnimatorsFromChildren()
        {
            if (m_animators == null)
            {
                m_animators = new List<Animator>(GetComponentsInChildren<Animator>());
            }
            else
            {
                m_animators.Clear();
                m_animators.AddRange(GetComponentsInChildren<Animator>());
            }
        }
    }
}