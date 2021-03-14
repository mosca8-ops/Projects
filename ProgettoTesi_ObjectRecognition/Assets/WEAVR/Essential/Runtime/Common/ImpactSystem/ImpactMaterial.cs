using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.ImpactSystem
{
    [CreateAssetMenu(fileName = "Material", menuName = "WEAVR/Material")]
    public class ImpactMaterial : ScriptableObject
    {
        [SerializeField]
        [Draggable]
        private PhysicMaterial m_material;
        [SerializeField]
        [Draggable]
        private AudioSource m_sampleAudioSource;

        [Space]
        [SerializeField]
        private ImpactData m_defaultImpact;

        [Space]
        [SerializeField]
        private List<ImpactDataGroup> m_impacts;

        public event Action<ImpactMaterial, PhysicMaterial> MaterialChanged;

        public PhysicMaterial Material {
            get => m_material;
            set
            {
                if(m_material != value)
                {
                    m_material = value;
                    MaterialChanged?.Invoke(this, m_material);
                }
            }
        }

        public AudioSource SampleAudioSource
        {
            get => m_sampleAudioSource;
            set
            {
                if(m_sampleAudioSource != value)
                {
                    m_sampleAudioSource = value;
                }
            }
        }

        public IReadOnlyList<ImpactDataGroup> Impacts => m_impacts;

        private Dictionary<ImpactMaterial, ImpactDataGroup> m_impactsDictionary;

        protected virtual void OnEnable()
        {
            if(m_impacts == null)
            {
                m_impacts = new List<ImpactDataGroup>();
            }
            if (m_impacts != null)
            {
                RefreshImpacts();
                if (m_impactsDictionary == null)
                {
                    m_impactsDictionary = new Dictionary<ImpactMaterial, ImpactDataGroup>();
                    for (int i = 0; i < m_impacts.Count; i++)
                    {
                        if (m_impacts[i].material)
                        {
                            m_impactsDictionary[m_impacts[i].material] = m_impacts[i];
                        }
                    }
                }
                ValidateImpactData();
            }
        }

        private void ValidateImpactData()
        {
            m_defaultImpact.Validate();
            for (int i = 0; i < m_impacts.Count; i++)
            {
                for (int j = 0; j < m_impacts[i].data.Count; j++)
                {
                    m_impacts[i].data[j]?.Validate();
                }
            }
        }

        public virtual void ApplyImpactAt(Vector3 point, float force, ImpactMaterial other, Vector3 impulse, Collider targetCollider)
        {
            var impactData = other != null ? GetImpact(other, force) : m_defaultImpact;
            if(impactData == null) { return; }
            if (SampleAudioSource)
            {
                var cloneAudio = Instantiate(SampleAudioSource);
                cloneAudio.transform.position = point;
                impactData.Apply(cloneAudio, impulse, targetCollider);
                if (cloneAudio.clip)
                {
                    Destroy(cloneAudio.gameObject, cloneAudio.clip.length * (1 / cloneAudio.pitch));
                }
                else
                {
                    Destroy(cloneAudio.gameObject);
                }
            }
            else
            {
                impactData.Apply(point, impulse, targetCollider);
            }
        }

        public virtual void ApplyImpact(Collision collision, ContactPoint point, float force, ImpactMaterial other)
        {
            var impactData = other != null ? GetImpact(other, force) : m_defaultImpact;
            if (impactData == null) { return; }
            if (SampleAudioSource)
            {
                var cloneAudio = Instantiate(SampleAudioSource);
                cloneAudio.transform.position = point.point;
                impactData.Apply(collision, point, cloneAudio, point.otherCollider);
                if (cloneAudio.clip)
                {
                    Destroy(cloneAudio.gameObject, cloneAudio.clip.length * (1 / cloneAudio.pitch));
                }
                else
                {
                    Destroy(cloneAudio.gameObject);
                }
            }
            else
            {
                impactData.Apply(collision, point, null, point.otherCollider);
            }
        }

        public virtual ImpactData GetImpact(ImpactMaterial otherType, float force)
        {
            for (int i = 0; i < m_impacts.Count; i++)
            {
                if(m_impacts[i].material == otherType)
                {
                    return m_impacts[i].GetByForce(force);
                }
            }
            return otherType == this ? m_defaultImpact : otherType.GetImpactInternal(this, force) ?? (m_defaultImpact.IsEmpty ? otherType.m_defaultImpact : m_defaultImpact);
        }

        protected virtual ImpactData GetImpactInternal(ImpactMaterial otherType, float force)
        {
            for (int i = 0; i < m_impacts.Count; i++)
            {
                if (m_impacts[i].material == otherType)
                {
                    return m_impacts[i].GetByForce(force);
                }
            }
            return m_defaultImpact.IsEmpty ? otherType.m_defaultImpact : m_defaultImpact;
        }

        public void RefreshImpacts()
        {
            foreach(var group in m_impacts)
            {
                group.Reorder();
            }
        }

        [Serializable]
        public class ImpactDataGroup
        {
            public ImpactMaterial material;
            public List<ImpactData> data;

            public void Reorder()
            {
                data?.Sort((a, b) => a.force > b.force ? 1 : a.force == b.force ? 0 : -1);
            }

            public ImpactData GetByForce(float force)
            {
                if (data != null)
                {
                    for (int i = data.Count - 1; i >= 0; i--)
                    {
                        if (data[i].force <= force)
                        {
                            return data[i];
                        }
                    }
                }
                return null;
            }
        }
    }

    [Serializable]
    public class ImpactData : ICloneable
    {
        public float force;
        public Sound[] sounds;
        public Effect[] effects;

        private bool[] m_isSpecializedEffect;

        public Sound RandomSound => sounds != null && sounds.Length > 0 ? sounds[UnityEngine.Random.Range(0, sounds.Length - 1)] : default;

        public bool IsEmpty => force == 0 && (sounds == null || sounds.Length == 0) && (effects == null || effects.Length == 0);

        public void Validate()
        {
            if (effects != null)
            {
                m_isSpecializedEffect = new bool[effects.Length];
                for (int i = 0; i < effects.Length; i++)
                {
                    m_isSpecializedEffect[i] = effects[i].effect && effects[i].effect.GetComponent<IImpactEffect>() != null;
                }
            }
            else
            {
                m_isSpecializedEffect = new bool[0];
            }
            if(sounds != null)
            {
                for (int i = 0; i < sounds.Length; i++)
                {
                    sounds[i].LimitPitch(0, 2);
                }
            }
            else
            {
                sounds = new Sound[0];
            }
        }

        public virtual void Apply(Vector3 point, Vector3 impulse, Collider collider)
        {
            PlayAudio(point);
            InstantiateEffects(point, impulse, collider);
        }

        public virtual void Apply(AudioSource source, Vector3 impulse, Collider collider)
        {
            PlayAudio(source);
            InstantiateEffects(source.transform.position, impulse, collider);
        }

        public virtual void Apply(Collision collision, ContactPoint point, AudioSource source, Collider collider)
        {
            if (source)
            {
                PlayAudio(source);
            }
            else
            {
                PlayAudio(point.point);
            }
            InstantiateEffects(collision, point, collider);
        }

        public virtual void PlayAudio(Vector3 point)
        {
            var sound = RandomSound;
            if (sound.clip)
            {
                AudioSource.PlayClipAtPoint(RandomSound.clip, point);
            }
        }

        public virtual void PlayAudio(AudioSource source)
        {
            var sound = RandomSound;
            if (sound.clip)
            {
                source.pitch = sound.pitch.random;
                source.clip = sound.clip;
                source.Play();
            }
        }

        public virtual void InstantiateEffects(Collision collision, ContactPoint point, Collider collider)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = UnityEngine.Object.Instantiate(effects[i].effect);
                if (m_isSpecializedEffect[i])
                {
                    effect.GetComponent<IImpactEffect>().PerformEffect(collision, point, collider);
                }
                if (effects[i].followCollider)
                {
                    effect.transform.SetParent(point.thisCollider.transform, false);
                }
                else if (effects[i].respondToImpulse && collision.impulse != Vector3.zero)
                {
                    effect.transform.forward = collision.impulse.normalized;
                }
                effect.transform.position = point.point;
                UnityEngine.Object.Destroy(effect, effects[i].lifetime);
            }
        }

        public virtual void InstantiateEffects(Vector3 point, Vector3 impulse, Collider collider)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = UnityEngine.Object.Instantiate(effects[i].effect);
                if (effects[i].followCollider && collider)
                {
                    effect.transform.SetParent(collider.transform, false);
                }
                else if (effects[i].respondToImpulse)
                {
                    effect.transform.forward = impulse.normalized;
                }
                effect.transform.position = point;
                UnityEngine.Object.Destroy(effect, effects[i].lifetime);
            }
        }

        public object Clone()
        {
            return new ImpactData()
            {
                force = force,
                sounds = Clone(sounds),
                effects = Clone(effects)
            };
        }

        private T[] Clone<T>(T[] source)
        {
            T[] dest = new T[source.Length];
            Array.Copy(source, dest, source.Length);
            return dest;
        }

        [Serializable]
        public struct Sound
        {
            public const float k_defaultMinPitch = 0.9f;
            public const float k_defaultMaxPitch = 1.1f;

            public AudioClip clip;
            public Span pitch;

            public void LimitPitch(float min, float max)
            {
                if (pitch.max == 0 && pitch.min == 0)
                {
                    pitch.Resize(k_defaultMinPitch, k_defaultMaxPitch);
                }
                else
                {
                    pitch.Limit(min, max);
                }
            }
        }

        [Serializable]
        public struct Effect
        {
            public GameObject effect;
            public float lifetime;
            public bool followCollider;
            public bool respondToImpulse;
        }
    }
}
