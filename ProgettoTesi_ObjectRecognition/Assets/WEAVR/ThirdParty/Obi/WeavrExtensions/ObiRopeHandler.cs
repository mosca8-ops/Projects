using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

#if WEAVR_EXTENSIONS_OBI
using Obi;
#endif

#if WEAVR_EXTENSIONS_OBI
[RequireComponent(typeof(ObiRope))]
#endif
public class ObiRopeHandler : MonoBehaviour
{

    public bool disableOnStart = true;
    public float disableDelay = 3;

    [SerializeField]
    private Events m_events;

    public UnityEvent OnActivated => m_events.OnActivated;
    public UnityEvent OnDeactivated => m_events.OnDeactivated;
    public UnityEventBoolean OnStateChanged => m_events.OnStateChanged;

#if WEAVR_EXTENSIONS_OBI
    private ObiRope m_rope;
    private ObiSolver m_solver;
#endif

#if WEAVR_EXTENSIONS_OBI
    public bool IsActive
    {
        get { return m_rope.enabled; }
        set
        {
            StopAllCoroutines();
            if (m_rope.enabled != value)
            {
                if (value)
                {
                    m_rope.AddToSolver();
                    m_rope.enabled = true;
                    m_events.OnActivated.Invoke();
                    m_events.OnStateChanged.Invoke(value);
                }
                else
                {
                    StartCoroutine(DisableCoroutine(disableDelay));
                }
            }
        }
    }

    void Start()
    {
        m_rope = GetComponent<ObiRope>();
        m_solver = m_rope.solver != null ? m_rope.solver : ObiSolverRetriever.Solver;

        if (disableOnStart)
        {
            IsActive = false;
        }
    }

    public void SetActive(bool active)
    {
        IsActive = active;
    }

    public void Toggle()
    {
        IsActive = !IsActive;
    }

    private IEnumerator DisableCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_rope.enabled = false;
        m_rope.RemoveFromSolver();

        m_events.OnDeactivated.Invoke();
        m_events.OnStateChanged.Invoke(false);
    }
#endif

    [Serializable]
    private struct Events
    {
        public UnityEvent OnActivated;
        public UnityEvent OnDeactivated;
        public UnityEventBoolean OnStateChanged;
    }
}
