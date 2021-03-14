#if WEAVR_EXTENSIONS_OBI
using Obi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObiSolverRetriever : MonoBehaviour {

    private static ObiSolver s_solver;

    public static ObiSolver Solver
    {
        get
        {
            if(s_solver == null)
            {
                s_solver = FindObjectOfType<ObiSolver>();
            }
            return s_solver;
        }
    }

    [SerializeField]
    private ObiSolver m_solver;

	// Use this for initialization
	void Start () {
		if(s_solver == null)
        {
            s_solver = m_solver;
        }
	}
}
#endif
