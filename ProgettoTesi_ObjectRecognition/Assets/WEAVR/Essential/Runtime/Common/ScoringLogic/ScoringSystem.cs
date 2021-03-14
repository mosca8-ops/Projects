using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    public interface IScoringItem
    {
        float CurrentScore { get; }
        float CorrectAnswerPoints { get; }
        float WrongAnswerPoints { get; }

        event Action<IScoringItem, ScoringSystem.Answer> AnswerChanged; 
        ScoringSystem.Answer CurrentAnswer { get; }
    }

    public interface IScoringItem<T> : IScoringItem
    {
        T CurrentAnsweredValue { get; set; }
        T CorrectValue { get; }
    }

    [AddComponentMenu("WEAVR/Scoring System/Scoring System")]
    public class ScoringSystem : MonoBehaviour
    {
        public enum Answer { Correct, Wrong, NotAnswered }


        public float correctAnswerPoints = 10;
        public float wrongAnswerPoints = -1;

        [Space]
        public UnityEventFloat onScoreChanged;

        [SerializeField]
        [ShowAsReadOnly]
        private float m_score;

        private List<IScoringItem> m_scoringItems = new List<IScoringItem>();

        public IReadOnlyList<IScoringItem> ScoringItems => m_scoringItems;
        public int TotalItems => m_scoringItems.Count;

        public bool FullyAnswered => !m_scoringItems.Any(i => i.CurrentAnswer == Answer.NotAnswered);

        public bool HasWrongAnswers => m_scoringItems.Any(i => i.CurrentAnswer == Answer.Wrong);

        public void Register(IScoringItem item)
        {
            if (!m_scoringItems.Contains(item))
            {
                item.AnswerChanged -= Item_AnswerChanged;
                item.AnswerChanged += Item_AnswerChanged;
                m_scoringItems.Add(item);
            }
        }

        private void Item_AnswerChanged(IScoringItem item, Answer answer)
        {
            onScoreChanged?.Invoke(CurrentScore);
        }

        public void Unregister(IScoringItem item)
        {
            m_scoringItems.Remove(item);
            item.AnswerChanged -= Item_AnswerChanged;
        }

        void Start()
        {
            m_score = 0;
        }

        public float CurrentScore
        {
            get
            {
                float points = 0;
                foreach(var item in ScoringItems)
                {
                    points += item.CurrentScore;
                }

                m_score = points;
                return points;
            }
        }
    }
}
