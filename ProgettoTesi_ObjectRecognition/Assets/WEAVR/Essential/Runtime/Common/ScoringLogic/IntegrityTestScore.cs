using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    public enum IntegrityTest { Pass, Fail, NotPerformed }

    [AddComponentMenu("WEAVR/Scoring System/Integrity Test Score")]
    public class IntegrityTestScore : ScoringItem<IntegrityTest>
    {
        [Space]
        public GameObject correctObject;

        [Space]
        public string passText = "PASS";
        public string failText = "FAIL";
        public string untestedText = "UNTESTED";

        protected override void Start()
        {
            base.Start();
            m_currentValue = IntegrityTest.NotPerformed;
        }

        private void Update()
        {
            CorrectValue = correctObject.activeInHierarchy ? IntegrityTest.Pass : IntegrityTest.Fail;
        }

        protected override void UpdateAnswer()
        {
            if (m_currentValue == IntegrityTest.NotPerformed)
            {
                CurrentAnswer = ScoringSystem.Answer.NotAnswered;
            }
            else
            {
                base.UpdateAnswer();
            }
        }

        public void SetPass()
        {
            CurrentAnsweredValue = IntegrityTest.Pass;
        }

        public void SetFail()
        {
            CurrentAnsweredValue = IntegrityTest.Fail;
        }

        public void ResetTest()
        {
            CurrentAnsweredValue = IntegrityTest.NotPerformed;
        }

        protected override string ToString(IntegrityTest value) => value == IntegrityTest.Pass ? passText : value == IntegrityTest.NotPerformed ? untestedText : failText;
    }
}
